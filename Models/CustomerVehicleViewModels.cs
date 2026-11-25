using System.ComponentModel.DataAnnotations;

namespace AutoTrack.Models;

// ─── Customer view models ──────────────────────────────────────────────────────

/// <summary>View model for the Add / Edit customer form.</summary>
public class CustomerFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 80 characters.")]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Phone number is required.")]
    [RegularExpression(@"^\+?[0-9\s\-]{7,20}$", ErrorMessage = "Enter a valid phone number.")]
    [StringLength(25)]
    [Display(Name = "Phone number")]
    public string Phone { get; set; } = "";

    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(120)]
    [Display(Name = "Email address")]
    public string? Email { get; set; }

    [StringLength(250)]
    [Display(Name = "Address")]
    public string? Address { get; set; }
}

/// <summary>View model for the paged customer list page.</summary>
public class CustomerListViewModel
{
    public List<CustomerRecord> Items { get; set; } = new();
    public string Search { get; set; } = "";
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
    public int PageSize { get; set; } = 10;
}

// ─── Vehicle view models ───────────────────────────────────────────────────────

/// <summary>View model for the Add / Edit vehicle form.</summary>
public class VehicleFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Registration number is required.")]
    [StringLength(20, ErrorMessage = "Registration number cannot exceed 20 characters.")]
    [Display(Name = "Registration No.")]
    public string RegistrationNo { get; set; } = "";

    [Required(ErrorMessage = "Make is required.")]
    [StringLength(50)]
    [Display(Name = "Make")]
    public string Make { get; set; } = "";

    [Required(ErrorMessage = "Model is required.")]
    [StringLength(50)]
    [Display(Name = "Model")]
    public string VehicleModel { get; set; } = "";

    [Required(ErrorMessage = "Year is required.")]
    [Range(1980, 2100, ErrorMessage = "Enter a valid year (1980–2100).")]
    [Display(Name = "Year")]
    public int Year { get; set; } = DateTime.Today.Year;

    [StringLength(30)]
    [Display(Name = "Colour")]
    public string? Colour { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Mileage cannot be negative.")]
    [Display(Name = "Mileage (km)")]
    public int? Mileage { get; set; }

    [Required(ErrorMessage = "Please select the vehicle owner.")]
    [Display(Name = "Owner")]
    public int OwnerId { get; set; }

    /// <summary>Populated by the controller for the owner dropdown.</summary>
    public List<VehicleOwnerOption> OwnerOptions { get; set; } = new();
}

/// <summary>Lightweight option for the owner dropdown in the vehicle form.</summary>
public class VehicleOwnerOption
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = "";
}
