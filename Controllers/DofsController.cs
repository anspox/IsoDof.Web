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
        if (ModelState.IsValid)
        {
            _context.Dofs.Add(dof);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name", dof.DepartmentId);
        ViewBag.Users = new SelectList(_context.AppUsers, "Id", "FullName");
        return View(dof);
    }
}