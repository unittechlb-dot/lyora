using Lyora.BLL.Services;
using Lyora.DAL.Entities;
using Lyora.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Lyora.Web.Controllers;

/// <summary>Browsing perfumes – open to everyone, including visitors who are not logged in.</summary>
public class PerfumesController : Controller
{
    private readonly PerfumeService _perfumeService;

    public PerfumesController(PerfumeService perfumeService)
    {
        _perfumeService = perfumeService;
    }

    // GET /Perfumes?search=rose&categoryId=1&intensity=Light&style=Natural
    public IActionResult Index(string? search, int? categoryId, ScentIntensity? intensity, ScentStyle? style)
    {
        var model = new CatalogViewModel
        {
            Search = search,
            CategoryId = categoryId,
            Intensity = intensity,
            Style = style,
            Categories = _perfumeService.GetCategories(),
            Perfumes = _perfumeService.Search(search, categoryId, intensity, style)
        };
        return View(model);
    }

    // GET /Perfumes/Details/4
    public IActionResult Details(int id)
    {
        var perfume = _perfumeService.GetPerfume(id);
        if (perfume == null)
        {
            return NotFound();
        }

        var model = new PerfumeDetailsViewModel
        {
            Perfume = perfume,
            Ingredients = _perfumeService.GetIngredients(id)
        };
        return View(model);
    }
}
