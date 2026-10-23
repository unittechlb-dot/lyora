using Lyora.DAL.Entities;
using Lyora.DAL.Repositories;

namespace Lyora.BLL.Services;

/// <summary>Administrator: Manage Orders and Confirm Cash Payment.</summary>
public class AdminOrderService
{
    private readonly OrderRepository _orders;

    public AdminOrderService(OrderRepository orders)
    {
        _orders = orders;
    }

    public List<Order> GetOrders(OrderStatus? status) => _orders.GetAllOrders(status);

    public Order? GetOrder(int orderId) => _orders.GetOrder(orderId);

    public ServiceResult MarkOutForDelivery(int orderId) =>
        ToResult(_orders.MarkOutForDelivery(orderId), "Only a pending order can be sent out for delivery.");

    public ServiceResult ConfirmCashPayment(int orderId) =>
        ToResult(_orders.ConfirmCashPayment(orderId), "Only an order that is out for delivery can be marked as paid.");

    /// <summary>Cancels the order; the database gives back exactly the raw materials that were deducted.</summary>
    public ServiceResult Cancel(int orderId) =>
        ToResult(_orders.Cancel(orderId), "Delivered or already cancelled orders cannot be cancelled.");

    private static ServiceResult ToResult(int? errorNumber, string message) =>
        errorNumber == null ? ServiceResult.Ok() : ServiceResult.Fail(message);
}
