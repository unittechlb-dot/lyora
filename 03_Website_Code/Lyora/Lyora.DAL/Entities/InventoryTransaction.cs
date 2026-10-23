namespace Lyora.DAL.Entities;

/// <summary>One raw-material stock movement: Sale (negative), Restock or CancelReturn (positive).</summary>
public class InventoryTransaction
{
    public int TransactionId { get; set; }
    public int MaterialId { get; set; }
    public int? OrderId { get; set; }
    public TransactionType Type { get; set; }
    public decimal QuantityChange { get; set; }
    public DateTime TransactionDate { get; set; }
}
