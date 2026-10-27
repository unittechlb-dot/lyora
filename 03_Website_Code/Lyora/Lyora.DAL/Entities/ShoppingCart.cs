namespace Lyora.DAL.Entities;

public class ShoppingCart
{
    public int CartId { get; set; }
    public int CustomerId { get; set; }
    public List<CartItem> Items { get; set; } = new();

    public decimal Total => Items.Sum(i => i.Subtotal);
    public int Bottles => Items.Sum(i => i.Quantity);
    public bool IsEmpty => Items.Count == 0;
}

public class CartItem
{
    public int CartItemId { get; set; }
    public int PerfumeId { get; set; }
    public string PerfumeName { get; set; } = "";
    public string? ImageUrl { get; set; }
    public int SizeMl { get; set; }
    /// <summary>Price paid per bottle (promotion applied) and the list price.</summary>
    public decimal UnitPrice { get; set; }
    public decimal ListPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public int Quantity { get; set; }
    public bool IsActive { get; set; }

    /// <summary>Bottles of this perfume the raw-material stock can still produce.</summary>
    public int ProducibleBottles { get; set; }

    public decimal Subtotal => UnitPrice * Quantity;
    public bool IsAvailable => IsActive && Quantity <= ProducibleBottles;
}
