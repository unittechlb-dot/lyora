using Lyora.BLL.Services;
using Lyora.DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lyora.Web.Controllers;

/// <summary>Administrator area – only users with the Administrator role can open it.</summary>
[Authorize(Roles = Roles.Administrator)]
public class AdminController : Controller
{
    private readonly DashboardService _dashboardService;

    public AdminController(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public IActionResult Index() => View(_dashboardService.GetSummary());
}
