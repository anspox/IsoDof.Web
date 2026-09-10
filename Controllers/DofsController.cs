using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using IsoDof.Web.Data;
using IsoDof.Web.Models;
using IsoDof.Web.Models.Entities;
using IsoDof.Web.Models.Entities.Enums;
using IsoDof.Web.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace IsoDof.Web.Controllers;

public class DofsController : Controller
{
    private const int DefaultPageSize = 15;

    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IDofReportService _reportService;

    public DofsController(AppDbContext context, IEmailService emailService, IDofReportService reportService)
    {
        _context = context;
        _emailService = emailService;
        _reportService = reportService;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private int? CurrentUserDepartmentId
    {
        get
        {
            var val = User.FindFirstValue("DepartmentId");
            if (int.TryParse(val, out var dId)) return dId;
            return _context.AppUsers.Find(CurrentUserId)?.DepartmentId;
        }
    }

    // Index ve export'ların paylaştığı ortak sorgu (filtre + yetki kapsamı + sıralama)
    private IQueryable<Dof> BuildDofQuery(int? departmentId, DofStatus? status, string? sortOrder, bool showArchived)
    {
        var query = _context.Dofs
            .Include(d => d.Department)
            .Include(d => d.CreatedByUser)
            .Include(d => d.AssignedToUser)
            .Where(d => d.IsArchived == showArchived);

        if (User.IsInRole("Admin"))
        {
            if (departmentId.HasValue)
                query = query.Where(d => d.DepartmentId == departmentId.Value);
        }
        else if (User.IsInRole("KaliteKontrol"))
        {
            // Kalite Kontrol sorumluları yalnızca kendi departmanındaki sorunları görür
            var deptId = CurrentUserDepartmentId;
            if (deptId.HasValue)
            {
                query = query.Where(d => d.DepartmentId == deptId.Value);
            }
            else
            {
                query = query.Where(d => false);
            }
        }
        else
        {
            // Kullanıcı kendi açtığı veya kendisine atanan DÖF'leri görür
            var currentUserId = CurrentUserId;
            query = query.Where(d => d.AssignedToUserId == currentUserId || d.CreatedByUserId == currentUserId);

            if (departmentId.HasValue)
                query = query.Where(d => d.DepartmentId == departmentId.Value);
        }

        if (status.HasValue)
            query = query.Where(d => d.Status == status.Value);

        query = sortOrder switch
        {
            "duedate_asc" => query.OrderBy(d => d.DueDate),
            "duedate_desc" => query.OrderByDescending(d => d.DueDate),
            "oldest" => query.OrderBy(d => d.CreatedAt),
            _ => query.OrderByDescending(d => d.CreatedAt),
        };

        return query;
    }

    public async Task<IActionResult> Index(int? departmentId, DofStatus? status, string? sortOrder,
        bool showArchived = false, int page = 1)
    {
        var query = BuildDofQuery(departmentId, status, sortOrder, showArchived);
        var pagedDofs = await PagedList<Dof>.CreateAsync(query, page, DefaultPageSize);

        ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name", departmentId);
        ViewBag.StatusList = new SelectList(
            Enum.GetValues(typeof(DofStatus)).Cast<DofStatus>().Select(s => new { Id = s, Name = s.ToString() }),
            "Id", "Name", status);
        ViewBag.SortOrder = sortOrder;
        ViewBag.ShowArchived = showArchived;
        ViewBag.DepartmentId = departmentId;
        ViewBag.Status = status;

        if (User.IsInRole("KaliteKontrol"))
        {
            var dept = CurrentUserDepartmentId.HasValue
                ? await _context.Departments.FindAsync(CurrentUserDepartmentId.Value)
                : null;
            ViewBag.UserDepartmentName = dept?.Name ?? User.FindFirstValue("DepartmentName") ?? "Departmanınız";
            ViewBag.IsKaliteKontrol = true;
        }

        return View(pagedDofs);
    }

    // --- Excel / PDF export ---

    public async Task<IActionResult> ExportExcel(int? departmentId, DofStatus? status, string? sortOrder, bool showArchived = false)
    {
        var dofs = await BuildDofQuery(departmentId, status, sortOrder, showArchived).ToListAsync();
        var bytes = _reportService.BuildExcel(dofs);
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"DOF-Kayitlari-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
    }

    public async Task<IActionResult> ExportPdf(int? departmentId, DofStatus? status, string? sortOrder, bool showArchived = false)
    {
        var dofs = await BuildDofQuery(departmentId, status, sortOrder, showArchived).ToListAsync();
        var title = showArchived ? "DÖF Kayıtları (Arşiv)" : "DÖF Kayıtları";
        var bytes = _reportService.BuildPdf(dofs, title);
        return File(bytes, "application/pdf", $"DOF-Kayitlari-{DateTime.Now:yyyyMMdd-HHmm}.pdf");
    }

    public async Task<IActionResult> Create()
    {
        if (User.IsInRole("Admin") || User.IsInRole("KaliteKontrol"))
        {
            TempData["ErrorMessage"] = "Yöneticiler ve Kalite Kontrol sorumluları DÖF açamaz. DÖF kayıtları yalnızca çalışanlar tarafından açılır.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name");
        ViewBag.Users = new SelectList(_context.AppUsers.OrderBy(u => u.FullName), "Id", "FullName");
        ViewBag.CurrentUserName = (await _context.AppUsers.FindAsync(CurrentUserId))?.FullName;
        return View();
    }

    // Departmanın kalite kontrol sorumlusunu döndürür (Create ekranındaki otomatik atama için)
    [HttpGet]
    public async Task<IActionResult> DepartmentResponsible(int id)
    {
        var dept = await _context.Departments
            .Include(d => d.QualityResponsibleUser)
            .FirstOrDefaultAsync(d => d.Id == id);

        return Json(new
        {
            userId = dept?.QualityResponsibleUserId,
            userName = dept?.QualityResponsibleUser?.FullName
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Dof dof, IFormFile? file)
    {
        if (User.IsInRole("Admin") || User.IsInRole("KaliteKontrol"))
        {
            TempData["ErrorMessage"] = "Yöneticiler ve Kalite Kontrol sorumluları DÖF açamaz. DÖF kayıtları yalnızca çalışanlar tarafından açılır.";
            return RedirectToAction(nameof(Index));
        }
        // Açan kişi her zaman giriş yapan kullanıcıdır
        dof.CreatedByUserId = CurrentUserId;
        ModelState.Remove(nameof(dof.CreatedByUserId));

        var department = dof.DepartmentId > 0
            ? await _context.Departments.FindAsync(dof.DepartmentId)
            : null;

        if (dof.DepartmentId > 0 && department == null)
            ModelState.AddModelError(nameof(dof.DepartmentId), "Seçilen departman bulunamadı.");

        // Atanan kişi seçilmemişse departmanın kalite kontrol sorumlusuna ata
        if ((dof.AssignedToUserId is null || dof.AssignedToUserId <= 0) && department?.QualityResponsibleUserId is int qrId)
        {
            dof.AssignedToUserId = qrId;
            ModelState.Remove(nameof(dof.AssignedToUserId));
        }

        if (dof.AssignedToUserId is int assignedId && assignedId > 0
            && !await _context.AppUsers.AnyAsync(u => u.Id == assignedId))
            ModelState.AddModelError(nameof(dof.AssignedToUserId), "Seçilen kişi bulunamadı.");

        if (dof.AssignedToUserId is null || dof.AssignedToUserId <= 0)
            ModelState.AddModelError(nameof(dof.AssignedToUserId),
                "Atanacak kişi belirlenemedi. Seçilen departmana bir kalite kontrol sorumlusu tanımlayın.");

        if (dof.DueDate.HasValue && dof.DueDate.Value.Date < DateTime.UtcNow.Date)
            ModelState.AddModelError(nameof(dof.DueDate), "Son tarih geçmiş bir tarih olamaz.");

        if (ModelState.IsValid)
        {
            _context.Dofs.Add(dof);
            await _context.SaveChangesAsync();

            // Denetim izi: kaydın ilk durumu
            _context.DofStatusHistories.Add(new DofStatusHistory
            {
                DofId = dof.Id,
                OldStatus = null,
                NewStatus = dof.Status,
                ChangedByUserId = CurrentUserId,
                Note = "Kayıt oluşturuldu"
            });
            await _context.SaveChangesAsync();

            // Varsa dosya ekini kaydet
            if (file != null && file.Length > 0)
            {
                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".docx", ".xlsx" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (allowedExtensions.Contains(extension) && file.Length <= 10 * 1024 * 1024)
                {
                    var uploadsFolder = Path.Combine("wwwroot", "uploads", "dof-attachments");
                    Directory.CreateDirectory(uploadsFolder);
                    var storedFileName = $"{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(uploadsFolder, storedFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    _context.DofAttachments.Add(new DofAttachment
                    {
                        DofId = dof.Id,
                        FileName = file.FileName,
                        StoredFileName = storedFileName,
                        UploadedByUserId = CurrentUserId
                    });
                    await _context.SaveChangesAsync();
                }
            }

            if (dof.AssignedToUserId is int notifyId)
            {
                var assignedUser = await _context.AppUsers.FindAsync(notifyId);
                if (assignedUser != null)
                {
                    await _emailService.SendEmailAsync(
                        assignedUser.Email,
                        $"Yeni DÖF Atandı: {dof.Title}",
                        $"Merhaba {assignedUser.FullName},\n\n\"{dof.Title}\" başlıklı DÖF kaydı size atandı.\n\nDetaylar için sisteme giriş yapabilirsiniz."
                    );
                }
            }

            return RedirectToAction(nameof(Index));
        }
        ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name", dof.DepartmentId);
        ViewBag.Users = new SelectList(_context.AppUsers.OrderBy(u => u.FullName), "Id", "FullName", dof.AssignedToUserId);
        ViewBag.CurrentUserName = (await _context.AppUsers.FindAsync(CurrentUserId))?.FullName;
        return View(dof);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var dof = await _context.Dofs.FindAsync(id);
        if (dof == null)
        {
            return NotFound();
        }
        ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name", dof.DepartmentId);
        ViewBag.Users = new SelectList(_context.AppUsers.OrderBy(u => u.FullName), "Id", "FullName");
        return View(dof);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Dof dof)
    {
        if (id != dof.Id)
        {
            return NotFound();
        }

        var existing = await _context.Dofs.FirstOrDefaultAsync(d => d.Id == id);
        if (existing == null)
        {
            return NotFound();
        }

        if (dof.DueDate.HasValue && dof.DueDate.Value.Date < DateTime.UtcNow.Date)
            ModelState.AddModelError(nameof(dof.DueDate), "Son tarih geçmiş bir tarih olamaz.");

        if (ModelState.IsValid)
        {
            var oldStatus = existing.Status;

            // Yalnızca düzenlenebilir alanları güncelle (CreatedAt, arşiv alanları vb. korunur)
            existing.Title = dof.Title;
            existing.Description = dof.Description;
            existing.Type = dof.Type;
            existing.Source = dof.Source;
            existing.Status = dof.Status;
            existing.DepartmentId = dof.DepartmentId;
            existing.CreatedByUserId = dof.CreatedByUserId;
            existing.AssignedToUserId = dof.AssignedToUserId;
            existing.DueDate = dof.DueDate;

            if (oldStatus != dof.Status)
            {
                // Kapanış tarihini duruma göre ayarla
                if (dof.Status == DofStatus.Kapatildi)
                    existing.ClosedAt = DateTime.UtcNow;
                else if (oldStatus == DofStatus.Kapatildi)
                    existing.ClosedAt = null;

                // Denetim izi kaydı
                _context.DofStatusHistories.Add(new DofStatusHistory
                {
                    DofId = existing.Id,
                    OldStatus = oldStatus,
                    NewStatus = dof.Status,
                    ChangedByUserId = CurrentUserId
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name", dof.DepartmentId);
        ViewBag.Users = new SelectList(_context.AppUsers.OrderBy(u => u.FullName), "Id", "FullName");
        return View(dof);
    }

    // --- Arşivleme (soft delete) ---

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Archive(int id)
    {
        var dof = await _context.Dofs
            .Include(d => d.Department)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (dof == null)
        {
            return NotFound();
        }
        return View(dof);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ActionName("Archive")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ArchiveConfirmed(int id, string? archiveReason)
    {
        var dof = await _context.Dofs.FindAsync(id);
        if (dof != null && !dof.IsArchived)
        {
            dof.IsArchived = true;
            dof.ArchivedAt = DateTime.UtcNow;
            dof.ArchivedByUserId = CurrentUserId;
            dof.ArchiveReason = string.IsNullOrWhiteSpace(archiveReason) ? null : archiveReason.Trim();
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unarchive(int id)
    {
        var dof = await _context.Dofs.FindAsync(id);
        if (dof != null && dof.IsArchived)
        {
            dof.IsArchived = false;
            dof.ArchivedAt = null;
            dof.ArchivedByUserId = null;
            dof.ArchiveReason = null;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index), new { showArchived = true });
    }

    public async Task<IActionResult> Details(int id)
    {
        var dof = await _context.Dofs
            .Include(d => d.Department)
            .Include(d => d.CreatedByUser)
            .Include(d => d.AssignedToUser)
            .Include(d => d.ArchivedByUser)
            .Include(d => d.Actions)
            .ThenInclude(a => a.ResponsibleUser)
            .Include(d => d.Attachments)
                .ThenInclude(a => a.UploadedByUser)
            .Include(d => d.Comments.OrderBy(c => c.CreatedAt))
                .ThenInclude(c => c.AuthorUser)
            .Include(d => d.StatusHistory.OrderBy(h => h.ChangedAt))
                .ThenInclude(h => h.ChangedByUser)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (dof == null)
        {
            return NotFound();
        }

        // Yetki kontrolü
        if (User.IsInRole("KaliteKontrol"))
        {
            var deptId = CurrentUserDepartmentId;
            if (deptId.HasValue && dof.DepartmentId != deptId.Value)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
        }
        else if (!User.IsInRole("Admin"))
        {
            if (dof.AssignedToUserId != CurrentUserId && dof.CreatedByUserId != CurrentUserId)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
        }

        return View(dof);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int dofId, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return RedirectToAction(nameof(Details), new { id = dofId });
        }

        var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var comment = new DofComment
        {
            DofId = dofId,
            AuthorUserId = currentUserId,
            Text = text.Trim()
        };

        _context.DofComments.Add(comment);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = dofId });
    }
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> UploadAttachment(int dofId, IFormFile file)
{
    if (file == null || file.Length == 0)
    {
        TempData["UploadError"] = "Lütfen bir dosya seçin.";
        return RedirectToAction(nameof(Details), new { id = dofId });
    }

    var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".docx", ".xlsx" };
    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (!allowedExtensions.Contains(extension))
    {
        TempData["UploadError"] = "İzin verilmeyen dosya türü. (İzin verilenler: pdf, jpg, png, docx, xlsx)";
        return RedirectToAction(nameof(Details), new { id = dofId });
    }

    const long maxSize = 10 * 1024 * 1024; // 10 MB
    if (file.Length > maxSize)
    {
        TempData["UploadError"] = "Dosya boyutu 10 MB'ı geçemez.";
        return RedirectToAction(nameof(Details), new { id = dofId });
    }

    var uploadsFolder = Path.Combine("wwwroot", "uploads", "dof-attachments");
    Directory.CreateDirectory(uploadsFolder);

    var storedFileName = $"{Guid.NewGuid()}{extension}";
    var filePath = Path.Combine(uploadsFolder, storedFileName);

    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }

    var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    var attachment = new DofAttachment
    {
        DofId = dofId,
        FileName = file.FileName,
        StoredFileName = storedFileName,
        UploadedByUserId = currentUserId
    };

    _context.DofAttachments.Add(attachment);
    await _context.SaveChangesAsync();

    return RedirectToAction(nameof(Details), new { id = dofId });
}
}
