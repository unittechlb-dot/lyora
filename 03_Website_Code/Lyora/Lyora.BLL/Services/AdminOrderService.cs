using Lyora.DAL.Entities;
using Lyora.DAL.Repositories;

namespace Lyora.BLL.Services;

/// <summary>Administrator: Manage Orders and Confirm Cash Payment.</summary>
public class AdminOrderService
{
    private readonly OrderRepository _orders;
    private readonly NotificationRepository _notifications;

    public AdminOrderService(OrderRepository orders, NotificationRepository notifications)
    {
        _orders = orders;
        _notifications = notifications;
    }

    public List<Order> GetOrders(OrderStatus? status) => _orders.GetAllOrders(status);

    public Order? GetOrder(int orderId) => _orders.GetOrder(orderId);

    public ServiceResult MarkOutForDelivery(int orderId) =>
        Change(orderId, _orders.MarkOutForDelivery, "Only a pending order can be sent out for delivery.",
            "Your order is on its way", "Order #{0} is out for delivery. Please prepare {1} in cash.");

    public ServiceResult ConfirmCashPayment(int orderId) =>
        Change(orderId, _orders.ConfirmCashPayment, "Only an order that is out for delivery can be marked as paid.",
            "Order delivered", "Order #{0} was delivered and your cash payment of {1} was received. Thank you!");

    /// <summary>Cancels the order; the database gives back exactly the raw materials that were deducted.</summary>
    public ServiceResult Cancel(int orderId) =>
        Change(orderId, _orders.Cancel, "Delivered or already cancelled orders cannot be cancelled.",
            "Order cancelled", "Order #{0} ({1}) was cancelled.");

    /// <summary>Runs the status change, then notifies the customer about it.</summary>
    private ServiceResult Change(int orderId, Func<int, int?> action, string error, string title, string message)
    {
        if (action(orderId) != null)
        {
            return ServiceResult.Fail(error);
        }

        var order = _orders.GetOrder(orderId);
        if (order != null)
        {
            var total = "$" + order.TotalAmount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            _notifications.Create(order.CustomerId, title, string.Format(message, orderId, total), NotificationType.Order);
        }
        return ServiceResult.Ok();
    }
}
