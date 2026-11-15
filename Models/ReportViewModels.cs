namespace AutoTrack.Models.Reports;

// ---------------------------------------------------------------- chart data (sent to Chart.js as JSON)

public class ChartDataset
{
    public string Label { get; set; } = "";
    public decimal[] Data { get; set; } = Array.Empty<decimal>();
}

public class ChartPayload
{
    public string[] Labels { get; set; } = Array.Empty<string>();
    public List<ChartDataset> Datasets { get; set; } = new();
}

/// <summary>Floating bars for the profit "waterfall": each bar runs from Starts[i] to Ends[i].</summary>
public class WaterfallPayload
{
    public string[] Labels { get; set; } = Array.Empty<string>();
    public decimal[] Starts { get; set; } = Array.Empty<decimal>();
    public decimal[] Ends { get; set; } = Array.Empty<decimal>();
    public bool NetIsLoss { get; set; }
}

// ---------------------------------------------------------------- owner dashboard

public class DashboardViewModel
{
    public DateTime Today { get; set; }
    public int TodaysVehicles { get; set; }
    public int OpenJobs { get; set; }
    public int LowStockCount { get; set; }
    public decimal MonthIncome { get; set; }
    public decimal LastMonthIncome { get; set; }
    public int MonthJobsCompleted { get; set; }

    /// <summary>Income so far this month compared with the same days of last month (null when there is nothing to compare).</summary>
    public decimal? IncomeChangePercent =>
        LastMonthIncome > 0 ? Math.Round((MonthIncome - LastMonthIncome) / LastMonthIncome * 100m, 1) : null;

    public ChartPayload Revenue { get; set; } = new();      // 12 months, parts + labour
    public ChartPayload JobStatus { get; set; } = new();    // jobs opened this month by status
    public List<ReportJob> RecentJobs { get; set; } = new();
    public List<ReportPart> LowStockParts { get; set; } = new();
}

// ---------------------------------------------------------------- report 1: monthly revenue

public class MonthlyRevenueRow
{
    public DateTime Month { get; set; }
    public int Jobs { get; set; }
    public decimal PartsRevenue { get; set; }
    public decimal LabourRevenue { get; set; }
    public decimal Total => PartsRevenue + LabourRevenue;
    public decimal AveragePerJob => Jobs == 0 ? 0 : Math.Round(Total / Jobs);
}

public class MonthlyRevenueViewModel
{
    public ReportFilterViewModel Filter { get; set; } = new();
    public bool HasData { get; set; }
    public List<MonthlyRevenueRow> Rows { get; set; } = new();
    public ChartPayload Chart { get; set; } = new();

    public int TotalJobs => Rows.Sum(r => r.Jobs);
    public decimal TotalParts => Rows.Sum(r => r.PartsRevenue);
    public decimal TotalLabour => Rows.Sum(r => r.LabourRevenue);
    public decimal GrandTotal => TotalParts + TotalLabour;
    public decimal AveragePerJob => TotalJobs == 0 ? 0 : Math.Round(GrandTotal / TotalJobs);
}

// ---------------------------------------------------------------- report 2: most-used parts

public class PartUsageRow
{
    public int Rank { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public int QtyUsed { get; set; }
    public int Jobs { get; set; }
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal Margin => Revenue - Cost;
}

public class MostUsedPartsViewModel
{
    public ReportFilterViewModel Filter { get; set; } = new();
    public bool HasData { get; set; }
    public List<PartUsageRow> Rows { get; set; } = new();
    public ChartPayload Chart { get; set; } = new();

    public int TotalQty => Rows.Sum(r => r.QtyUsed);
    public decimal TotalRevenue => Rows.Sum(r => r.Revenue);
    public decimal TotalCost => Rows.Sum(r => r.Cost);
    public decimal TotalMargin => TotalRevenue - TotalCost;
}

// ---------------------------------------------------------------- report 3: mechanic workload

public class MechanicWorkloadRow
{
    public string Mechanic { get; set; } = "";
    public int Assigned { get; set; }
    public int Completed { get; set; }
    public int StillOpen { get; set; }
    public double Hours { get; set; }
    public decimal LabourRevenue { get; set; }
    public decimal SharePercent { get; set; }
    public double AverageHours => Assigned == 0 ? 0 : Math.Round(Hours / Assigned, 1);
}

public class MechanicWorkloadViewModel
{
    public ReportFilterViewModel Filter { get; set; } = new();
    public bool HasData { get; set; }
    public List<MechanicWorkloadRow> Rows { get; set; } = new();
    public List<ReportJob> OpenJobs { get; set; } = new();   // right now, not filtered by date
    public ChartPayload Chart { get; set; } = new();

    public int TotalAssigned => Rows.Sum(r => r.Assigned);
    public int TotalCompleted => Rows.Sum(r => r.Completed);
    public int TotalOpen => Rows.Sum(r => r.StillOpen);
    public double TotalHours => Rows.Sum(r => r.Hours);
    public decimal TotalLabour => Rows.Sum(r => r.LabourRevenue);
}

// ---------------------------------------------------------------- report 4: payroll summary

public class PayrollSummaryRow
{
    public string Employee { get; set; } = "";
    public string Role { get; set; } = "";
    public int Months { get; set; }
    public double Hours { get; set; }
    public double OvertimeHours { get; set; }
    public decimal Basic { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal Gross => Basic + OvertimePay;
    public decimal Epf { get; set; }
    public decimal Net => Gross - Epf;
}

public class PayrollSummaryViewModel
{
    public ReportFilterViewModel Filter { get; set; } = new();
    public bool HasData { get; set; }
    public List<PayrollSummaryRow> Rows { get; set; } = new();
    public ChartPayload Chart { get; set; } = new();     // net pay + EPF per month

    public double TotalHours => Rows.Sum(r => r.Hours);
    public double TotalOvertimeHours => Rows.Sum(r => r.OvertimeHours);
    public decimal TotalBasic => Rows.Sum(r => r.Basic);
    public decimal TotalOvertimePay => Rows.Sum(r => r.OvertimePay);
    public decimal TotalGross => Rows.Sum(r => r.Gross);
    public decimal TotalEpf => Rows.Sum(r => r.Epf);
    public decimal TotalNet => Rows.Sum(r => r.Net);
}

// ---------------------------------------------------------------- month-end profit

public class ExpenseCategoryRow
{
    public string Category { get; set; } = "";
    public int Items { get; set; }
    public decimal Amount { get; set; }
    public decimal SharePercent { get; set; }
}

public class ProfitViewModel
{
    public ProfitFilterViewModel Filter { get; set; } = new();
    public List<(string Value, string Text)> MonthOptions { get; set; } = new();
    public bool HasData { get; set; }
    public DateTime MonthStart { get; set; }
    public bool IsCurrentMonth { get; set; }

    public int Jobs { get; set; }
    public decimal Income { get; set; }
    public decimal PartsCost { get; set; }
    public decimal Salaries { get; set; }
    public decimal Expenses { get; set; }
    public decimal? PreviousNetProfit { get; set; }

    public decimal NetProfit => Income - PartsCost - Salaries - Expenses;
    public decimal MarginPercent => Income == 0 ? 0 : Math.Round(NetProfit / Income * 100m, 1);
    public decimal? ChangeFromPrevious => PreviousNetProfit.HasValue ? NetProfit - PreviousNetProfit.Value : null;

    public List<ExpenseCategoryRow> ExpenseRows { get; set; } = new();
    public WaterfallPayload Waterfall { get; set; } = new();
    public ChartPayload Trend { get; set; } = new();     // last 6 months: income, costs, net profit
}
