using System.Data;
using Lyora.DAL.Entities;
using Microsoft.Data.SqlClient;

namespace Lyora.DAL.Repositories;

public class OrderRepository
{
    private readonly Db _db;

    public OrderRepository(Db db)
    {
        _db = db;
    }

    /// <summary>
    /// Calls sp_PlaceOrder: in ONE transaction it checks the stock, creates the order and its
    /// cash-on-delivery payment, deducts the raw materials by BOM and empties the cart.
    /// </summary>
    public PlaceOrderResult PlaceOrder(int customerId, string deliveryAddress)
    {
        using var connection = _db.Open();
        using var command = new SqlCommand("dbo.sp_PlaceOrder", connection) { CommandType = CommandType.StoredProcedure };
        command.Parameters.AddWithValue("@CustomerId", customerId);
        command.Parameters.AddWithValue("@DeliveryAddress", deliveryAddress);
        var orderIdParameter = command.Parameters.Add("@OrderId", SqlDbType.Int);
        orderIdParameter.Direction = ParameterDirection.Output;

        var result = new PlaceOrderResult();
        try
        {
            using (var reader = command.ExecuteReader())
            {
                // The procedure returns the raw materials that just went below their threshold.
                while (reader.Read())
                {
                    result.LowStockMaterials.Add(new RawMaterial
                    {
                        MaterialId = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Unit = reader.GetString(2),
                        QuantityInStock = reader.GetDecimal(3),
                        Threshold = reader.GetDecimal(4)
                    });
                }
            }
            result.OrderId = (int)orderIdParameter.Value;
        }
        catch (SqlException ex) when (ex.Number is PlaceOrderResult.CartEmpty or PlaceOrderResult.MissingBom or PlaceOrderResult.NotEnoughStock)
        {
            result.ErrorNumber = ex.Number;
        }
        return result;
    }

    public List<Order> GetCustomerOrders(int customerId)
    {
        const string sql = @"
        SELECT o.OrderId, o.CustomerId, o.OrderDate, o.Status, o.DeliveryAddress, o.TotalAmount,
               (SELECT ISNULL(SUM(Quantity), 0) FROM dbo.OrderItems WHERE OrderId = o.OrderId) AS Bottles,
               p.Method, p.IsReceived, p.ReceivedAt
        FROM dbo.Orders o
        LEFT JOIN dbo.Payments p ON p.OrderId = o.OrderId
        WHERE o.CustomerId = @CustomerId
        ORDER BY o.OrderDate DESC;";

        using var connection = _db.Open();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@CustomerId", customerId);

        var orders = new List<Order>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            orders.Add(ReadOrder(reader));
        }
        return orders;
    }

    /// <summary>One order of this customer with its lines, or null when it does not belong to the customer.</summary>
    public Order? GetCustomerOrder(int customerId, int orderId)
    {
        const string sql = @"
        SELECT o.OrderId, o.CustomerId, o.OrderDate, o.Status, o.DeliveryAddress, o.TotalAmount,
               (SELECT ISNULL(SUM(Quantity), 0) FROM dbo.OrderItems WHERE OrderId = o.OrderId) AS Bottles,
               p.Method, p.IsReceived, p.ReceivedAt
        FROM dbo.Orders o
        LEFT JOIN dbo.Payments p ON p.OrderId = o.OrderId
        WHERE o.OrderId = @OrderId AND o.CustomerId = @CustomerId;

        SELECT oi.OrderItemId, oi.PerfumeId, pf.Name, pf.ImageUrl, oi.Quantity, oi.UnitPrice
        FROM dbo.OrderItems oi
        JOIN dbo.Orders o    ON o.OrderId = oi.OrderId
        JOIN dbo.Perfumes pf ON pf.PerfumeId = oi.PerfumeId
        WHERE oi.OrderId = @OrderId AND o.CustomerId = @CustomerId
        ORDER BY oi.OrderItemId;";

        using var connection = _db.Open();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@CustomerId", customerId);
        command.Parameters.AddWithValue("@OrderId", orderId);
        using var reader = command.ExecuteReader();

        if (!reader.Read())
        {
            return null;
        }
        var order = ReadOrder(reader);

        reader.NextResult();
        while (reader.Read())
        {
            order.Items.Add(new OrderItem
            {
                OrderItemId = reader.GetInt32(0),
                PerfumeId = reader.GetInt32(1),
                PerfumeName = reader.GetString(2),
                ImageUrl = reader.IsDBNull(3) ? null : reader.GetString(3),
                Quantity = reader.GetInt32(4),
                UnitPrice = reader.GetDecimal(5)
            });
        }
        return order;
    }

    private static Order ReadOrder(SqlDataReader reader) => new()
    {
        OrderId = reader.GetInt32(0),
        CustomerId = reader.GetInt32(1),
        OrderDate = reader.GetDateTime(2),
        Status = Enum.Parse<OrderStatus>(reader.GetString(3)),
        DeliveryAddress = reader.GetString(4),
        TotalAmount = reader.GetDecimal(5),
        Bottles = reader.GetInt32(6),
        PaymentMethod = reader.IsDBNull(7) ? "Cash on Delivery" : reader.GetString(7),
        IsPaid = !reader.IsDBNull(8) && reader.GetBoolean(8),
        PaidAt = reader.IsDBNull(9) ? null : reader.GetDateTime(9)
    };
}
