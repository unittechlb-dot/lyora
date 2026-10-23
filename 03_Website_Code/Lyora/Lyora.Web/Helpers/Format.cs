using System.Globalization;
using Lyora.DAL.Entities;

namespace Lyora.Web.Helpers;

public static class Format
{
    /// <summary>Prices are shown in US dollars, e.g. $85.00.</summary>
    public static string Money(decimal amount) => "$" + amount.ToString("0.00", CultureInfo.InvariantCulture);

    /// <summary>Image of a perfume, or a placeholder when it has none.</summary>
    public static string Image(string? url) => string.IsNullOrWhiteSpace(url) ? "/images/perfume-placeholder.svg" : url;

    public static string Status(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "Pending",
        OrderStatus.OutForDelivery => "Out for delivery",
        OrderStatus.DeliveredPaid => "Delivered & paid",
        OrderStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };

    public static string Date(DateTime date) => date.ToString("dd MMM yyyy, HH:mm", CultureInfo.InvariantCulture);

    private static readonly HashSet<string> Families = new(StringComparer.OrdinalIgnoreCase) { "floral", "woody", "oriental", "fresh" };

    /// <summary>Illustration of a perfume family (Floral, Woody…), or the bottle placeholder for a new family.</summary>
    public static string FamilyImage(string categoryName) =>
        Families.Contains(categoryName) ? $"/images/families/{categoryName.ToLowerInvariant()}.svg" : "/images/perfume-placeholder.svg";
}
