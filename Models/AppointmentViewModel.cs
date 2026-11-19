using System.ComponentModel.DataAnnotations;

namespace AutoTrack.Models;

/// <summary>
/// Core model representing an appointment in the AutoTrack system (Option 4).
/// </summary>
public class AppointmentViewModel
{
    public int Id { get; set; }

    [Display(Name = "Appointment Ref")]
    public string AppointmentNo { get; set; } = "";

    [Required(ErrorMessage = "Customer name is required.")]
    [Display(Name = "Customer Name")]
    public string CustomerName { get; set; } = "";

    [Required(ErrorMessage = "Phone number is required.")]
    [Phone(ErrorMessage = "Enter a valid phone number.")]
    [Display(Name = "Phone Number")]
    public string CustomerPhone { get; set; } = "";

    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Email Address")]
    public string? CustomerEmail { get; set; }

    [Required(ErrorMessage = "Vehicle registration number is required.")]
    [Display(Name = "Vehicle Reg No")]
    public string VehicleNo { get; set; } = "";

    [Display(Name = "Vehicle Make / Model")]
    public string? VehicleModel { get; set; }

    [Required(ErrorMessage = "Please select a service type.")]
    [Display(Name = "Service Type")]
    public string ServiceType { get; set; } = "";

    [Required(ErrorMessage = "Appointment date is required.")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [Display(Name = "Appointment Date")]
    public DateTime AppointmentDate { get; set; } = DateTime.Today.AddDays(1);

    [Required(ErrorMessage = "Please select a time slot.")]
    [Display(Name = "Time Slot")]
    public string TimeSlot { get; set; } = "";

    [StringLength(500)]
    [Display(Name = "Notes / Special Instructions")]
    public string? Notes { get; set; }

    /// <summary>
    /// Pending | Confirmed | Cancelled
    /// </summary>
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Display(Name = "Cancellation Reason")]
    public string? CancellationReason { get; set; }
}

/// <summary>
/// View model for the Customer booking page.
/// </summary>
public class AppointmentBookingViewModel
{
    [Required(ErrorMessage = "Please enter or pick your vehicle number.")]
    [Display(Name = "Vehicle Registration No")]
    public string VehicleNo { get; set; } = "";

    [Display(Name = "Vehicle Make & Model")]
    public string? VehicleModel { get; set; }

    [Required(ErrorMessage = "Please choose a service type.")]
    [Display(Name = "Service Type")]
    public string ServiceType { get; set; } = "";

    [Required(ErrorMessage = "Please select an appointment date.")]
    [DataType(DataType.Date)]
    [Display(Name = "Date")]
    public DateTime AppointmentDate { get; set; } = DateTime.Today.AddDays(1);

    [Required(ErrorMessage = "Please choose an available time slot.")]
    [Display(Name = "Time Slot")]
    public string TimeSlot { get; set; } = "";

    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    [Display(Name = "Notes / Customer Instructions")]
    public string? Notes { get; set; }

    [Required(ErrorMessage = "Your name is required.")]
    [Display(Name = "Full Name")]
    public string CustomerName { get; set; } = "";

    [Required(ErrorMessage = "Phone number is required.")]
    [Phone(ErrorMessage = "Enter a valid phone number.")]
    [Display(Name = "Phone Number")]
    public string CustomerPhone { get; set; } = "";

    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Email Address")]
    public string? CustomerEmail { get; set; }

    // Helpers for form options
    public List<string> ExistingVehicles { get; set; } = new();
    public List<string> AvailableServiceTypes { get; set; } = new();
    public List<string> AllTimeSlots { get; set; } = new();
    public List<string> BookedSlotsForDate { get; set; } = new();
}

/// <summary>
/// View model for the Front Desk calendar view (Day and Week views).
/// </summary>
public class AppointmentCalendarViewModel
{
    /// <summary>
    /// "day" or "week"
    /// </summary>
    public string ViewMode { get; set; } = "week";

    public DateTime SelectedDate { get; set; } = DateTime.Today;

    public DateTime WeekStartDate { get; set; }

    public DateTime WeekEndDate { get; set; }

    public List<DateTime> WeekDays { get; set; } = new();

    public List<string> TimeSlots { get; set; } = new();

    public List<AppointmentViewModel> Appointments { get; set; } = new();

    /// <summary>
    /// Key: "yyyy-MM-dd|TimeSlot", Value: Appointment
    /// </summary>
    public Dictionary<string, AppointmentViewModel> BookedSlotsLookup { get; set; } = new();
}

/// <summary>
/// View model for rescheduling an appointment.
/// </summary>
public class RescheduleAppointmentViewModel
{
    public int Id { get; set; }

    public string AppointmentNo { get; set; } = "";

    [Required(ErrorMessage = "Please select a new date.")]
    [DataType(DataType.Date)]
    public DateTime NewDate { get; set; } = DateTime.Today.AddDays(1);

    [Required(ErrorMessage = "Please select a new time slot.")]
    public string NewTimeSlot { get; set; } = "";

    public string CustomerName { get; set; } = "";
    public string VehicleNo { get; set; } = "";
}

