using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using IsoDof.Web.Data;
using IsoDof.Web.Models.Entities;
using IsoDof.Web.Models.Entities.Enums;
using IsoDof.Web.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace IsoDof.Web.Controllers;

public class DofsController : Controller
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;

    public DofsController(AppDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<IActionResult> Index(int? departmentId, DofStatus? status, string? sortOrder)
{
    var query = _context.Dofs
        .Include(d => d.Department)
        .Include(d => d.CreatedByUser)
        .Include(d => d.AssignedToUser)
        .AsQueryable();

    if (!User.IsInRole("Admin"))
    {
        var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        query = query.Where(d => d.AssignedToUserId == currentUserId);
    }

    if (departmentId.HasValue)
    {
        query = query.Where(d => d.DepartmentId == departmentId.Value);
    }

    if (status.HasValue)
    {
        query = query.Where(d => d.Status == status.Value);
    }

    query = sortOrder switch
    {
        "duedate_asc" => query.OrderBy(d => d.DueDate),
        "duedate_desc" => query.OrderByDescending(d => d.DueDate),
        "oldest" => query.OrderBy(d => d.CreatedAt),
        _ => query.OrderByDescending(d => d.CreatedAt),
    };

    ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name", departmentId);
    ViewBag.StatusList = new SelectList(
        Enum.GetValues(typeof(DofStatus)).Cast<DofStatus>().Select(s => new { Id = s, Name = s.ToString() }),
        "Id", "Name", status);
    ViewBag.SortOrder = sortOrder;

    var dofs = await query.ToListAsync();
    return View(dofs);
}

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Create()
    {
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
    public async Task<IActionResult> Create(Dof dof)
    {
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
        ViewBag.Users = new SelectList(_context.AppUsers, "Id", "FullName");
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

        if (dof.DueDate.HasValue && dof.DueDate.Value.Date < DateTime.UtcNow.Date)
            ModelState.AddModelError(nameof(dof.DueDate), "Son tarih geçmiş bir tarih olamaz.");

        if (ModelState.IsValid)
        {
            _context.Update(dof);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name", dof.DepartmentId);
        ViewBag.Users = new SelectList(_context.AppUsers, "Id", "FullName");
        return View(dof);
    }
    
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
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
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var dof = await _context.Dofs.FindAsync(id);
        if (dof != null)
        {
            _context.Dofs.Remove(dof);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var dof = await _context.Dofs
            .Include(d => d.Department)
            .Include(d => d.CreatedByUser)
            .Include(d => d.AssignedToUser)
            .Include(d => d.Actions)
            .ThenInclude(a => a.ResponsibleUser)
            .Include(d => d.Attachments)
                .ThenInclude(a => a.UploadedByUser)
            .Include(d => d.Comments.OrderBy(c => c.CreatedAt))
                .ThenInclude(c => c.AuthorUser)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (dof == null)
        {
            return NotFound();
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