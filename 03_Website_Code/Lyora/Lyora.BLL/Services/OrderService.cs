using Lyora.DAL.Entities;
using Lyora.DAL.Repositories;

namespace Lyora.BLL.Services;

/// <summary>Place Order and Track Order Status use cases.</summary>
public class OrderService
{
    private const int MaxAddressLength = 250;

    private readonly OrderRepository _orders;

    public OrderService(OrderRepository orders)
    {
        _orders = orders;
    }

    /// <summary>
    /// Places the order (Cash on Delivery). The database procedure deducts the raw materials
    /// of every perfume according to its BOM and creates a low-stock alert for the administrator.
    /// </summary>
    public (ServiceResult Result, PlaceOrderResult? Order) PlaceOrder(int customerId, string? deliveryAddress)
    {
        var address = deliveryAddress?.Trim();
        if (string.IsNullOrEmpty(address))
        {
            return (ServiceResult.Fail("Please enter the delivery address."), null);
        }
        if (address.Length > MaxAddressLength)
        {
            return (ServiceResult.Fail($"The address must be {MaxAddressLength} characters or less."), null);
        }

        var order = _orders.PlaceOrder(customerId, address);
        if (order.Succeeded)
        {
            return (ServiceResult.Ok(), order);
        }

        var message = order.ErrorNumber switch
        {
            PlaceOrderResult.CartEmpty => "Your cart is empty.",
            PlaceOrderResult.MissingBom => "A perfume in your cart cannot be produced yet. Please remove it.",
            PlaceOrderResult.NotEnoughStock => "Sorry, there are not enough raw materials for this order anymore. Please reduce some quantities.",
            _ => "The order could not be placed. Please try again."
        };
        return (ServiceResult.Fail(message), order);
    }

    public List<Order> GetCustomerOrders(int customerId) => _orders.GetCustomerOrders(customerId);

    public Order? GetCustomerOrder(int customerId, int orderId) => _orders.GetCustomerOrder(customerId, orderId);
}
