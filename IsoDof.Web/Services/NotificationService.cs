using IsoDof.Web.Data;
using IsoDof.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace IsoDof.Web.Services;

public interface INotificationService
{
    Task NotifyAsync(int recipientUserId, string message, string? url = null, string icon = "notifications");
    Task NotifyManyAsync(IEnumerable<int?> recipientUserIds, string message, string? url = null, string icon = "notifications");
    Task<int> GetUnreadCountAsync(int userId);
}

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;

    public NotificationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task NotifyAsync(int recipientUserId, string message, string? url = null, string icon = "notifications")
    {
        if (recipientUserId <= 0) return;

        _context.Notifications.Add(new Notification
        {
            RecipientUserId = recipientUserId,
            Message = message,
            Url = url,
            Icon = icon
        });
        await _context.SaveChangesAsync();
    }

    public async Task NotifyManyAsync(IEnumerable<int?> recipientUserIds, string message, string? url = null, string icon = "notifications")
    {
        var ids = recipientUserIds
            .Where(id => id.HasValue && id.Value > 0)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        if (ids.Count == 0) return;

        foreach (var id in ids)
        {
            _context.Notifications.Add(new Notification
            {
                RecipientUserId = id,
                Message = message,
                Url = url,
                Icon = icon
            });
        }
        await _context.SaveChangesAsync();
    }

    public Task<int> GetUnreadCountAsync(int userId) =>
        _context.Notifications.CountAsync(n => n.RecipientUserId == userId && !n.IsRead);
}
