using Lyora.BLL.Services;
using Lyora.DAL.Entities;
using Lyora.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Lyora.Web.Controllers;

/// <summary>Manage Orders + Confirm Cash Payment use cases.</summary>
public class ManageOrdersController : AdminControllerBase
{
    private readonly AdminOrderService _orderService;

    public ManageOrdersController(AdminOrderService orderService)
    {
        _orderService = orderService;
    }

    public IActionResult Index(OrderStatus? status) =>
        View(new ManageOrdersViewModel { Status = status, Orders = _orderService.GetOrders(status) });

    public IActionResult Details(int id)
    {
        var order = _orderService.GetOrder(id);
        return order == null ? NotFound() : View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Deliver(int id) =>
        Done(id, _orderService.MarkOutForDelivery(id), $"Order #{id} is out for delivery.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ConfirmPayment(int id) =>
        Done(id, _orderService.ConfirmCashPayment(id), $"Cash payment of order #{id} confirmed.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Cancel(int id) =>
        Done(id, _orderService.Cancel(id), $"Order #{id} was cancelled and its raw materials were returned to the stock.");

    private IActionResult Done(int id, ServiceResult result, string successMessage)
    {
        if (result.Succeeded)
        {
            Success(successMessage);
        }
        else
        {
            Error(result.Error!);
        }
        return RedirectToAction(nameof(Details), new { id });
    }
}
