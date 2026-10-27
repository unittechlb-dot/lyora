using System.Security.Claims;
using Lyora.BLL.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lyora.Web.Controllers;

/// <summary>Administrator dashboard.</summary>
public class AdminController : AdminControllerBase
{
    private readonly DashboardService _dashboardService;
    private readonly NotificationService _notificationService;

    public AdminController(DashboardService dashboardService, NotificationService notificationService)
    {
        _dashboardService = dashboardService;
        _notificationService = notificationService;
    }

    public IActionResult Index()
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        ViewBag.UnreadAlerts = _notificationService.CountUnreadLowStockAlerts(adminId);
        return View(_dashboardService.GetSummary());
    }
}
