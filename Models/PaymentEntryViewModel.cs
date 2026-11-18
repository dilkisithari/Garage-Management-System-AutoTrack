using System.ComponentModel.DataAnnotations;

namespace AutoTrack.Models;

public class PaymentEntryViewModel
{
    [Required]
    public int InvoiceId { get; set; }

    public string InvoiceNo { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string VehicleNo { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public decimal CurrentPaid { get; set; }
    public decimal BalanceDue { get; set; }

    [Required(ErrorMessage = "Enter the payment amount.")]
    [Range(0.01, 100000000, ErrorMessage = "Payment amount must be greater than zero.")]
    [Display(Name = "Payment amount (LKR)")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Select a payment method.")]
    [Display(Name = "Payment method")]
    public string PaymentMethod { get; set; } = "Cash"; // Cash | Card | Bank Transfer

    [Required(ErrorMessage = "Select payment date.")]
    [DataType(DataType.Date)]
    [Display(Name = "Payment date")]
    public DateTime PaymentDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Enter transaction or reference number.")]
    [StringLength(50)]
    [Display(Name = "Reference no.")]
    public string ReferenceNo { get; set; } = "";

    [StringLength(250)]
    [Display(Name = "Notes / remarks")]
    public string? Notes { get; set; }
}
