using Lyora.BLL.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lyora.Web.Controllers;

/// <summary>View Analytics Reports use case – sales, product performance and customer demographics.</summary>
public class AnalyticsController : AdminControllerBase
{
    private readonly AnalyticsService _analyticsService;

    public AnalyticsController(AnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    public IActionResult Index() => View(_analyticsService.GetReport());
}
