using System.ComponentModel.DataAnnotations;

namespace AutoTrack.Models;

/// <summary>
/// Shared view model for the job card screens.
/// Keep these field names - the backend team maps them to the JOB_CARD tables.
/// </summary>
public class JobCardViewModel
{
    public int Id { get; set; }

    public string JobNo { get; set; } = "";

    [Required(ErrorMessage = "Enter the vehicle registration number.")]
    [StringLength(15)]
    [Display(Name = "Vehicle no.")]
    public string VehicleNo { get; set; } = "";

    [Required(ErrorMessage = "Enter the customer name.")]
    [StringLength(80)]
    [Display(Name = "Customer")]
    public string CustomerName { get; set; } = "";

    [Required(ErrorMessage = "Describe the customer's complaint.")]
    [StringLength(500)]
    public string Complaint { get; set; } = "";

    [Required(ErrorMessage = "Assign a mechanic.")]
    public string Mechanic { get; set; } = "";

    [Required]
    public string Status { get; set; } = "Pending";

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [Display(Name = "Date opened")]
    public DateTime DateOpened { get; set; } = DateTime.Today;

    [StringLength(500)]
    public string? Notes { get; set; }

    public List<JobTaskItem> Tasks { get; set; } = new();
    public List<JobPartItem> Parts { get; set; } = new();
    public List<JobLabourItem> Labour { get; set; } = new();

    public decimal PartsTotal => Parts.Sum(p => p.LineTotal);
    public decimal LabourTotal => Labour.Sum(l => l.LineTotal);
    public decimal GrandTotal => PartsTotal + LabourTotal;
}

public class JobTaskItem
{
    public string? Description { get; set; }
    public bool Done { get; set; }
}

public class JobPartItem
{
    public string? PartName { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;
}

public class JobLabourItem
{
    public string? Description { get; set; }
    public decimal Hours { get; set; } = 1;
    public decimal Rate { get; set; }
    public decimal LineTotal => Hours * Rate;
}
