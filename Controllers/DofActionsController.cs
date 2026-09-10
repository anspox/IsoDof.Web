using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using IsoDof.Web.Data;
using IsoDof.Web.Models.Entities;

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
    public async Task<IActionResult> Create(DofAction dofAction)
    {
        var currentUser = await _context.AppUsers.FindAsync(CurrentUserId);

        // Kullanıcı yalnızca kapsamındaki DÖF kayıtlarına faaliyet ekleyebilir
        var targetDof = await AssignableDofsQuery(currentUser).FirstOrDefaultAsync(d => d.Id == dofAction.DofId);
        if (targetDof == null)
        {
            TempData["ErrorMessage"] = User.IsInRole("Admin") || User.IsInRole("KaliteKontrol")
                ? "Seçilen DÖF kaydına faaliyet ekleyemezsiniz."
                : "Yalnızca kendi açtığınız DÖF kayıtlarına faaliyet adımı ekleyebilirsiniz.";
            return RedirectToAction(nameof(Index));
        }

        // Sorumlu kişi, seçilen DÖF'ün departmanında olmalı
        if (dofAction.ResponsibleUserId is int respId && respId > 0)
        {
            var respInDept = await _context.AppUsers
                .AnyAsync(u => u.Id == respId && u.DepartmentId == targetDof.DepartmentId);
            if (!respInDept)
            {
                TempData["ErrorMessage"] = "Sorumlu kişi, seçilen DÖF'ün departmanında yer alan bir kullanıcı olmalıdır.";
                return RedirectToAction(nameof(Index));
            }
        }

        if (ModelState.IsValid)
        {
            _context.DofActions.Add(dofAction);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
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
    public async Task<IActionResult> Complete(int id)
    {
        var action = await _context.DofActions.FindAsync(id);
        if (action != null)
        {
            action.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
        public async Task<IActionResult> Edit(int id)
    {
        var dofAction = await _context.DofActions.FindAsync(id);
        if (dofAction == null)
        {
            return NotFound();
        }
        ViewBag.Dofs = new SelectList(_context.Dofs, "Id", "Title", dofAction.DofId);
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

        if (ModelState.IsValid)
        {
            _context.Update(dofAction);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Dofs = new SelectList(_context.Dofs, "Id", "Title", dofAction.DofId);
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
        return View(dofAction);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var dofAction = await _context.DofActions.FindAsync(id);
        if (dofAction != null)
        {
            _context.DofActions.Remove(dofAction);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}