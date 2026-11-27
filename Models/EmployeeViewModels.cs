using System.ComponentModel.DataAnnotations;

namespace AutoTrack.Models;

/// <summary>Represents a staff employee in AutoTrack (Option 8).</summary>
public class EmployeeRecord
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Role { get; set; } = "";
    public decimal HourlyRate { get; set; }
    public string Phone { get; set; } = "";
    public bool IsActive { get; set; } = true;
    /// <summary>Optional username used to link an employee record to a sign-in account.</summary>
    public string? Username { get; set; }
    public DateTime JoinedDate { get; set; } = DateTime.Today;
}

/// <summary>View model for the Add / Edit employee form.</summary>
public class EmployeeFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 80 characters.")]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Please select a role.")]
    [StringLength(50)]
    [Display(Name = "Role")]
    public string Role { get; set; } = "Mechanic";

    [Required(ErrorMessage = "Hourly rate is required.")]
    [Range(1, 100000, ErrorMessage = "Hourly rate must be between LKR 1 and LKR 100,000.")]
    [Display(Name = "Hourly rate (LKR)")]
    public decimal HourlyRate { get; set; } = 600m;

    [Required(ErrorMessage = "Phone number is required.")]
    [RegularExpression(@"^\+?[0-9\s\-]{7,20}$", ErrorMessage = "Enter a valid phone number.")]
    [StringLength(25)]
    [Display(Name = "Phone number")]
    public string Phone { get; set; } = "";

    [Display(Name = "Active employee")]
    public bool IsActive { get; set; } = true;

    [StringLength(120)]
    [Display(Name = "Linked system username (optional)")]
    public string? Username { get; set; }

    public List<string> RoleOptions { get; set; } = new();
}

/// <summary>View model for the employee directory page.</summary>
public class EmployeeListViewModel
{
    public List<EmployeeRecord> Items { get; set; } = new();
    public string Search { get; set; } = "";
    public string Status { get; set; } = "";
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
}

/// <summary>Daily attendance entry for an employee (Option 8).</summary>
public class AttendanceRecord
{
    public static readonly TimeSpan ShiftStart = new(8, 30, 0);   // 08:30 AM
    public const double StandardDailyHours = 8.0;

    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public TimeSpan? ClockIn { get; set; }
    public TimeSpan? ClockOut { get; set; }
    public string? Notes { get; set; }

    public bool IsClockedIn => ClockIn.HasValue;
    public bool IsCompleted => ClockIn.HasValue && ClockOut.HasValue;

    public bool IsLate => ClockIn.HasValue && ClockIn.Value > ShiftStart;

    public int LateMinutes => IsLate
        ? (int)Math.Round((ClockIn!.Value - ShiftStart).TotalMinutes)
        : 0;

    public double HoursWorked
    {
        get
        {
            if (!ClockIn.HasValue || !ClockOut.HasValue) return 0;
            var diff = ClockOut.Value - ClockIn.Value;
            return diff.TotalHours > 0 ? Math.Round(diff.TotalHours, 2) : 0;
        }
    }

    public double OvertimeHours => HoursWorked > StandardDailyHours
        ? Math.Round(HoursWorked - StandardDailyHours, 2)
        : 0;

    public double RegularHours => Math.Round(Math.Min(HoursWorked, StandardDailyHours), 2);

    public bool IsOvertime => OvertimeHours > 0;
}

/// <summary>View model for the Clock In / Clock Out terminal page.</summary>
public class ClockInOutViewModel
{
    public EmployeeRecord? SelectedEmployee { get; set; }
    public AttendanceRecord? TodayEntry { get; set; }
    public List<EmployeeRecord> EmployeeOptions { get; set; } = new();
    public bool CanSwitchEmployee { get; set; }
    public DateTime ServerNow { get; set; } = DateTime.Now;
    public TimeSpan ShiftStart => AttendanceRecord.ShiftStart;
    public double StandardDailyHours => AttendanceRecord.StandardDailyHours;
}

/// <summary>One calendar day row in the monthly attendance sheet.</summary>
public class AttendanceSheetDayRow
{
    public DateTime Date { get; set; }
    public AttendanceRecord? Entry { get; set; }
    public bool IsSunday => Date.DayOfWeek == DayOfWeek.Sunday;
    public bool IsFuture => Date.Date > DateTime.Today;
}

/// <summary>View model for the monthly per-employee attendance sheet.</summary>
public class AttendanceSheetViewModel
{
    public EmployeeRecord? Employee { get; set; }
    public int SelectedEmployeeId { get; set; }
    public string SelectedMonth { get; set; } = "";
    public DateTime MonthStart { get; set; }
    public bool CanSwitchEmployee { get; set; }

    public List<EmployeeRecord> EmployeeOptions { get; set; } = new();
    public List<(string Value, string Text)> MonthOptions { get; set; } = new();
    public List<AttendanceSheetDayRow> Days { get; set; } = new();

    public int DaysPresent => Days.Count(d => d.Entry?.IsClockedIn == true);
    public int LateDays => Days.Count(d => d.Entry?.IsLate == true);
    public int OvertimeDays => Days.Count(d => d.Entry?.IsOvertime == true);
    public double TotalHours => Math.Round(Days.Sum(d => d.Entry?.HoursWorked ?? 0), 2);
    public double TotalRegularHours => Math.Round(Days.Sum(d => d.Entry?.RegularHours ?? 0), 2);
    public double TotalOvertimeHours => Math.Round(Days.Sum(d => d.Entry?.OvertimeHours ?? 0), 2);
}
