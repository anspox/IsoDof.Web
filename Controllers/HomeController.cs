using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IsoDof.Web.Data;
using IsoDof.Web.Models;
using IsoDof.Web.Models.Entities.Enums;

namespace IsoDof.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _context;

    public HomeController(ILogger<HomeController> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var query = _context.Dofs.Include(d => d.Department).AsQueryable();

        if (!User.IsInRole("Admin"))
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            query = query.Where(d => d.AssignedToUserId == currentUserId);
        }

        var dofs = await query.ToListAsync();

        var model = new DashboardViewModel
        {
            TotalCount = dofs.Count,
            OpenCount = dofs.Count(d => d.Status != DofStatus.Kapatildi && d.Status != DofStatus.Reddedildi),
            OverdueCount = dofs.Count(d => d.IsOverdue),
            ClosedCount = dofs.Count(d => d.Status == DofStatus.Kapatildi),
            ByDepartment = dofs
                .GroupBy(d => d.Department != null ? d.Department.Name : "Bilinmiyor")
                .Select(g => new DepartmentStat { DepartmentName = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList(),
            ByStatus = dofs
                .GroupBy(d => d.Status)
                .Select(g => new StatusStat { StatusName = g.Key.ToString(), Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList()
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}