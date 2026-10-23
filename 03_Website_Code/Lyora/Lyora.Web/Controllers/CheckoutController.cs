using Lyora.BLL.Services;
using Lyora.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Lyora.Web.Controllers;

/// <summary>Place Order use case – payment is Cash on Delivery.</summary>
public class CheckoutController : CustomerControllerBase
{
    private readonly CartService _cartService;
    private readonly OrderService _orderService;
    private readonly AccountService _accountService;

    public CheckoutController(CartService cartService, OrderService orderService, AccountService accountService)
    {
        _cartService = cartService;
        _orderService = orderService;
        _accountService = accountService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var cart = _cartService.GetCart(CustomerId);
        var check = _cartService.CheckAvailability(cart);
        if (!check.Succeeded)
        {
            TempData["Error"] = check.Error;
            return RedirectToAction("Index", "Cart");
        }

        var customer = _accountService.GetCustomer(CustomerId);
        return View(new CheckoutViewModel
        {
            Cart = cart,
            FullName = customer?.FullName ?? "",
            Phone = customer?.Phone,
            DeliveryAddress = customer?.Address ?? ""
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(CheckoutViewModel model)
    {
        var cart = _cartService.GetCart(CustomerId);
        if (!ModelState.IsValid)
        {
            var customer = _accountService.GetCustomer(CustomerId);
            model.Cart = cart;
            model.FullName = customer?.FullName ?? "";
            model.Phone = customer?.Phone;
            return View(model);
        }

        var (result, order) = _orderService.PlaceOrder(CustomerId, model.DeliveryAddress);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction("Index", "Cart");
        }

        TempData["Success"] = "Thank you! Your order has been placed.";
        return RedirectToAction("Details", "Orders", new { id = order!.OrderId });
    }
}
