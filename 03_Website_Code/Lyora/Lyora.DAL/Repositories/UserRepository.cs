using Lyora.DAL.Entities;
using Microsoft.Data.SqlClient;

namespace Lyora.DAL.Repositories;

public class UserRepository
{
    private readonly Db _db;

    // Users + the extra columns of Customers or Administrators.
    private const string SelectUser = @"
        SELECT u.UserId, u.FullName, u.Email, u.PasswordHash, u.Phone, u.Role, u.CreatedAt,
               c.DateOfBirth, c.Gender, c.Address, a.AdminRole
        FROM dbo.Users u
        LEFT JOIN dbo.Customers c      ON c.CustomerId = u.UserId
        LEFT JOIN dbo.Administrators a ON a.AdminId = u.UserId";

    public UserRepository(Db db)
    {
        _db = db;
    }

    public User? GetByEmail(string email)
    {
        using var connection = _db.Open();
        using var command = new SqlCommand(SelectUser + " WHERE u.Email = @Email;", connection);
        command.Parameters.AddWithValue("@Email", email);
        return ReadUser(command);
    }

    public User? GetById(int userId)
    {
        using var connection = _db.Open();
        using var command = new SqlCommand(SelectUser + " WHERE u.UserId = @UserId;", connection);
        command.Parameters.AddWithValue("@UserId", userId);
        return ReadUser(command);
    }

    public bool EmailExists(string email)
    {
        using var connection = _db.Open();
        using var command = new SqlCommand("SELECT COUNT(*) FROM dbo.Users WHERE Email = @Email;", connection);
        command.Parameters.AddWithValue("@Email", email);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    /// <summary>
    /// Creates the user, the customer row and an empty shopping cart in one transaction.
    /// Returns the new user id, or null when the email is already used.
    /// </summary>
    public int? CreateCustomer(Customer customer)
    {
        const string sql = @"
        DECLARE @UserId INT;

        INSERT INTO dbo.Users (FullName, Email, PasswordHash, Phone, Role)
        VALUES (@FullName, @Email, @PasswordHash, @Phone, N'Customer');

        SET @UserId = SCOPE_IDENTITY();

        INSERT INTO dbo.Customers (CustomerId, DateOfBirth, Gender, Address)
        VALUES (@UserId, @DateOfBirth, @Gender, @Address);

        INSERT INTO dbo.ShoppingCarts (CustomerId) VALUES (@UserId);

        SELECT @UserId;";

        using var connection = _db.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@FullName", customer.FullName);
            command.Parameters.AddWithValue("@Email", customer.Email);
            command.Parameters.AddWithValue("@PasswordHash", customer.PasswordHash);
            command.Parameters.AddWithValue("@Phone", (object?)customer.Phone ?? DBNull.Value);
            command.Parameters.AddWithValue("@DateOfBirth", (object?)customer.DateOfBirth ?? DBNull.Value);
            command.Parameters.AddWithValue("@Gender", (object?)customer.Gender ?? DBNull.Value);
            command.Parameters.AddWithValue("@Address", (object?)customer.Address ?? DBNull.Value);

            var userId = Convert.ToInt32(command.ExecuteScalar());
            transaction.Commit();
            return userId;
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)   // unique key violation = email already used
        {
            transaction.Rollback();
            return null;
        }
    }

    public void UpdateCustomerProfile(Customer customer)
    {
        const string sql = @"
        UPDATE dbo.Users
        SET FullName = @FullName, Phone = @Phone
        WHERE UserId = @UserId;

        UPDATE dbo.Customers
        SET DateOfBirth = @DateOfBirth, Gender = @Gender, Address = @Address
        WHERE CustomerId = @UserId;";

        using var connection = _db.Open();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", customer.UserId);
        command.Parameters.AddWithValue("@FullName", customer.FullName);
        command.Parameters.AddWithValue("@Phone", (object?)customer.Phone ?? DBNull.Value);
        command.Parameters.AddWithValue("@DateOfBirth", (object?)customer.DateOfBirth ?? DBNull.Value);
        command.Parameters.AddWithValue("@Gender", (object?)customer.Gender ?? DBNull.Value);
        command.Parameters.AddWithValue("@Address", (object?)customer.Address ?? DBNull.Value);
        command.ExecuteNonQuery();
    }

    public void UpdatePasswordHash(int userId, string passwordHash)
    {
        using var connection = _db.Open();
        using var command = new SqlCommand("UPDATE dbo.Users SET PasswordHash = @Hash WHERE UserId = @UserId;", connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@Hash", passwordHash);
        command.ExecuteNonQuery();
    }

    private static User? ReadUser(SqlCommand command)
    {
        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        User user = reader.GetString(5) == Roles.Administrator
            ? new Administrator { AdminRole = reader.IsDBNull(10) ? "Manager" : reader.GetString(10) }
            : new Customer
            {
                DateOfBirth = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                Gender = reader.IsDBNull(8) ? null : reader.GetString(8),
                Address = reader.IsDBNull(9) ? null : reader.GetString(9)
            };

        user.UserId = reader.GetInt32(0);
        user.FullName = reader.GetString(1);
        user.Email = reader.GetString(2);
        user.PasswordHash = reader.GetString(3);
        user.Phone = reader.IsDBNull(4) ? null : reader.GetString(4);
        user.CreatedAt = reader.GetDateTime(6);
        return user;
    }
}
