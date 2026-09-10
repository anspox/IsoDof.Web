using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using IsoDof.Web.Data;
using IsoDof.Web.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;

namespace IsoDof.Web.Controllers;
[Authorize(Roles = "Admin")]
public class AppUsersController : Controller
{
    private readonly AppDbContext _context;

    public AppUsersController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _context.AppUsers.Include(u => u.Department).ToListAsync();
        ViewBag.TotalDepartmentsCount = await _context.Departments.CountAsync();
        return View(users);
    }

    public IActionResult Create()
    {
        ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AppUser appUser, string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("Password", "Şifre alanı zorunludur.");
        }

        if (ModelState.IsValid)
        {
            var hasher = new PasswordHasher<AppUser>();
            appUser.PasswordHash = hasher.HashPassword(appUser, password);

            _context.AppUsers.Add(appUser);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name", appUser.DepartmentId);
        return View(appUser);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var appUser = await _context.AppUsers.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == id);
        if (appUser == null)
        {
            return NotFound();
        }
        return View(appUser);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var appUser = await _context.AppUsers.FindAsync(id);
        if (appUser != null)
        {
            _context.AppUsers.Remove(appUser);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var appUser = await _context.AppUsers.FindAsync(id);
        if (appUser == null)
        {
            return NotFound();
        }
        ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name", appUser.DepartmentId);
        return View(appUser);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AppUser appUser, string? newPassword)
    {
        if (id != appUser.Id)
        {
            return NotFound();
        }

        var existingUser = await _context.AppUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (existingUser == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            appUser.PasswordHash = existingUser.PasswordHash;
        }
        else
        {
            var hasher = new PasswordHasher<AppUser>();
            appUser.PasswordHash = hasher.HashPassword(appUser, newPassword);
        }

        if (ModelState.IsValid)
        {
            _context.Update(appUser);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name", appUser.DepartmentId);
        return View(appUser);
    }
}