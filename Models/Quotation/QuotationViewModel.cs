using System.ComponentModel.DataAnnotations;

namespace AutoTrack.Models;

public class QuotationViewModel
{
    public int Id { get; set; }
    public string QuoteNo { get; set; } = "";

    [Required(ErrorMessage = "Enter the vehicle registration number.")]
    [StringLength(15)]
    [Display(Name = "Vehicle Reg No.")]
    public string VehicleNo { get; set; } = "";

    [Required(ErrorMessage = "Enter the vehicle make and model.")]
    [StringLength(80)]
    [Display(Name = "Make & Model")]
    public string VehicleModel { get; set; } = "";

    [Display(Name = "Current Mileage (km)")]
    public int? Mileage { get; set; }

    [Required(ErrorMessage = "Enter the customer name.")]
    [StringLength(80)]
    [Display(Name = "Customer Name")]
    public string CustomerName { get; set; } = "";

    [Required(ErrorMessage = "Enter customer phone number.")]
    [Phone]
    [Display(Name = "Phone Number")]
    public string CustomerPhone { get; set; } = "";

    [EmailAddress]
    [Display(Name = "Email Address")]
    public string? CustomerEmail { get; set; }

    [Required(ErrorMessage = "Enter customer complaint / requested work.")]
    [StringLength(500)]
    [Display(Name = "Work Scope / Request")]
    public string CustomerRequest { get; set; } = "";

    // Draft | Pending Owner Approval | Approved by Owner | Revision Requested | Sent to Customer | Converted
    public string Status { get; set; } = "Draft";

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [Display(Name = "Date Created")]
    public DateTime DateCreated { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [Display(Name = "Valid Until (Expiry)")]
    public DateTime ExpiryDate { get; set; } = DateTime.Today.AddDays(14);

    [StringLength(500)]
    public string? Notes { get; set; }

    // Owner Approval properties
    public string? OwnerRemarks { get; set; }
    public DateTime? OwnerApprovedDate { get; set; }
    public string? ApprovedBy { get; set; }

    // Job Card reference once converted
    public string? ConvertedJobCardNo { get; set; }

    // Line items
    public List<QuotationPartItem> Parts { get; set; } = new();
    public List<QuotationLabourItem> Labour { get; set; } = new();

    // Financial calculations
    [Display(Name = "Discount (%)")]
    [Range(0, 100)]
    public decimal DiscountPercent { get; set; } = 0;

    [Display(Name = "Include VAT (18%)")]
    public bool IncludeVat { get; set; } = false;
    public decimal PartsSubtotal => Parts.Sum(p => p.LineTotal);
    public decimal LabourSubtotal => Labour.Sum(l => l.LineTotal);
    public decimal NetSubtotal => PartsSubtotal + LabourSubtotal;
    public decimal DiscountAmount => Math.Round(NetSubtotal * (DiscountPercent / 100m), 2);
    public decimal AfterDiscount => NetSubtotal - DiscountAmount;
    public decimal VatAmount => IncludeVat ? Math.Round(AfterDiscount * 0.18m, 2) : 0m;
    public decimal GrandTotal => AfterDiscount + VatAmount;

    // Audit log trail
    public List<QuotationAuditItem> AuditLogs { get; set; } = new();
}
