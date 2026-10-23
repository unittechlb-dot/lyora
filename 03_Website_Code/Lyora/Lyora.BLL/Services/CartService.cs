using Lyora.DAL.Entities;
using Lyora.DAL.Repositories;

namespace Lyora.BLL.Services;

/// <summary>Rules of the shopping cart (Manage Shopping Cart use case).</summary>
public class CartService
{
    public const int MaxBottlesPerPerfume = 10;

    private readonly CartRepository _carts;
    private readonly PerfumeRepository _perfumes;

    public CartService(CartRepository carts, PerfumeRepository perfumes)
    {
        _carts = carts;
        _perfumes = perfumes;
    }

    public ShoppingCart GetCart(int customerId) => _carts.GetCart(customerId);

    public int CountBottles(int customerId) => _carts.CountBottles(customerId);

    public ServiceResult Add(int customerId, int perfumeId, int quantity)
    {
        if (quantity < 1 || quantity > MaxBottlesPerPerfume)
        {
            return ServiceResult.Fail($"Please choose between 1 and {MaxBottlesPerPerfume} bottles.");
        }

        var perfume = _perfumes.GetById(perfumeId);
        if (perfume is not { IsActive: true })
        {
            return ServiceResult.Fail("This perfume is not available.");
        }

        var alreadyInCart = _carts.GetCart(customerId).Items.FirstOrDefault(i => i.PerfumeId == perfumeId)?.Quantity ?? 0;
        var wanted = alreadyInCart + quantity;
        if (wanted > MaxBottlesPerPerfume)
        {
            return ServiceResult.Fail($"You can order at most {MaxBottlesPerPerfume} bottles of the same perfume.");
        }
        if (wanted > perfume.ProducibleBottles)
        {
            return ServiceResult.Fail(perfume.ProducibleBottles == 0
                ? $"{perfume.Name} is out of stock."
                : $"Only {perfume.ProducibleBottles} bottle(s) of {perfume.Name} can be produced right now.");
        }

        _carts.AddItem(customerId, perfumeId, quantity);
        return ServiceResult.Ok();
    }

    /// <summary>Changes the quantity of a line; 0 removes it.</summary>
    public ServiceResult UpdateQuantity(int customerId, int cartItemId, int quantity)
    {
        if (quantity == 0)
        {
            _carts.RemoveItem(customerId, cartItemId);
            return ServiceResult.Ok();
        }
        if (quantity < 0 || quantity > MaxBottlesPerPerfume)
        {
            return ServiceResult.Fail($"Please choose between 1 and {MaxBottlesPerPerfume} bottles.");
        }

        _carts.SetQuantity(customerId, cartItemId, quantity);
        return ServiceResult.Ok();
    }

    public void Remove(int customerId, int cartItemId) => _carts.RemoveItem(customerId, cartItemId);

    /// <summary>
    /// Checks the whole cart against the raw-material stock using the BOM
    /// (two perfumes can use the same raw material, so each line alone is not enough).
    /// </summary>
    public ServiceResult CheckAvailability(ShoppingCart cart)
    {
        if (cart.IsEmpty)
        {
            return ServiceResult.Fail("Your cart is empty.");
        }

        var unavailable = cart.Items.FirstOrDefault(i => !i.IsAvailable);
        if (unavailable != null)
        {
            return ServiceResult.Fail(!unavailable.IsActive || unavailable.ProducibleBottles == 0
                ? $"{unavailable.PerfumeName} is out of stock. Please remove it from your cart."
                : $"Only {unavailable.ProducibleBottles} bottle(s) of {unavailable.PerfumeName} can be produced right now.");
        }

        return _carts.HasEnoughRawMaterials(cart.CartId)
            ? ServiceResult.Ok()
            : ServiceResult.Fail("There are not enough raw materials to prepare your whole cart. Please reduce some quantities.");
    }
}
