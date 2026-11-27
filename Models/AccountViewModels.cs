using System.ComponentModel.DataAnnotations;

namespace AutoTrack.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Enter your username or e-mail.")]
    [Display(Name = "Username or e-mail")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Display(Name = "Keep me signed in")]
    public bool RememberMe { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Enter your full name.")]
    [StringLength(80, MinimumLength = 3, ErrorMessage = "Name must be 3 to 80 characters.")]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Enter your phone number.")]
    [RegularExpression(@"^\+?[0-9\s\-]{9,15}$", ErrorMessage = "Enter a valid phone number, for example 0771234567.")]
    [Display(Name = "Phone number")]
    public string Phone { get; set; } = "";

    [Required(ErrorMessage = "Enter your e-mail address.")]
    [EmailAddress(ErrorMessage = "Enter a valid e-mail address.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Choose a password.")]
    [StringLength(100, MinimumLength = 12, ErrorMessage = "Password must be at least 12 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Confirm your password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Enter your current password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string OldPassword { get; set; } = "";

    [Required(ErrorMessage = "Enter a new password.")]
    [StringLength(100, MinimumLength = 12, ErrorMessage = "Password must be at least 12 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Confirm the new password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; } = "";
}
