using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using IsoDof.Web.Data;
using IsoDof.Web.Models.Entities;
using IsoDof.Web.Services;

namespace IsoDof.Web.Controllers;

public class DofActionsController : Controller
{
    private readonly AppDbContext _context;

    public DofActionsController(AppDbContext context)
    {
        _context = context;
    }

    private int CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : 0;

    /// <summary>
    /// Faaliyet eklenebilecek DÖF kayıtları (role göre kapsam):
    /// Admin tümünü, Kalite Kontrol kendi departmanını, standart kullanıcı ise
    /// yalnızca kendi açtığı DÖF kayıtlarını görebilir.
    /// </summary>
    private IQueryable<Dof> AssignableDofsQuery(AppUser? currentUser)
    {
        var query = _context.Dofs.Where(d => !d.IsArchived);

        if (User.IsInRole("Admin"))
            return query;

        if (User.IsInRole("KaliteKontrol"))
        {
            var deptId = currentUser?.DepartmentId;
            return deptId.HasValue ? query.Where(d => d.DepartmentId == deptId.Value) : query.Where(d => false);
        }

        var userId = CurrentUserId;
        return query.Where(d => d.CreatedByUserId == userId);
    }

    /// <summary>Faaliyetin bağlı olduğu DÖF'e erişimi olan kullanıcılar faaliyeti yönetebilir.</summary>
    private async Task<bool> CanManageDofAsync(int dofId)
    {
        var dof = await _context.Dofs.AsNoTracking().FirstOrDefaultAsync(d => d.Id == dofId);
        if (dof == null) return false;
        var deptId = (await _context.AppUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Id == CurrentUserId))?.DepartmentId;
        return DofAccess.CanAccess(User, dof, CurrentUserId, deptId);
    }

    public async Task<IActionResult> Index()
    {
        var currentUserIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        int currentUserId = int.TryParse(currentUserIdStr, out var uid) ? uid : 0;
        var currentUser = await _context.AppUsers.FindAsync(currentUserId);

        var query = _context.DofActions
            .Include(a => a.Dof)
                .ThenInclude(d => d!.Department)
            .Include(a => a.ResponsibleUser)
            .AsQueryable();

        if (User.IsInRole("Admin"))
        {
            // Admin sees all
        }
        else if (User.IsInRole("KaliteKontrol") && currentUser?.DepartmentId is int deptId)
        {
            query = query.Where(a => a.Dof != null && a.Dof.DepartmentId == deptId);
        }
        else
        {
            query = query.Where(a => a.ResponsibleUserId == currentUserId || (a.Dof != null && (a.Dof.CreatedByUserId == currentUserId || a.Dof.AssignedToUserId == currentUserId)));
        }

        var actions = await query.OrderByDescending(a => a.Id).ToListAsync();

        ViewBag.Dofs = new SelectList(
            await AssignableDofsQuery(currentUser).OrderByDescending(d => d.Id).ToListAsync(),
            "Id", "Title");
        ViewBag.Users = new SelectList(_context.AppUsers.OrderBy(u => u.FullName), "Id", "FullName");

        return View(actions);
    }

    public async Task<IActionResult> Create()
    {
        var currentUser = await _context.AppUsers.FindAsync(CurrentUserId);
        ViewBag.Dofs = new SelectList(
            await AssignableDofsQuery(currentUser).OrderByDescending(d => d.Id).ToListAsync(),
            "Id", "Title");
        ViewBag.Users = new SelectList(_context.AppUsers.OrderBy(u => u.FullName), "Id", "FullName");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DofAction dofAction, string? returnUrl = null)
    {
        var currentUser = await _context.AppUsers.FindAsync(CurrentUserId);
        IActionResult BackOrIndex() => !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));

        // Kullanıcı yalnızca kapsamındaki DÖF kayıtlarına faaliyet ekleyebilir
        var targetDof = await AssignableDofsQuery(currentUser).FirstOrDefaultAsync(d => d.Id == dofAction.DofId);
        if (targetDof == null)
        {
            TempData["ErrorMessage"] = User.IsInRole("Admin") || User.IsInRole("KaliteKontrol")
                ? "Seçilen DÖF kaydına faaliyet ekleyemezsiniz."
                : "Yalnızca kendi açtığınız DÖF kayıtlarına faaliyet adımı ekleyebilirsiniz.";
            return BackOrIndex();
        }

        // Sorumlu kişi, seçilen DÖF'ün departmanında olmalı
        if (dofAction.ResponsibleUserId is int respId && respId > 0)
        {
            var respInDept = await _context.AppUsers
                .AnyAsync(u => u.Id == respId && u.DepartmentId == targetDof.DepartmentId);
            if (!respInDept)
            {
                TempData["ErrorMessage"] = "Sorumlu kişi, seçilen DÖF'ün departmanında yer alan bir kullanıcı olmalıdır.";
                return BackOrIndex();
            }
        }

        if (ModelState.IsValid)
        {
            _context.DofActions.Add(dofAction);
            await _context.SaveChangesAsync();
            return BackOrIndex();
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            TempData["ErrorMessage"] = "Faaliyet adımı eklenemedi. Lütfen açıklama alanını kontrol edin.";
            return Redirect(returnUrl);
        }

        ViewBag.Dofs = new SelectList(
            await AssignableDofsQuery(currentUser).OrderByDescending(d => d.Id).ToListAsync(),
            "Id", "Title");
        ViewBag.Users = new SelectList(_context.AppUsers, "Id", "FullName");
        return View(dofAction);
    }

    // Seçilen DÖF'ün departmanındaki kullanıcılar (modal "Sorumlu Kişi" listesi için)
    [HttpGet]
    public async Task<IActionResult> DepartmentUsers(int dofId)
    {
        var currentUser = await _context.AppUsers.FindAsync(CurrentUserId);
        var dof = await AssignableDofsQuery(currentUser).FirstOrDefaultAsync(d => d.Id == dofId);
        if (dof == null)
            return Json(Array.Empty<object>());

        var users = await _context.AppUsers
            .Where(u => u.DepartmentId == dof.DepartmentId)
            .OrderBy(u => u.FullName)
            .Select(u => new { id = u.Id, fullName = u.FullName })
            .ToListAsync();

        return Json(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, string? returnUrl = null)
    {
        var action = await _context.DofActions.FindAsync(id);
        if (action != null)
        {
            // Faaliyetin sorumlusu veya DÖF'e erişimi olan kullanıcı tamamlayabilir.
            if (action.ResponsibleUserId != CurrentUserId && !await CanManageDofAsync(action.DofId))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            action.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(nameof(Index));
    }

    // Düzenleme ekranındaki DÖF listesi: kullanıcının kapsamındaki kayıtlar + faaliyetin mevcut DÖF'ü
    private async Task<SelectList> EditableDofsSelectListAsync(int currentDofId)
    {
        var currentUser = await _context.AppUsers.FindAsync(CurrentUserId);
        var dofs = await AssignableDofsQuery(currentUser)
            .Union(_context.Dofs.Where(d => d.Id == currentDofId))
            .OrderByDescending(d => d.Id)
            .ToListAsync();
        return new SelectList(dofs, "Id", "Title", currentDofId);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var dofAction = await _context.DofActions.FindAsync(id);
        if (dofAction == null)
        {
            return NotFound();
        }
        if (!await CanManageDofAsync(dofAction.DofId))
        {
            return RedirectToAction("AccessDenied", "Account");
        }
        ViewBag.Dofs = await EditableDofsSelectListAsync(dofAction.DofId);
        ViewBag.Users = new SelectList(_context.AppUsers, "Id", "FullName", dofAction.ResponsibleUserId);
        return View(dofAction);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DofAction dofAction)
    {
        if (id != dofAction.Id)
        {
            return NotFound();
        }

        var existing = await _context.DofActions.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
        if (existing == null)
        {
            return NotFound();
        }
        // Hem mevcut hem de (değiştirildiyse) hedef DÖF kullanıcının kapsamında olmalı.
        if (!await CanManageDofAsync(existing.DofId) ||
            (dofAction.DofId != existing.DofId && !await CanManageDofAsync(dofAction.DofId)))
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (ModelState.IsValid)
        {
            _context.Update(dofAction);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Dofs = await EditableDofsSelectListAsync(existing.DofId);
        ViewBag.Users = new SelectList(_context.AppUsers, "Id", "FullName", dofAction.ResponsibleUserId);
        return View(dofAction);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var dofAction = await _context.DofActions
            .Include(a => a.Dof)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (dofAction == null)
        {
            return NotFound();
        }
        if (!await CanManageDofAsync(dofAction.DofId))
        {
            return RedirectToAction("AccessDenied", "Account");
        }
        return View(dofAction);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var dofAction = await _context.DofActions.FindAsync(id);
        if (dofAction != null)
        {
            if (!await CanManageDofAsync(dofAction.DofId))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            _context.DofActions.Remove(dofAction);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}