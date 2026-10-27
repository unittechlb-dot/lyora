using System.Globalization;
using Lyora.DAL.Entities;

namespace Lyora.Web.Helpers;

public static class Format
{
    /// <summary>Prices are shown in US dollars, e.g. $85.00.</summary>
    public static string Money(decimal amount) => "$" + amount.ToString("#,0.00", CultureInfo.InvariantCulture);

    /// <summary>Short money for chart labels: $850, $1.2k, $12k.</summary>
    public static string ShortMoney(decimal amount) => amount switch
    {
        >= 10_000 => "$" + (amount / 1000).ToString("0", CultureInfo.InvariantCulture) + "k",
        >= 1_000 => "$" + (amount / 1000).ToString("0.#", CultureInfo.InvariantCulture) + "k",
        _ => "$" + amount.ToString("0", CultureInfo.InvariantCulture)
    };

    /// <summary>Width/height of a chart bar in % of the biggest value (never 0 so tiny values stay visible).</summary>
    public static string Percent(decimal value, decimal max) =>
        (max <= 0 || value <= 0 ? 0 : Math.Max(2, Math.Round(value / max * 100, 1))).ToString(CultureInfo.InvariantCulture) + "%";

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
