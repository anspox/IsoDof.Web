using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IsoDof.Web.Data;
using IsoDof.Web.Models.Entities;

namespace IsoDof.Web.Controllers;

public class DepartmentsController : Controller
{
    private readonly AppDbContext _context;

    public DepartmentsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var departments = await _context.Departments.ToListAsync();
        return View(departments);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Department department)
    {
        if (ModelState.IsValid)
        {
            _context.Departments.Add(department);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(department);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department == null)
        {
            return NotFound();
        }
        return View(department);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Department department)
    {
        if (id != department.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            _context.Update(department);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(department);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department == null)
        {
            return NotFound();
        }
        return View(department);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department != null)
        {
            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}