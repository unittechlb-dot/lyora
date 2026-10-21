using System.Diagnostics;
using Lyora.BLL.Services;
using Lyora.Web.Models;
using Lyora.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Lyora.Web.Controllers;

public class HomeController : Controller
{
    private readonly PerfumeService _perfumeService;

    public HomeController(PerfumeService perfumeService)
    {
        _perfumeService = perfumeService;
    }

    public IActionResult Index()
    {
        var model = new HomeViewModel
        {
            Featured = _perfumeService.GetFeatured(3),
            Categories = _perfumeService.GetCategories()
        };
        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
