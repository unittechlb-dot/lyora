using System.Globalization;

namespace Lyora.Web.Helpers;

public static class Format
{
    /// <summary>Prices are shown in US dollars, e.g. $85.00.</summary>
    public static string Money(decimal amount) => "$" + amount.ToString("0.00", CultureInfo.InvariantCulture);

    /// <summary>Image of a perfume, or a placeholder when it has none.</summary>
    public static string Image(string? url) => string.IsNullOrWhiteSpace(url) ? "/images/perfume-placeholder.svg" : url;
}
