using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IsoDof.Web.Data;
using IsoDof.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IsoDof.Web.Controllers;

[Authorize(Roles = "Admin")]
public class DepartmentsController : Controller
{
    private readonly AppDbContext _context;

    public DepartmentsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var departments = await _context.Departments
            .Include(d => d.QualityResponsibleUser)
            .ToListAsync();
        return View(departments);
    }

    private void PopulateUsers(object? selected = null)
    {
        ViewBag.Users = new SelectList(_context.AppUsers.OrderBy(u => u.FullName), "Id", "FullName", selected);
    }

    public IActionResult Create()
    {
        PopulateUsers();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Department department)
    {
        if (department.QualityResponsibleUserId is int qrId && !await _context.AppUsers.AnyAsync(u => u.Id == qrId))
            ModelState.AddModelError(nameof(department.QualityResponsibleUserId), "Seçilen kişi bulunamadı.");

        if (ModelState.IsValid)
        {
            _context.Departments.Add(department);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        PopulateUsers(department.QualityResponsibleUserId);
        return View(department);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department == null)
        {
            return NotFound();
        }
        PopulateUsers(department.QualityResponsibleUserId);
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

        if (department.QualityResponsibleUserId is int qrId && !await _context.AppUsers.AnyAsync(u => u.Id == qrId))
            ModelState.AddModelError(nameof(department.QualityResponsibleUserId), "Seçilen kişi bulunamadı.");

        if (ModelState.IsValid)
        {
            _context.Update(department);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        PopulateUsers(department.QualityResponsibleUserId);
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