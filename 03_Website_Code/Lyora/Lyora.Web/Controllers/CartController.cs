using Lyora.BLL.Services;
using Lyora.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Lyora.Web.Controllers;

public class CartController : CustomerControllerBase
{
    private readonly CartService _cartService;

    public CartController(CartService cartService)
    {
        _cartService = cartService;
    }

    public IActionResult Index()
    {
        var cart = _cartService.GetCart(CustomerId);
        var check = _cartService.CheckAvailability(cart);
        return View(new CartViewModel
        {
            Cart = cart,
            CanCheckout = check.Succeeded,
            Problem = cart.IsEmpty ? null : check.Error
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Add(int perfumeId, int quantity = 1)
    {
        var result = _cartService.Add(CustomerId, perfumeId, quantity);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
            var previousPage = Request.Headers.Referer.ToString();
            return Url.IsLocalUrl(previousPage) || previousPage.StartsWith($"{Request.Scheme}://{Request.Host}/")
                ? Redirect(previousPage)
                : RedirectToAction("Index", "Perfumes");
        }

        TempData["Success"] = "Added to your cart.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Update(int cartItemId, int quantity)
    {
        var result = _cartService.UpdateQuantity(CustomerId, cartItemId, quantity);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(int cartItemId)
    {
        _cartService.Remove(CustomerId, cartItemId);
        TempData["Success"] = "Removed from your cart.";
        return RedirectToAction(nameof(Index));
    }
}
