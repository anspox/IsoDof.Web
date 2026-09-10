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
        var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var currentUser = await _context.AppUsers.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == currentUserId);

        var query = _context.Dofs
            .Include(d => d.Department)
            .Include(d => d.AssignedToUser)
            .Include(d => d.CreatedByUser)
            .Include(d => d.Actions)
            .Where(d => !d.IsArchived);

        var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Kullanici";
        string? departmentName = currentUser?.Department?.Name;

        if (User.IsInRole("Admin"))
        {
            // Yönetici tüm şirketi görür
        }
        else if (User.IsInRole("KaliteKontrol"))
        {
            // Kalite Kontrol sorumlusu sadece kendi departmanını görür
            if (currentUser?.DepartmentId is int deptId)
            {
                query = query.Where(d => d.DepartmentId == deptId);
            }
            else
            {
                query = query.Where(d => false);
            }
        }
        else
        {
            // Çalışan kendi açtığı veya kendisine atanan DÖF'leri görür
            query = query.Where(d => d.AssignedToUserId == currentUserId || d.CreatedByUserId == currentUserId);
        }

        var dofs = await query.ToListAsync();

        var model = new DashboardViewModel
        {
            TotalCount = dofs.Count,
            OpenCount = dofs.Count(d => d.Status != DofStatus.Kapatildi && d.Status != DofStatus.Reddedildi),
            OverdueCount = dofs.Count(d => d.IsOverdue),
            ClosedCount = dofs.Count(d => d.Status == DofStatus.Kapatildi),
            UserRole = userRole,
            UserFullName = currentUser?.FullName ?? User.Identity?.Name ?? "",
            DepartmentName = departmentName,
            ByDepartment = dofs
                .GroupBy(d => d.Department != null ? d.Department.Name : "Bilinmiyor")
                .Select(g => new DepartmentStat { DepartmentName = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList(),
            ByStatus = dofs
                .GroupBy(d => d.Status)
                .Select(g => new StatusStat { StatusName = g.Key.ToString(), Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList(),
            UrgentDofs = dofs
                .Where(d => d.Status != DofStatus.Kapatildi && d.Status != DofStatus.Reddedildi)
                .OrderBy(d => d.IsOverdue ? 0 : 1)
                .ThenBy(d => d.DueDate ?? DateTime.MaxValue)
                .Take(5)
                .ToList(),
            RecentDofs = dofs
                .OrderByDescending(d => d.CreatedAt)
                .Take(5)
                .ToList(),
            MyOpenedCount = dofs.Count(d => d.CreatedByUserId == currentUserId),
            MyAssignedCount = dofs.Count(d => d.AssignedToUserId == currentUserId),
            PendingActionsCount = dofs.SelectMany(d => d.Actions).Count(a => !a.IsCompleted)
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