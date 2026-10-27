using System.Security.Claims;
using Lyora.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lyora.Web.Controllers;

/// <summary>View Notifications – every logged-in user (customer or administrator) sees only their own.</summary>
[Authorize]
public class NotificationsController : Controller
{
    private readonly NotificationService _notificationService;

    public NotificationsController(NotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public IActionResult Index() => View(_notificationService.GetForUser(UserId));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Read(int id)
    {
        _notificationService.MarkRead(UserId, id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ReadAll()
    {
        _notificationService.MarkAllRead(UserId);
        TempData["Success"] = "All notifications marked as read.";
        return RedirectToAction(nameof(Index));
    }
}
