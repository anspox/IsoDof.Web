using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using IsoDof.Web.Data;
using IsoDof.Web.Models.Entities;

namespace IsoDof.Web.Controllers;

public class DofsController : Controller
{
    private readonly AppDbContext _context;

    public DofsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var dofs = await _context.Dofs
            .Include(d => d.Department)
            .Include(d => d.CreatedByUser)
            .Include(d => d.AssignedToUser)
            .ToListAsync();
        return View(dofs);
    }

    public IActionResult Create()
    {
        ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name");
        ViewBag.Users = new SelectList(_context.AppUsers, "Id", "FullName");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Dof dof)
    {
        if (dof.DepartmentId > 0 && !await _context.Departments.AnyAsync(d => d.Id == dof.DepartmentId))
            ModelState.AddModelError(nameof(dof.DepartmentId), "Seçilen departman bulunamadı.");

        if (dof.CreatedByUserId > 0 && !await _context.AppUsers.AnyAsync(u => u.Id == dof.CreatedByUserId))
            ModelState.AddModelError(nameof(dof.CreatedByUserId), "Seçilen kişi bulunamadı.");

        if (dof.AssignedToUserId is int assignedId && !await _context.AppUsers.AnyAsync(u => u.Id == assignedId))
            ModelState.AddModelError(nameof(dof.AssignedToUserId), "Seçilen kişi bulunamadı.");

        if (ModelState.IsValid)
        {
            _context.Dofs.Add(dof);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name", dof.DepartmentId);
        ViewBag.Users = new SelectList(_context.AppUsers, "Id", "FullName", dof.CreatedByUserId);
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
}