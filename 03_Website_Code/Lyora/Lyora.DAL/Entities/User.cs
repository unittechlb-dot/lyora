namespace Lyora.DAL.Entities;

/// <summary>Common data of every account (abstract in the Class Diagram).</summary>
public abstract class User
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; }

    public abstract string Role { get; }
}

public class Customer : User
{
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }

    public override string Role => Roles.Customer;
}

public class Administrator : User
{
    public string AdminRole { get; set; } = "Manager";

    public override string Role => Roles.Administrator;
}
