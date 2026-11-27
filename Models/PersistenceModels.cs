using System.ComponentModel.DataAnnotations;
using AutoTrack.Data;

namespace AutoTrack.Models;

public class CustomerVehicle
{
    public int Id { get; set; }
    public string VehicleNo { get; set; } = "";
    public string? Model { get; set; }
    public string CustomerId { get; set; } = "";
    public AutoTrackUser Customer { get; set; } = null!;
}

public class CustomerAppointment
{
    public int Id { get; set; }
    public string AppointmentNo { get; set; } = "";
    public string CustomerId { get; set; } = "";
    public AutoTrackUser Customer { get; set; } = null!;
    public int VehicleId { get; set; }
    public CustomerVehicle Vehicle { get; set; } = null!;
    public string ServiceType { get; set; } = "";
    public DateTime AppointmentDate { get; set; }
    public string TimeSlot { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ServiceJobRecord
{
    public int Id { get; set; }
    public string JobNo { get; set; } = "";
    public string VehicleNo { get; set; } = "";
    public string? CustomerId { get; set; }
    public AutoTrackUser? Customer { get; set; }
    public DateTime DateOpened { get; set; }
    public string Complaint { get; set; } = "";
    public string WorkDone { get; set; } = "";
    public string PartsJson { get; set; } = "[]";
    public string LabourJson { get; set; } = "[]";
    public decimal TotalCost { get; set; }
    public string Status { get; set; } = "Completed";
}

public class InvoiceRecord
{
    public int Id { get; set; }
    public string InvoiceNo { get; set; } = "";
    public string? CustomerId { get; set; }
    public DateTime IssuedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public string Status { get; set; } = "";
}

public class PaymentRecord
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public InvoiceRecord Invoice { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Method { get; set; } = "";
    public DateTime PaidAt { get; set; }
}

public class StockRecord
{
    public int Id { get; set; }
    public string PartName { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AuditLogRecord
{
    public long Id { get; set; }
    public string ActorUsername { get; set; } = "";
    public string Action { get; set; } = "";
    public string TableName { get; set; } = "";
    public DateTime OccurredAt { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
}

public class ServiceHistoryItem
{
    public string JobNo { get; set; } = "";
    public DateTime Date { get; set; }
    public string Complaint { get; set; } = "";
    public string WorkDone { get; set; } = "";
    public List<ServiceHistoryPart> Parts { get; set; } = new();
    public decimal TotalCost { get; set; }
}

public class ServiceHistoryPart
{
    public string PartName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class ServiceHistoryLabour
{
    public string Description { get; set; } = "";
    public decimal Hours { get; set; }
    public decimal Rate { get; set; }
}

public class CreateManagedIdentityUserRequest
{
    [Required, StringLength(80, MinimumLength = 3)]
    public string FullName { get; set; } = "";

    [Required, EmailAddress, StringLength(120)]
    public string Email { get; set; } = "";

    [System.ComponentModel.DataAnnotations.RegularExpression(
        @"^\+?[0-9\s\-]{9,15}$",
        ErrorMessage = "Enter a valid phone number.")]
    public string? Phone { get; set; }

    [Required, StringLength(100, MinimumLength = 12)]
    public string Password { get; set; } = "";

    [Required]
    public string Role { get; set; } = "Customer";
}

public class AssignIdentityRoleRequest
{
    [Required]
    public string Role { get; set; } = "";
}

public class SetIdentityActiveRequest
{
    public bool IsActive { get; set; }
}
