using System.ComponentModel.DataAnnotations;

namespace AutoTrack.Models;

public class CustomerProfileViewModel
{
    [Required, StringLength(80, MinimumLength = 3)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [RegularExpression(@"^\+?[0-9\s\-]{9,15}$", ErrorMessage = "Enter a valid phone number.")]
    [Display(Name = "Phone number")]
    public string? Phone { get; set; }

    public string Email { get; set; } = "";
}

public class CustomerVehicleViewModel
{
    public string VehicleNo { get; set; } = "";
    public string VehicleModel { get; set; } = "";
}

public class CustomerPortalViewModel
{
    public CustomerProfileViewModel Profile { get; set; } = new();
    public List<CustomerVehicleViewModel> Vehicles { get; set; } = new();
    public List<AppointmentViewModel> Appointments { get; set; } = new();
}

public class ServiceHistoryViewModel
{
    public string VehicleNo { get; set; } = "";
    public List<JobCardViewModel> Jobs { get; set; } = new();
    public List<CustomerVehicleViewModel> Vehicles { get; set; } = new();
}

public class CreateManagedUserViewModel
{
    [Required, StringLength(80, MinimumLength = 3)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Required, StringLength(120, MinimumLength = 3)]
    [Display(Name = "Username or e-mail")]
    public string Username { get; set; } = "";

    [RegularExpression(@"^\+?[0-9\s\-]{9,15}$", ErrorMessage = "Enter a valid phone number.")]
    [Display(Name = "Phone number")]
    public string? Phone { get; set; }

    [Required, StringLength(60, MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required]
    public string Role { get; set; } = "Customer";
}

public class UserAdminIndexViewModel
{
    public List<MockUser> Users { get; set; } = new();
    public CreateManagedUserViewModel NewUser { get; set; } = new();
}

public class AuditLogEntry
{
    public DateTime At { get; set; }
    public string Username { get; set; } = "";
    public string Action { get; set; } = "";
    public string Details { get; set; } = "";
}

public class AuditLogViewModel
{
    public List<AuditLogEntry> Entries { get; set; } = new();
    public List<string> Users { get; set; } = new();
    public List<string> Actions { get; set; } = new();
    public string? SelectedUser { get; set; }
    public string? SelectedAction { get; set; }
    public DateTime? SelectedDate { get; set; }
}
