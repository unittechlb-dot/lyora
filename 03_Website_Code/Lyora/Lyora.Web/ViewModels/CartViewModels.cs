using System.ComponentModel.DataAnnotations;
using Lyora.DAL.Entities;

namespace Lyora.Web.ViewModels;

public class CartViewModel
{
    public ShoppingCart Cart { get; set; } = new();
    public bool CanCheckout { get; set; }
    public string? Problem { get; set; }
}

public class CheckoutViewModel
{
    public ShoppingCart Cart { get; set; } = new();
    public string FullName { get; set; } = "";
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Please enter the delivery address.")]
    [StringLength(250, MinimumLength = 5, ErrorMessage = "Address must be 5 to 250 characters.")]
    [Display(Name = "Delivery address")]
    public string DeliveryAddress { get; set; } = "";
}
