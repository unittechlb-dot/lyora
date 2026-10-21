using Lyora.BLL.Security;
using Lyora.DAL.Entities;
using Lyora.DAL.Repositories;

namespace Lyora.BLL.Services;

/// <summary>Register, login, profile and password rules.</summary>
public class AccountService
{
    private const int MinimumAge = 16;

    // Used when the email does not exist, so a wrong email takes as long as a wrong password.
    private static readonly string DummyHash = PasswordHasher.Hash("lyora-dummy-password");

    private readonly UserRepository _users;

    public AccountService(UserRepository users)
    {
        _users = users;
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    /// <summary>Returns the user when the email and password are correct, otherwise null.</summary>
    public User? Authenticate(string email, string password)
    {
        var user = _users.GetByEmail(NormalizeEmail(email));
        if (user == null)
        {
            PasswordHasher.Verify(password, DummyHash);
            return null;
        }
        return PasswordHasher.Verify(password, user.PasswordHash) ? user : null;
    }

    /// <summary>Creates a new customer account. On success, <paramref name="customer"/> gets its new UserId.</summary>
    public ServiceResult Register(Customer customer, string password)
    {
        customer.Email = NormalizeEmail(customer.Email);
        customer.FullName = customer.FullName.Trim();

        var ageError = ValidateDateOfBirth(customer.DateOfBirth);
        if (ageError != null)
        {
            return ServiceResult.Fail(ageError);
        }
        if (_users.EmailExists(customer.Email))
        {
            return ServiceResult.Fail("An account with this email already exists.");
        }

        customer.PasswordHash = PasswordHasher.Hash(password);
        var userId = _users.CreateCustomer(customer);
        if (userId == null)
        {
            return ServiceResult.Fail("An account with this email already exists.");
        }

        customer.UserId = userId.Value;
        return ServiceResult.Ok();
    }

    public Customer? GetCustomer(int userId) => _users.GetById(userId) as Customer;

    public ServiceResult UpdateProfile(Customer customer)
    {
        var ageError = ValidateDateOfBirth(customer.DateOfBirth);
        if (ageError != null)
        {
            return ServiceResult.Fail(ageError);
        }

        customer.FullName = customer.FullName.Trim();
        _users.UpdateCustomerProfile(customer);
        return ServiceResult.Ok();
    }

    public ServiceResult ChangePassword(int userId, string currentPassword, string newPassword)
    {
        var user = _users.GetById(userId);
        if (user == null || !PasswordHasher.Verify(currentPassword, user.PasswordHash))
        {
            return ServiceResult.Fail("The current password is not correct.");
        }
        if (currentPassword == newPassword)
        {
            return ServiceResult.Fail("The new password must be different from the current one.");
        }

        _users.UpdatePasswordHash(userId, PasswordHasher.Hash(newPassword));
        return ServiceResult.Ok();
    }

    private static string? ValidateDateOfBirth(DateTime? dateOfBirth)
    {
        if (dateOfBirth == null)
        {
            return null;
        }
        var today = DateTime.Today;
        if (dateOfBirth > today.AddYears(-MinimumAge))
        {
            return $"You must be at least {MinimumAge} years old.";
        }
        if (dateOfBirth < today.AddYears(-110))
        {
            return "Please enter a valid date of birth.";
        }
        return null;
    }
}
