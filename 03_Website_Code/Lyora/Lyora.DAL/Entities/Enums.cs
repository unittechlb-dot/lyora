namespace Lyora.DAL.Entities;

// Same enumerations as the Class Diagram.

public enum ScentIntensity
{
    Light,
    Strong
}

public enum ScentStyle
{
    Natural,
    Bold,
    Simple
}

public enum OrderStatus
{
    Pending,
    OutForDelivery,
    DeliveredPaid,
    Cancelled
}

public enum TransactionType
{
    Sale,
    Restock,
    CancelReturn
}
