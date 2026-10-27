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

/// <summary>What a questionnaire question is about (same values as dbo.Questions.Topic).</summary>
public enum QuestionTopic
{
    Allergy,
    Intensity,
    Style,
    Family,
    Notes,
    Occasion,
    Size,
    Budget
}

public enum NotificationType
{
    Promotion,
    Order,
    LowStock
}

/// <summary>Who receives a promotion notification.</summary>
public enum PromotionAudience
{
    AllCustomers,
    AgeGroup,
    Gender,
    BoughtPromotedPerfumes
}
