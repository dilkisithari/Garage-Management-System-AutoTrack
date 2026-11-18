using System.ComponentModel.DataAnnotations;

namespace AutoTrack.Models;

public class SupplierViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Enter the supplier name.")]
    [StringLength(100, ErrorMessage = "Supplier name cannot exceed 100 characters.")]
    [Display(Name = "Supplier name")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Enter the phone number.")]
    [Phone(ErrorMessage = "Enter a valid phone number.")]
    [StringLength(25)]
    [Display(Name = "Phone number")]
    public string Phone { get; set; } = "";

    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(100)]
    [Display(Name = "Email address")]
    public string? Email { get; set; }

    [StringLength(250)]
    [Display(Name = "Address")]
    public string? Address { get; set; }

    [Required(ErrorMessage = "Specify the parts or categories supplied.")]
    [StringLength(300)]
    [Display(Name = "Parts supplied")]
    public string PartsSupplied { get; set; } = "";

    [StringLength(80)]
    [Display(Name = "Contact person")]
    public string? ContactPerson { get; set; }

    [Display(Name = "Active supplier")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Date added")]
    public DateTime CreatedDate { get; set; } = DateTime.Today;

    [StringLength(300)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    /// <summary>Helper property returning parts as individual tags</summary>
    public IEnumerable<string> PartsList =>
        string.IsNullOrWhiteSpace(PartsSupplied)
            ? Enumerable.Empty<string>()
            : PartsSupplied.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(p => p.Trim());
}
