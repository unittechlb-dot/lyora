using Lyora.BLL.Services;
using Lyora.DAL.Entities;
using Lyora.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Lyora.Web.Controllers;

/// <summary>Manage Discounts &amp; Promotions + Send Targeted Notifications use cases.</summary>
public class ManagePromotionsController : AdminControllerBase
{
    private readonly PromotionService _promotionService;
    private readonly ProductService _productService;

    public ManagePromotionsController(PromotionService promotionService, ProductService productService)
    {
        _promotionService = promotionService;
        _productService = productService;
    }

    public IActionResult Index() => View(_promotionService.GetPromotions());

    [HttpGet]
    public IActionResult Create() => View("Form", Fill(new PromotionFormViewModel
    {
        StartDate = DateTime.Today,
        EndDate = DateTime.Today.AddDays(14)
    }));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(PromotionFormViewModel model) => Save(model);

    [HttpGet]
    public IActionResult Edit(int id)
    {
        var p = _promotionService.GetPromotion(id);
        if (p == null)
        {
            return NotFound();
        }
        return View("Form", Fill(new PromotionFormViewModel
        {
            PromotionId = p.PromotionId,
            Title = p.Title,
            DiscountPercent = p.DiscountPercent,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            PerfumeIds = p.PerfumeIds
        }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, PromotionFormViewModel model)
    {
        if (_promotionService.GetPromotion(id) == null)
        {
            return NotFound();
        }
        model.PromotionId = id;
        return Save(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        _promotionService.Delete(id);
        Success("Promotion deleted. Prices are back to normal.");
        return RedirectToAction(nameof(Index));
    }

    // ---------- Send the promotion to customers ----------

    [HttpGet]
    public IActionResult Send(int id)
    {
        var promotion = _promotionService.GetPromotion(id);
        return promotion == null ? NotFound() : View(new SendPromotionViewModel { Promotion = promotion });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Send(int id, SendPromotionViewModel model)
    {
        var (result, sent) = _promotionService.Send(id, model.Audience, model.AgeGroup, model.Gender);
        if (!result.Succeeded)
        {
            Error(result.Error!);
            return RedirectToAction(nameof(Send), new { id });
        }

        Success(sent == 0
            ? "No customer matches this audience – nothing was sent."
            : $"Promotion sent to {sent} customer{(sent == 1 ? "" : "s")}.");
        return RedirectToAction(nameof(Index));
    }

    // ---------- helpers ----------

    private IActionResult Save(PromotionFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Form", Fill(model));
        }

        var promotion = new Promotion
        {
            PromotionId = model.PromotionId,
            Title = model.Title,
            DiscountPercent = model.DiscountPercent,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            PerfumeIds = model.PerfumeIds
        };
        var result = _promotionService.Save(promotion);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View("Form", Fill(model));
        }

        Success(promotion.IsActive
            ? "Promotion saved – the discount is applied now in the shop, the cart and new orders."
            : "Promotion saved – the discount will be applied automatically on the start date.");
        return RedirectToAction(nameof(Index));
    }

    private PromotionFormViewModel Fill(PromotionFormViewModel model)
    {
        model.AllPerfumes = _productService.GetPerfumes();
        return model;
    }
}
