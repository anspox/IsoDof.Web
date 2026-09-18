using System.Security.Claims;
using IsoDof.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace IsoDof.Web.ViewComponents;

public class NotificationBellViewComponent : ViewComponent
{
    private readonly INotificationService _notifications;

    public NotificationBellViewComponent(INotificationService notifications)
    {
        _notifications = notifications;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var idClaim = ((ClaimsPrincipal)User).FindFirstValue(ClaimTypes.NameIdentifier);
        var unread = 0;
        if (int.TryParse(idClaim, out var userId))
        {
            unread = await _notifications.GetUnreadCountAsync(userId);
        }
        return View(unread);
    }
}
