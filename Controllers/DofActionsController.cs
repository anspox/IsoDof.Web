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
        var actions = await _context.DofActions
            .Include(a => a.Dof)
            .Include(a => a.ResponsibleUser)
            .ToListAsync();
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
}