using System.ComponentModel.DataAnnotations;

namespace RentalPortal.ViewModels;

public class RegisterViewModel
{
    [Required, Display(Name = "First name"), StringLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, Display(Name = "Last name"), StringLength(80)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone, Display(Name = "Phone")]
    public string? Phone { get; set; }

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Display(Name = "Confirm password")]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required, Display(Name = "I am a")]
    public string Role { get; set; } = Domain.Roles.Applicant;
}
