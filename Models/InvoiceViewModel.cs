using System.ComponentModel.DataAnnotations;

namespace AutoTrack.Models;

public class InvoiceViewModel
{
    public int Id { get; set; }

    [Display(Name = "Invoice No")]
    public string InvoiceNo { get; set; } = "";

    [Display(Name = "Job Card No")]
    public string? JobCardNo { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Invoice Date")]
    public DateTime IssueDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Display(Name = "Due Date")]
    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(7);

    // Status: Unpaid | Partially Paid | Paid
    public string Status
    {
        get
        {
            if (PaidAmount >= TotalAmount && TotalAmount > 0)
                return "Paid";
            if (PaidAmount > 0)
                return "Partially Paid";
            return "Unpaid";
        }
    }

    // Customer Information
    [Required(ErrorMessage = "Customer name is required.")]
    [Display(Name = "Customer Name")]
    public string CustomerName { get; set; } = "";

    [Required(ErrorMessage = "Customer phone number is required.")]
    [Display(Name = "Customer Phone")]
    public string CustomerPhone { get; set; } = "";

    [EmailAddress]
    [Display(Name = "Customer Email")]
    public string? CustomerEmail { get; set; }

    [Display(Name = "Customer Address")]
    public string? CustomerAddress { get; set; }

    // Vehicle Information
    [Required(ErrorMessage = "Vehicle registration number is required.")]
    [Display(Name = "Vehicle Reg No")]
    public string VehicleNo { get; set; } = "";

    [Display(Name = "Vehicle Make / Model")]
    public string? VehicleModel { get; set; }

    [Display(Name = "Mileage (km)")]
    public int? Mileage { get; set; }

    // Line items
    public List<InvoicePartItem> Parts { get; set; } = new();
    public List<InvoiceLabourItem> Labour { get; set; } = new();
    public List<InvoicePaymentRecord> Payments { get; set; } = new();

    // Summary calculations
    public decimal PartsTotal => Parts.Sum(p => p.LineTotal);
    public decimal LabourTotal => Labour.Sum(l => l.LineTotal);
    public decimal TotalAmount => PartsTotal + LabourTotal;
    public decimal PaidAmount => Payments.Sum(p => p.Amount);
    public decimal BalanceDue => Math.Max(0m, TotalAmount - PaidAmount);

    public string? Notes { get; set; }
}

public class InvoicePartItem
{
    public string PartName { get; set; } = "";
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;
}

public class InvoiceLabourItem
{
    public string Description { get; set; } = "";
    public decimal Hours { get; set; } = 1;
    public decimal HourlyRate { get; set; }
    public decimal LineTotal => Hours * HourlyRate;
}

public class InvoicePaymentRecord
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Cash"; // Cash, Card, Bank Transfer
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public string ReferenceNo { get; set; } = "";
    public string? RecordedBy { get; set; }
    public string? Notes { get; set; }
}
