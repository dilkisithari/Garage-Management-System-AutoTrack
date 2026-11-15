using System.ComponentModel.DataAnnotations;

namespace AutoTrack.Models.Reports;

/// <summary>Date filter used by every report page (sent as a GET query string).</summary>
public class ReportFilterViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Select a start date.")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [Display(Name = "From")]
    public DateTime? From { get; set; }

    [Required(ErrorMessage = "Select an end date.")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [Display(Name = "To")]
    public DateTime? To { get; set; }

    /// <summary>Only the "most-used parts" report shows this field.</summary>
    [Range(1, 20, ErrorMessage = "Enter a number between 1 and 20.")]
    [Display(Name = "Show top")]
    public int? Top { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From.HasValue && To.HasValue && From.Value.Date > To.Value.Date)
            yield return new ValidationResult("The end date must be on or after the start date.", new[] { nameof(To) });

        if (From.HasValue && To.HasValue && (To.Value.Date - From.Value.Date).TotalDays > 366 * 3)
            yield return new ValidationResult("Choose a period of 3 years or less.", new[] { nameof(To) });
    }
}

/// <summary>Month picker for the month-end profit page.</summary>
public class ProfitFilterViewModel
{
    [Required(ErrorMessage = "Select a month.")]
    [Display(Name = "Month")]
    public string? Month { get; set; }   // "yyyy-MM"
}
