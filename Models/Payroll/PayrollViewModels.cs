using System.ComponentModel.DataAnnotations;

namespace AutoTrack.Models.Payroll;

/// <summary>Single employee's payroll row for a given month (Option 9).</summary>
public class PayrollEmployeeRow
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public string Role { get; set; } = "";
    public double RegularHours { get; set; }
    public double OvertimeHours { get; set; }
    public decimal HourlyRate { get; set; }

    public decimal OvertimeRate => Math.Round(HourlyRate * 1.5m, 2);
    public decimal BasicPay => Math.Round((decimal)RegularHours * HourlyRate, 2);
    public decimal OvertimePay => Math.Round((decimal)OvertimeHours * OvertimeRate, 2);
    public decimal GrossSalary => BasicPay + OvertimePay;
    public decimal EpfDeduction => Math.Round(BasicPay * 0.08m, 2);
    public decimal NetSalary => GrossSalary - EpfDeduction;

    public bool IsPaid { get; set; }
}

/// <summary>Option for the month selector dropdown.</summary>
public class MonthSelectOption
{
    public string Value { get; set; } = "";
    public string Label { get; set; } = "";
}

/// <summary>View model for the Payroll Run page (GET /Payroll).</summary>
public class PayrollRunViewModel
{
    public string SelectedMonth { get; set; } = "";
    public string SelectedMonthLabel { get; set; } = "";
    public List<MonthSelectOption> MonthOptions { get; set; } = new();
    public List<PayrollEmployeeRow> Rows { get; set; } = new();

    public bool IsMonthProcessed { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ProcessedBy { get; set; }

    public double TotalRegularHours => Math.Round(Rows.Sum(r => r.RegularHours), 1);
    public double TotalOvertimeHours => Math.Round(Rows.Sum(r => r.OvertimeHours), 1);
    public decimal TotalBasicPay => Rows.Sum(r => r.BasicPay);
    public decimal TotalOvertimePay => Rows.Sum(r => r.OvertimePay);
    public decimal TotalEpf => Rows.Sum(r => r.EpfDeduction);
    public decimal TotalNetSalary => Rows.Sum(r => r.NetSalary);
}

/// <summary>View model for a single employee's printable payslip (GET /Payroll/Payslip).</summary>
public class PayslipViewModel
{
    public string MonthKey { get; set; } = "";
    public string MonthLabel { get; set; } = "";
    public string PayPeriod { get; set; } = "";
    public string PayslipNumber { get; set; } = "";
    public DateTime GeneratedDate { get; set; } = DateTime.Today;

    public PayrollEmployeeRow Row { get; set; } = new();
    public string EmployeeCode => $"EMP-{Row.EmployeeId:D3}";
    public string PaymentStatus => Row.IsPaid ? "Paid" : "Pending Run";
    public DateTime? PaidAt { get; set; }
}

/// <summary>Represents a single garage expense record (Option 9).</summary>
public class ExpenseRecord
{
    public int Id { get; set; }
    public string Category { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public string Notes { get; set; } = "";
}

/// <summary>Form input model for adding an expense with full validation.</summary>
public class ExpenseFormViewModel
{
    [Required(ErrorMessage = "Please select an expense category.")]
    [StringLength(60)]
    [Display(Name = "Category")]
    public string Category { get; set; } = "Tools";

    [Required(ErrorMessage = "Amount is required.")]
    [Range(1, 10000000, ErrorMessage = "Amount must be between LKR 1 and LKR 10,000,000.")]
    [Display(Name = "Amount (LKR)")]
    public decimal? Amount { get; set; }

    [Required(ErrorMessage = "Expense date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Date")]
    public DateTime Date { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Please enter notes or description for this expense.")]
    [StringLength(250, MinimumLength = 3, ErrorMessage = "Notes must be between 3 and 250 characters.")]
    [Display(Name = "Notes")]
    public string Notes { get; set; } = "";
}

/// <summary>Summary of expenses per category for the selected month.</summary>
public class ExpenseCategorySummary
{
    public string Category { get; set; } = "";
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
}

/// <summary>View model for the Expense form and list page (GET /Payroll/Expenses).</summary>
public class ExpensePageViewModel
{
    public string SelectedMonth { get; set; } = "";
    public string SelectedMonthLabel { get; set; } = "";
    public string SelectedCategory { get; set; } = "";

    public List<MonthSelectOption> MonthOptions { get; set; } = new();
    public List<string> CategoryOptions { get; set; } = new();
    public List<ExpenseRecord> Items { get; set; } = new();
    public List<ExpenseCategorySummary> CategoryTotals { get; set; } = new();

    public decimal MonthlyTotal => Items.Sum(e => e.Amount);
    public decimal AllCategoriesMonthlyTotal { get; set; }
    public int AllCategoriesCount { get; set; }

    public ExpenseFormViewModel Form { get; set; } = new();
}
