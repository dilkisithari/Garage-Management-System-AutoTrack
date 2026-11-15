namespace AutoTrack.Models.Reports;

/// <summary>A spare part in the (fake) stock list. Option 6 owns the real parts module.</summary>
public class ReportPart
{
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public string Category { get; init; } = "";
    public decimal UnitCost { get; init; }
    public decimal UnitPrice { get; init; }
    public int Stock { get; init; }
    public int ReorderLevel { get; init; }
    public string Supplier { get; init; } = "";

    public bool IsLow => Stock < ReorderLevel;
}

/// <summary>One part line used on a job card.</summary>
public class ReportJobPart
{
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public int Qty { get; init; }
    public decimal UnitCost { get; init; }
    public decimal UnitPrice { get; init; }

    public decimal LineRevenue => Qty * UnitPrice;
    public decimal LineCost => Qty * UnitCost;
}

/// <summary>A job card as the reports see it. Income is counted when the job is closed.</summary>
public class ReportJob
{
    public string JobNo { get; set; } = "";
    public string VehicleNo { get; init; } = "";
    public string Customer { get; init; } = "";
    public string Mechanic { get; init; } = "";
    public string Status { get; init; } = "Pending";   // Pending | In Progress | Waiting for Parts | Completed
    public DateTime Opened { get; init; }
    public DateTime? Closed { get; init; }
    public double Hours { get; init; }
    public decimal LabourCharge { get; init; }
    public List<ReportJobPart> Parts { get; init; } = new();

    public bool IsOpen => Status != "Completed";
    public decimal PartsRevenue => Parts.Sum(p => p.LineRevenue);
    public decimal PartsCost => Parts.Sum(p => p.LineCost);
    public decimal Total => PartsRevenue + LabourCharge;
}

/// <summary>One employee's pay for one month.</summary>
public class ReportPayroll
{
    public string Employee { get; init; } = "";
    public string Role { get; init; } = "";
    public DateTime Month { get; init; }          // first day of the pay month
    public decimal HourlyRate { get; init; }
    public double RegularHours { get; init; }
    public double OvertimeHours { get; init; }
    public decimal Basic { get; init; }
    public decimal OvertimePay { get; init; }

    public decimal Gross => Basic + OvertimePay;
    public decimal Epf => Math.Round(Basic * 0.08m);   // employee EPF 8%
    public decimal Net => Gross - Epf;
}

public class ReportExpense
{
    public DateTime Date { get; init; }
    public string Category { get; init; } = "";
    public string Description { get; init; } = "";
    public decimal Amount { get; init; }
}

/// <summary>All the money figures for one calendar month.</summary>
public class MonthFigures
{
    public DateTime Month { get; init; }
    public int Jobs { get; init; }
    public decimal PartsRevenue { get; init; }
    public decimal LabourRevenue { get; init; }
    public decimal PartsCost { get; init; }
    public decimal Salaries { get; init; }
    public decimal Expenses { get; init; }

    public decimal Income => PartsRevenue + LabourRevenue;
    public decimal TotalCosts => PartsCost + Salaries + Expenses;
    public decimal NetProfit => Income - TotalCosts;
}
