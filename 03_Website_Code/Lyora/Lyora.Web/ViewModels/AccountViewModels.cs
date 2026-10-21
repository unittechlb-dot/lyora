using System.ComponentModel.DataAnnotations;

namespace Lyora.Web.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be 3 to 100 characters.")]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Please enter your email.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email.")]
    [StringLength(150)]
    public string Email { get; set; } = "";

    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    [StringLength(20)]
    public string? Phone { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateTime? DateOfBirth { get; set; }

    [RegularExpression("Male|Female", ErrorMessage = "Please choose Male or Female.")]
    public string? Gender { get; set; }

    [StringLength(250)]
    [Display(Name = "Delivery address")]
    public string? Address { get; set; }

    [Required(ErrorMessage = "Please choose a password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Password must contain letters and numbers.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare(nameof(Password), ErrorMessage = "The two passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";
}

public class ProfileViewModel
{
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be 3 to 100 characters.")]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    [StringLength(20)]
    public string? Phone { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateTime? DateOfBirth { get; set; }

    [RegularExpression("Male|Female", ErrorMessage = "Please choose Male or Female.")]
    public string? Gender { get; set; }

    [StringLength(250)]
    [Display(Name = "Delivery address")]
    public string? Address { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Please enter your current password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "Please choose a new password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Password must contain letters and numbers.")]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Please confirm the new password.")]
    [Compare(nameof(NewPassword), ErrorMessage = "The two passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; } = "";
}
