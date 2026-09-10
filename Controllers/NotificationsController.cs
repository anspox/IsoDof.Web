using System.Security.Claims;
using IsoDof.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IsoDof.Web.Controllers;

public class NotificationsController : Controller
{
    private readonly AppDbContext _context;

    public NotificationsController(AppDbContext context)
    {
        _context = context;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index()
    {
        var items = await _context.Notifications
            .Where(n => n.RecipientUserId == CurrentUserId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .ToListAsync();
        return View(items);
    }

    // Çan menüsü için JSON: okunmamış sayısı + son bildirimler
    [HttpGet]
    public async Task<IActionResult> Recent()
    {
        var uid = CurrentUserId;

        var unreadCount = await _context.Notifications
            .CountAsync(n => n.RecipientUserId == uid && !n.IsRead);

        var recent = await _context.Notifications
            .Where(n => n.RecipientUserId == uid)
            .OrderByDescending(n => n.CreatedAt)
            .Take(8)
            .Select(n => new
            {
                n.Id,
                n.Message,
                n.Url,
                n.Icon,
                n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        return Json(new
        {
            unreadCount,
            items = recent.Select(n => new
            {
                n.Id,
                n.Message,
                url = string.IsNullOrEmpty(n.Url) ? Url.Action("Index", "Notifications") : n.Url,
                n.Icon,
                n.IsRead,
                ago = TimeAgo(n.CreatedAt)
            })
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var uid = CurrentUserId;
        await _context.Notifications
            .Where(n => n.RecipientUserId == uid && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        return RedirectToAction(nameof(Index));
    }

    // Bildirime tıklandığında: okundu işaretle ve hedefe yönlendir
    [HttpGet]
    public async Task<IActionResult> Open(int id)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.RecipientUserId == CurrentUserId);

        if (notification == null)
            return RedirectToAction(nameof(Index));

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }

        if (!string.IsNullOrEmpty(notification.Url) && Url.IsLocalUrl(notification.Url))
            return Redirect(notification.Url);

        return RedirectToAction(nameof(Index));
    }

    private static string TimeAgo(DateTime utc)
    {
        var span = DateTime.UtcNow - utc;
        if (span.TotalMinutes < 1) return "az önce";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} dk önce";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours} sa önce";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays} gün önce";
        return utc.ToLocalTime().ToString("dd.MM.yyyy");
    }
}
