using Lyora.BLL.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lyora.Web.Controllers;

/// <summary>Administrator dashboard.</summary>
public class AdminController : AdminControllerBase
{
    private readonly DashboardService _dashboardService;

    public AdminController(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public IActionResult Index() => View(_dashboardService.GetSummary());
}
