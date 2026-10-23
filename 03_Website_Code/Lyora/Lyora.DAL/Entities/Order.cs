namespace Lyora.DAL.Entities;

public class Order
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string? CustomerPhone { get; set; }
    public DateTime OrderDate { get; set; }
    public OrderStatus Status { get; set; }
    public string DeliveryAddress { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public int Bottles { get; set; }

    public string PaymentMethod { get; set; } = "Cash on Delivery";
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }

    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public int OrderItemId { get; set; }
    public int PerfumeId { get; set; }
    public string PerfumeName { get; set; } = "";
    public string? ImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public decimal Subtotal => UnitPrice * Quantity;
}
