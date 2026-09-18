using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using IsoDof.Web.Data;
using IsoDof.Web.Models.Entities;

namespace IsoDof.Web.Controllers;

// Sık karşılaşılan DÖF konuları için hazır şablonlar. Yönetimi Admin/Kalite Kontrol yapar,
// çalışanlar Create ekranında bu şablonları seçerek formu tek tıkla doldurabilir.
public class DofTemplatesController : Controller
{
    private readonly AppDbContext _context;

    public DofTemplatesController(AppDbContext context)
    {
        _context = context;
    }

    [Authorize(Roles = "Admin,KaliteKontrol")]
    public async Task<IActionResult> Index()
    {
        var templates = await _context.DofTemplates
            .Include(t => t.Department)
            .OrderBy(t => t.Name)
            .ToListAsync();
        return View(templates);
    }

    [Authorize(Roles = "Admin,KaliteKontrol")]
    public IActionResult Create()
    {
        ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name");
        return View();
    }

    [Authorize(Roles = "Admin,KaliteKontrol")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DofTemplate template)
    {
        if (ModelState.IsValid)
        {
            _context.DofTemplates.Add(template);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name", template.DepartmentId);
        return View(template);
    }

    [Authorize(Roles = "Admin,KaliteKontrol")]
    public async Task<IActionResult> Edit(int id)
    {
        var template = await _context.DofTemplates.FindAsync(id);
        if (template == null) return NotFound();
        ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name", template.DepartmentId);
        return View(template);
    }

    [Authorize(Roles = "Admin,KaliteKontrol")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DofTemplate template)
    {
        if (id != template.Id) return NotFound();

        if (ModelState.IsValid)
        {
            _context.Update(template);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name", template.DepartmentId);
        return View(template);
    }

    [Authorize(Roles = "Admin,KaliteKontrol")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var template = await _context.DofTemplates.FindAsync(id);
        if (template != null)
        {
            _context.DofTemplates.Remove(template);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    // Create ekranında şablon seçildiğinde formu doldurmak için kullanılan JSON uç noktası.
    // Tüm giriş yapmış kullanıcılar erişebilir (çalışanlar DÖF açarken kullanır).
    [HttpGet]
    public async Task<IActionResult> Get(int id)
    {
        var t = await _context.DofTemplates.FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        if (t == null) return NotFound();

        return Json(new
        {
            title = t.TitleTemplate,
            description = t.DescriptionTemplate,
            type = (int)t.Type,
            source = (int)t.Source,
            departmentId = t.DepartmentId
        });
    }

    [HttpGet]
    public async Task<IActionResult> ActiveList()
    {
        var templates = await _context.DofTemplates
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new { id = t.Id, name = t.Name })
            .ToListAsync();
        return Json(templates);
    }
}
