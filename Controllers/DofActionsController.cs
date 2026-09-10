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

        ViewBag.Dofs = new SelectList(_context.Dofs.Where(d => !d.IsArchived).OrderByDescending(d => d.Id), "Id", "Title");
        ViewBag.Users = new SelectList(_context.AppUsers.OrderBy(u => u.FullName), "Id", "FullName");

        return View(actions);
    }

    public IActionResult Create()
    {
        ViewBag.Dofs = new SelectList(_context.Dofs, "Id", "Title");
        ViewBag.Users = new SelectList(_context.AppUsers, "Id", "FullName");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DofAction dofAction)
    {
        if (ModelState.IsValid)
        {
            _context.DofActions.Add(dofAction);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Dofs = new SelectList(_context.Dofs, "Id", "Title");
        ViewBag.Users = new SelectList(_context.AppUsers, "Id", "FullName");
        return View(dofAction);
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