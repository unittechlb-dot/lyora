using Lyora.BLL.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lyora.Web.Controllers;

/// <summary>Track Order Status use case.</summary>
public class OrdersController : CustomerControllerBase
{
    private readonly OrderService _orderService;

    public OrdersController(OrderService orderService)
    {
        _orderService = orderService;
    }

    public IActionResult Index() => View(_orderService.GetCustomerOrders(CustomerId));

    public IActionResult Details(int id)
    {
        var order = _orderService.GetCustomerOrder(CustomerId, id);
        return order == null ? NotFound() : View(order);
    }
}
