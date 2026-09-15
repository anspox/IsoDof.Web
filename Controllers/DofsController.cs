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
    private readonly INotificationService _notifications;
    private readonly IFileStorageService _fileStorage;

    public DofsController(AppDbContext context, IEmailService emailService,
        IDofReportService reportService, INotificationService notifications,
        IFileStorageService fileStorage)
    {
        _context = context;
        _emailService = emailService;
        _reportService = reportService;
        _notifications = notifications;
        _fileStorage = fileStorage;
    }

    // --- Durum geçiş kuralları (state machine) ---
    // Anahtar: mevcut durum, Değer: geçilebilecek durumlar
    private static readonly Dictionary<DofStatus, DofStatus[]> AllowedTransitions = new()
    {
        [DofStatus.Acik] = new[] { DofStatus.Incelemede, DofStatus.Reddedildi },
        [DofStatus.Incelemede] = new[] { DofStatus.Acik, DofStatus.FaaliyetPlanlandi, DofStatus.Reddedildi },
        [DofStatus.FaaliyetPlanlandi] = new[] { DofStatus.Incelemede, DofStatus.Kapatildi },
        [DofStatus.Kapatildi] = new[] { DofStatus.Incelemede },
        [DofStatus.Reddedildi] = new[] { DofStatus.Acik },
    };

    // Durum geçişinin iş kurallarına uygun olup olmadığını doğrular.
    private bool TryValidateStatusTransition(Dof existing, DofStatus newStatus, out string? error)
    {
        error = null;
        if (existing.Status == newStatus)
            return true;

        if (!AllowedTransitions.TryGetValue(existing.Status, out var allowed) || !allowed.Contains(newStatus))
        {
            error = $"'{existing.Status}' durumundan '{newStatus}' durumuna geçiş yapılamaz.";
            return false;
        }

        if (newStatus == DofStatus.Kapatildi && existing.Actions.Any(a => !a.IsCompleted))
        {
            error = "Tamamlanmamış faaliyet adımları olan bir DÖF kapatılamaz. Önce tüm faaliyetleri tamamlayın.";
            return false;
        }

        return true;
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
    private IQueryable<Dof> BuildDofQuery(int? departmentId, DofStatus? status, string? sortOrder, bool showArchived,
        string? search = null, string? quickFilter = null)
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

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var normalizedNumber = term.TrimStart('#', 'D', 'd', '-', '0');
            query = query.Where(d =>
                d.Title.Contains(term) ||
                d.Description.Contains(term) ||
                d.Id.ToString() == term ||
                (normalizedNumber != "" && d.Id.ToString() == normalizedNumber));
        }

        switch (quickFilter)
        {
            case "mine":
                query = query.Where(d => d.AssignedToUserId == CurrentUserId);
                break;
            case "overdue":
                query = query.Where(d => d.DueDate.HasValue && DateTime.UtcNow > d.DueDate.Value
                    && d.Status != DofStatus.Kapatildi && d.Status != DofStatus.Reddedildi);
                break;
            case "open":
                query = query.Where(d => d.Status != DofStatus.Kapatildi && d.Status != DofStatus.Reddedildi);
                break;
            case "recent7":
                var since = DateTime.UtcNow.AddDays(-7);
                query = query.Where(d => d.CreatedAt >= since);
                break;
        }

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
        bool showArchived = false, int page = 1, string? search = null, string? quickFilter = null)
    {
        var query = BuildDofQuery(departmentId, status, sortOrder, showArchived, search, quickFilter);
        var pagedDofs = await PagedList<Dof>.CreateAsync(query, page, DefaultPageSize);

        ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name", departmentId);
        ViewBag.StatusList = new SelectList(
            Enum.GetValues(typeof(DofStatus)).Cast<DofStatus>().Select(s => new { Id = s, Name = s.ToString() }),
            "Id", "Name", status);
        ViewBag.SortOrder = sortOrder;
        ViewBag.ShowArchived = showArchived;
        ViewBag.DepartmentId = departmentId;
        ViewBag.Status = status;
        ViewBag.Search = search;
        ViewBag.QuickFilter = quickFilter;

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

    public async Task<IActionResult> ExportExcel(int? departmentId, DofStatus? status, string? sortOrder, bool showArchived = false,
        string? search = null, string? quickFilter = null)
    {
        var dofs = await BuildDofQuery(departmentId, status, sortOrder, showArchived, search, quickFilter).ToListAsync();
        var bytes = _reportService.BuildExcel(dofs);
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"DOF-Kayitlari-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
    }

    public async Task<IActionResult> ExportPdf(int? departmentId, DofStatus? status, string? sortOrder, bool showArchived = false,
        string? search = null, string? quickFilter = null)
    {
        var dofs = await BuildDofQuery(departmentId, status, sortOrder, showArchived, search, quickFilter).ToListAsync();
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
        ViewBag.Templates = new SelectList(
            await _context.DofTemplates.Where(t => t.IsActive).OrderBy(t => t.Name).ToListAsync(),
            "Id", "Name");
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
                var uploadResult = await _fileStorage.SaveAsync(file, "dof-attachments");
                if (uploadResult.Success)
                {
                    _context.DofAttachments.Add(new DofAttachment
                    {
                        DofId = dof.Id,
                        FileName = file.FileName,
                        StoredFileName = uploadResult.StoredFileName!,
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
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _emailService.SendEmailAsync(
                                assignedUser.Email,
                                $"Yeni DÖF Atandı: {dof.Title}",
                                $"Merhaba {assignedUser.FullName},\n\n\"{dof.Title}\" başlıklı DÖF kaydı size atandı.\n\nDetaylar için sisteme giriş yapabilirsiniz."
                            );
                        }
                        catch
                        {
                            // Background email gönderim hatası ana akışı etkilemez
                        }
                    });
                }

                if (notifyId != CurrentUserId)
                {
                    await _notifications.NotifyAsync(notifyId,
                        $"Size yeni bir DÖF atandı: {dof.Title}",
                        Url.Action(nameof(Details), "Dofs", new { id = dof.Id }),
                        "assignment");
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

        var existing = await _context.Dofs.Include(d => d.Actions).FirstOrDefaultAsync(d => d.Id == id);
        if (existing == null)
        {
            return NotFound();
        }

        if (dof.DueDate.HasValue && dof.DueDate.Value.Date < DateTime.UtcNow.Date)
            ModelState.AddModelError(nameof(dof.DueDate), "Son tarih geçmiş bir tarih olamaz.");

        if (!TryValidateStatusTransition(existing, dof.Status, out var transitionError))
            ModelState.AddModelError(nameof(dof.Status), transitionError!);

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
            existing.RootCauseCategory = dof.RootCauseCategory;
            existing.RootCauseWhy1 = dof.RootCauseWhy1;
            existing.RootCauseWhy2 = dof.RootCauseWhy2;
            existing.RootCauseWhy3 = dof.RootCauseWhy3;
            existing.RootCauseWhy4 = dof.RootCauseWhy4;
            existing.RootCauseWhy5 = dof.RootCauseWhy5;
            existing.RootCauseAnalysis = dof.RootCauseAnalysis;

            if (oldStatus != dof.Status)
            {
                // Kapanış tarihini duruma göre ayarla
                if (dof.Status == DofStatus.Kapatildi)
                {
                    existing.ClosedAt = DateTime.UtcNow;
                    existing.EffectivenessCheckDueDate = DateTime.UtcNow.AddDays(30);
                    existing.EffectivenessResult = EffectivenessResult.Beklemede;
                }
                else if (oldStatus == DofStatus.Kapatildi)
                {
                    existing.ClosedAt = null;
                    existing.EffectivenessCheckDueDate = null;
                }

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

            if (oldStatus != existing.Status)
            {
                await _notifications.NotifyManyAsync(
                    new int?[] { existing.AssignedToUserId, existing.CreatedByUserId }
                        .Where(uid => uid != CurrentUserId),
                    $"DÖF #{existing.Id} durumu güncellendi: {oldStatus} → {existing.Status}",
                    Url.Action(nameof(Details), "Dofs", new { id = existing.Id }),
                    "sync_alt");
            }

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

            await _notifications.NotifyManyAsync(
                new int?[] { dof.AssignedToUserId, dof.CreatedByUserId }
                    .Where(uid => uid != CurrentUserId),
                $"DÖF #{dof.Id} arşive taşındı: {dof.Title}",
                Url.Action(nameof(Details), "Dofs", new { id = dof.Id }),
                "archive");
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

        var dof = await _context.Dofs.FindAsync(dofId);
        if (dof != null)
        {
            await _notifications.NotifyManyAsync(
                new int?[] { dof.AssignedToUserId, dof.CreatedByUserId }
                    .Where(uid => uid != currentUserId),
                $"DÖF #{dof.Id} kaydına yeni yorum eklendi",
                Url.Action(nameof(Details), "Dofs", new { id = dof.Id }),
                "chat");
        }

        return RedirectToAction(nameof(Details), new { id = dofId });
    }
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> UploadAttachment(int dofId, IFormFile file)
{
    var uploadResult = await _fileStorage.SaveAsync(file, "dof-attachments");
    if (!uploadResult.Success)
    {
        TempData["UploadError"] = uploadResult.ErrorMessage;
        return RedirectToAction(nameof(Details), new { id = dofId });
    }

    var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    var attachment = new DofAttachment
    {
        DofId = dofId,
        FileName = file!.FileName,
        StoredFileName = uploadResult.StoredFileName!,
        UploadedByUserId = currentUserId
    };

    _context.DofAttachments.Add(attachment);
    await _context.SaveChangesAsync();

    return RedirectToAction(nameof(Details), new { id = dofId });
}

    // --- Hızlı durum aksiyonları (Details sayfasındaki tek tıkla butonlar) ---
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickStatus(int id, DofStatus newStatus)
    {
        var dof = await _context.Dofs.Include(d => d.Actions).FirstOrDefaultAsync(d => d.Id == id);
        if (dof == null)
        {
            return NotFound();
        }

        if (!TryValidateStatusTransition(dof, newStatus, out var error))
        {
            TempData["ErrorMessage"] = error;
            return RedirectToAction(nameof(Details), new { id });
        }

        var oldStatus = dof.Status;
        dof.Status = newStatus;

        if (newStatus == DofStatus.Kapatildi)
        {
            dof.ClosedAt = DateTime.UtcNow;
            dof.EffectivenessCheckDueDate = DateTime.UtcNow.AddDays(30);
            dof.EffectivenessResult = EffectivenessResult.Beklemede;
        }
        else if (oldStatus == DofStatus.Kapatildi)
        {
            dof.ClosedAt = null;
            dof.EffectivenessCheckDueDate = null;
        }

        _context.DofStatusHistories.Add(new DofStatusHistory
        {
            DofId = dof.Id,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedByUserId = CurrentUserId,
            Note = "Hızlı aksiyon ile değiştirildi"
        });

        await _context.SaveChangesAsync();

        await _notifications.NotifyManyAsync(
            new int?[] { dof.AssignedToUserId, dof.CreatedByUserId }
                .Where(uid => uid != CurrentUserId),
            $"DÖF #{dof.Id} durumu güncellendi: {oldStatus} → {newStatus}",
            Url.Action(nameof(Details), "Dofs", new { id = dof.Id }),
            "sync_alt");

        return RedirectToAction(nameof(Details), new { id });
    }

    // --- Etkinlik Takibi: kapatılan DÖF'ün alınan önlemin kalıcı olup olmadığının doğrulanması ---
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EvaluateEffectiveness(int id, EffectivenessResult result, string? notes)
    {
        var dof = await _context.Dofs.FirstOrDefaultAsync(d => d.Id == id);
        if (dof == null || dof.Status != DofStatus.Kapatildi)
        {
            return NotFound();
        }

        dof.EffectivenessResult = result;
        dof.EffectivenessNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        dof.EffectivenessCheckedAt = DateTime.UtcNow;
        dof.EffectivenessCheckedByUserId = CurrentUserId;

        await _context.SaveChangesAsync();

        if (result == EffectivenessResult.TekrarEtti)
        {
            await _notifications.NotifyManyAsync(
                new int?[] { dof.AssignedToUserId, dof.CreatedByUserId }
                    .Where(uid => uid != CurrentUserId),
                $"DÖF #{dof.Id} için etkinlik değerlendirmesi: Sorun tekrar etti, yeniden değerlendirme gerekebilir.",
                Url.Action(nameof(Details), "Dofs", new { id = dof.Id }),
                "warning");
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    // --- Kanban görünümü ---
    public async Task<IActionResult> Kanban(int? departmentId)
    {
        var query = BuildDofQuery(departmentId, null, null, false);
        var dofs = await query.ToListAsync();

        ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name", departmentId);
        ViewBag.DepartmentId = departmentId;

        return View(dofs);
    }
}
