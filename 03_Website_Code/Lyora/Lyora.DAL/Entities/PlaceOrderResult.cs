namespace Lyora.DAL.Entities;

/// <summary>What sp_PlaceOrder returned: the new order id, or why the order was refused.</summary>
public class PlaceOrderResult
{
    // Error numbers thrown by sp_PlaceOrder (see 03_Procedures_and_Views.sql)
    public const int CartEmpty = 50001;
    public const int MissingBom = 50002;
    public const int NotEnoughStock = 50003;

    public int? OrderId { get; set; }
    public int? ErrorNumber { get; set; }

    /// <summary>Raw materials that went below their threshold because of this order.</summary>
    public List<RawMaterial> LowStockMaterials { get; set; } = new();

    public bool Succeeded => OrderId.HasValue;
}
