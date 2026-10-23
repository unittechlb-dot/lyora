using Lyora.DAL.Entities;
using Microsoft.Data.SqlClient;

namespace Lyora.DAL.Repositories;

/// <summary>
/// The customer's shopping cart. Every query is filtered by CustomerId,
/// so a customer can never read or change another customer's cart.
/// </summary>
public class CartRepository
{
    private readonly Db _db;

    public CartRepository(Db db)
    {
        _db = db;
    }

    public ShoppingCart GetCart(int customerId)
    {
        const string sql = @"
        SELECT CartId FROM dbo.ShoppingCarts WHERE CustomerId = @CustomerId;

        SELECT ci.CartItemId, ci.PerfumeId, p.Name, p.ImageUrl, p.SizeMl, p.Price,
               ci.Quantity, p.IsActive, a.ProducibleBottles
        FROM dbo.CartItems ci
        JOIN dbo.ShoppingCarts s           ON s.CartId = ci.CartId
        JOIN dbo.Perfumes p                ON p.PerfumeId = ci.PerfumeId
        JOIN dbo.vw_PerfumeAvailability a  ON a.PerfumeId = p.PerfumeId
        WHERE s.CustomerId = @CustomerId
        ORDER BY ci.CartItemId;";

        using var connection = _db.Open();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@CustomerId", customerId);
        using var reader = command.ExecuteReader();

        var cart = new ShoppingCart { CustomerId = customerId };
        if (reader.Read())
        {
            cart.CartId = reader.GetInt32(0);
        }
        reader.NextResult();
        while (reader.Read())
        {
            cart.Items.Add(new CartItem
            {
                CartItemId = reader.GetInt32(0),
                PerfumeId = reader.GetInt32(1),
                PerfumeName = reader.GetString(2),
                ImageUrl = reader.IsDBNull(3) ? null : reader.GetString(3),
                SizeMl = reader.GetInt32(4),
                UnitPrice = reader.GetDecimal(5),
                Quantity = reader.GetInt32(6),
                IsActive = reader.GetBoolean(7),
                ProducibleBottles = reader.GetInt32(8)
            });
        }
        return cart;
    }

    public int CountBottles(int customerId)
    {
        const string sql = @"
        SELECT ISNULL(SUM(ci.Quantity), 0)
        FROM dbo.CartItems ci
        JOIN dbo.ShoppingCarts s ON s.CartId = ci.CartId
        WHERE s.CustomerId = @CustomerId;";

        using var connection = _db.Open();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@CustomerId", customerId);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    /// <summary>Adds bottles of a perfume (creates the cart if needed, increases the quantity if the perfume is already there).</summary>
    public void AddItem(int customerId, int perfumeId, int quantity)
    {
        const string sql = @"
        DECLARE @CartId INT = (SELECT CartId FROM dbo.ShoppingCarts WHERE CustomerId = @CustomerId);
        IF @CartId IS NULL
        BEGIN
            INSERT INTO dbo.ShoppingCarts (CustomerId) VALUES (@CustomerId);
            SET @CartId = SCOPE_IDENTITY();
        END;

        UPDATE dbo.CartItems SET Quantity = Quantity + @Quantity
        WHERE CartId = @CartId AND PerfumeId = @PerfumeId;

        IF @@ROWCOUNT = 0
            INSERT INTO dbo.CartItems (CartId, PerfumeId, Quantity) VALUES (@CartId, @PerfumeId, @Quantity);";

        using var connection = _db.Open();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@CustomerId", customerId);
        command.Parameters.AddWithValue("@PerfumeId", perfumeId);
        command.Parameters.AddWithValue("@Quantity", quantity);
        command.ExecuteNonQuery();
    }

    public void SetQuantity(int customerId, int cartItemId, int quantity)
    {
        const string sql = @"
        UPDATE ci SET ci.Quantity = @Quantity
        FROM dbo.CartItems ci
        JOIN dbo.ShoppingCarts s ON s.CartId = ci.CartId
        WHERE ci.CartItemId = @CartItemId AND s.CustomerId = @CustomerId;";

        using var connection = _db.Open();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@CustomerId", customerId);
        command.Parameters.AddWithValue("@CartItemId", cartItemId);
        command.Parameters.AddWithValue("@Quantity", quantity);
        command.ExecuteNonQuery();
    }

    public void RemoveItem(int customerId, int cartItemId)
    {
        const string sql = @"
        DELETE ci
        FROM dbo.CartItems ci
        JOIN dbo.ShoppingCarts s ON s.CartId = ci.CartId
        WHERE ci.CartItemId = @CartItemId AND s.CustomerId = @CustomerId;";

        using var connection = _db.Open();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@CustomerId", customerId);
        command.Parameters.AddWithValue("@CartItemId", cartItemId);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Runs sp_CheckCartAvailability: raw materials that are not enough for the whole cart
    /// (perfumes can share materials, so this is checked for the cart as a whole).
    /// </summary>
    public bool HasEnoughRawMaterials(int cartId)
    {
        using var connection = _db.Open();
        using var command = new SqlCommand("dbo.sp_CheckCartAvailability", connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@CartId", cartId);
        using var reader = command.ExecuteReader();
        return !reader.Read();
    }
}
