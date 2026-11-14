using System.Globalization;
using AutoTrack.Models.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoTrack.Controllers;

/// <summary>
/// Option 10 - Owner dashboard, report pages and month-end profit.
/// All data comes from ReportsMockData until the database is connected.
/// </summary>
[Authorize(Roles = "Owner")]
public class ReportsController : Controller
{
    private static DateTime Today => ReportsMockData.Today;

    // ------------------------------------------------------------------ reports hub

    public IActionResult Index() => View();

    // ------------------------------------------------------------------ 1. owner dashboard

    public IActionResult Dashboard()
    {
        var thisMonth = ReportsMockData.Month(Today);
        // Compare with the same number of days last month, because this month is not finished yet.
        var lastMonthStart = new DateTime(Today.Year, Today.Month, 1).AddMonths(-1);
        var sameDayLastMonth = lastMonthStart.AddDays(Math.Min(Today.Day, DateTime.DaysInMonth(lastMonthStart.Year, lastMonthStart.Month)) - 1);
        var lastMonthIncome = ReportsMockData.ClosedJobs(lastMonthStart, sameDayLastMonth).Sum(j => j.Total);
        var months = ReportsMockData.LastMonths(12);
        var monthStart = new DateTime(Today.Year, Today.Month, 1);
        var jobsThisMonth = ReportsMockData.Jobs.Where(j => j.Opened >= monthStart).ToList();

        var model = new DashboardViewModel
        {
            Today = Today,
            TodaysVehicles = ReportsMockData.Jobs.Where(j => j.Opened.Date == Today).Select(j => j.VehicleNo).Distinct().Count(),
            OpenJobs = ReportsMockData.Jobs.Count(j => j.IsOpen),
            LowStockCount = ReportsMockData.Parts.Count(p => p.IsLow),
            MonthIncome = thisMonth.Income,
            LastMonthIncome = lastMonthIncome,
            MonthJobsCompleted = thisMonth.Jobs,
            Revenue = new ChartPayload
            {
                Labels = months.Select(m => ReportFormat.Month(m.Month)).ToArray(),
                Datasets =
                {
                    new ChartDataset { Label = "Labour",  Data = months.Select(m => m.LabourRevenue).ToArray() },
                    new ChartDataset { Label = "Parts",   Data = months.Select(m => m.PartsRevenue).ToArray() }
                }
            },
            JobStatus = new ChartPayload
            {
                Labels = ReportsMockData.JobStatuses,
                Datasets =
                {
                    new ChartDataset
                    {
                        Label = "Jobs",
                        Data = ReportsMockData.JobStatuses
                            .Select(s => (decimal)jobsThisMonth.Count(j => j.Status == s)).ToArray()
                    }
                }
            },
            RecentJobs = ReportsMockData.Jobs.OrderByDescending(j => j.Opened).ThenByDescending(j => j.JobNo).Take(8).ToList(),
            LowStockParts = ReportsMockData.Parts.Where(p => p.IsLow).OrderBy(p => p.Stock - p.ReorderLevel).ToList()
        };

        return View(model);
    }

    // ------------------------------------------------------------------ 2a. monthly revenue

    public IActionResult MonthlyRevenue(ReportFilterViewModel filter)
    {
        var model = new MonthlyRevenueViewModel { Filter = PrepareFilter(filter) };
        if (!ModelState.IsValid) return View(model);

        var rows = ReportsMockData.ClosedJobs(filter.From!.Value, filter.To!.Value)
            .GroupBy(j => new DateTime(j.Closed!.Value.Year, j.Closed.Value.Month, 1))
            .OrderBy(g => g.Key)
            .Select(g => new MonthlyRevenueRow
            {
                Month = g.Key,
                Jobs = g.Count(),
                PartsRevenue = g.Sum(j => j.PartsRevenue),
                LabourRevenue = g.Sum(j => j.LabourCharge)
            }).ToList();

        model.Rows = rows;
        model.HasData = rows.Count > 0;
        model.Chart = new ChartPayload
        {
            Labels = rows.Select(r => ReportFormat.Month(r.Month)).ToArray(),
            Datasets =
            {
                new ChartDataset { Label = "Labour", Data = rows.Select(r => r.LabourRevenue).ToArray() },
                new ChartDataset { Label = "Parts",  Data = rows.Select(r => r.PartsRevenue).ToArray() }
            }
        };
        return View(model);
    }

    // ------------------------------------------------------------------ 2b. most-used parts

    public IActionResult MostUsedParts(ReportFilterViewModel filter)
    {
        var model = new MostUsedPartsViewModel { Filter = PrepareFilter(filter) };
        if (!ModelState.IsValid) return View(model);

        var rows = ReportsMockData.ClosedJobs(filter.From!.Value, filter.To!.Value)
            .SelectMany(j => j.Parts.Select(p => (Job: j, Part: p)))
            .GroupBy(x => x.Part.Code)
            .Select(g =>
            {
                var info = ReportsMockData.Parts.First(p => p.Code == g.Key);
                return new PartUsageRow
                {
                    Code = g.Key,
                    Name = info.Name,
                    Category = info.Category,
                    QtyUsed = g.Sum(x => x.Part.Qty),
                    Jobs = g.Select(x => x.Job.JobNo).Distinct().Count(),
                    Revenue = g.Sum(x => x.Part.LineRevenue),
                    Cost = g.Sum(x => x.Part.LineCost)
                };
            })
            .OrderByDescending(r => r.QtyUsed).ThenByDescending(r => r.Revenue)
            .Take(filter.Top ?? 10)
            .ToList();

        for (var i = 0; i < rows.Count; i++) rows[i].Rank = i + 1;

        model.Rows = rows;
        model.HasData = rows.Count > 0;
        model.Chart = new ChartPayload
        {
            Labels = rows.Select(r => r.Name).ToArray(),
            Datasets = { new ChartDataset { Label = "Quantity used", Data = rows.Select(r => (decimal)r.QtyUsed).ToArray() } }
        };
        return View(model);
    }

    // ------------------------------------------------------------------ 2c. mechanic workload

    public IActionResult MechanicWorkload(ReportFilterViewModel filter)
    {
        var model = new MechanicWorkloadViewModel { Filter = PrepareFilter(filter) };
        if (!ModelState.IsValid) return View(model);

        // Workload = jobs that were opened (assigned) in the period, whatever their status now.
        var jobs = ReportsMockData.Jobs
            .Where(j => j.Opened.Date >= filter.From!.Value.Date && j.Opened.Date <= filter.To!.Value.Date)
            .ToList();

        var rows = ReportsMockData.Mechanics.Select(name =>
        {
            var mine = jobs.Where(j => j.Mechanic == name).ToList();
            return new MechanicWorkloadRow
            {
                Mechanic = name,
                Assigned = mine.Count,
                Completed = mine.Count(j => !j.IsOpen),
                StillOpen = mine.Count(j => j.IsOpen),
                Hours = Math.Round(mine.Sum(j => j.Hours), 1),
                LabourRevenue = mine.Sum(j => j.LabourCharge)
            };
        }).OrderByDescending(r => r.Assigned).ToList();

        var totalAssigned = rows.Sum(r => r.Assigned);
        foreach (var r in rows)
            r.SharePercent = totalAssigned == 0 ? 0 : Math.Round(r.Assigned * 100m / totalAssigned, 1);

        model.Rows = rows;
        model.HasData = totalAssigned > 0;
        model.OpenJobs = ReportsMockData.Jobs.Where(j => j.IsOpen)
            .OrderBy(j => j.Mechanic).ThenBy(j => j.Opened).ToList();
        model.Chart = new ChartPayload
        {
            Labels = rows.Select(r => r.Mechanic).ToArray(),
            Datasets =
            {
                new ChartDataset { Label = "Completed", Data = rows.Select(r => (decimal)r.Completed).ToArray() },
                new ChartDataset { Label = "Still open", Data = rows.Select(r => (decimal)r.StillOpen).ToArray() }
            }
        };
        return View(model);
    }

    // ------------------------------------------------------------------ 2d. payroll summary

    public IActionResult PayrollSummary(ReportFilterViewModel filter)
    {
        var model = new PayrollSummaryViewModel { Filter = PrepareFilter(filter) };
        if (!ModelState.IsValid) return View(model);

        // Payroll is monthly, so every pay month that touches the chosen dates is included.
        var fromMonth = new DateTime(filter.From!.Value.Year, filter.From.Value.Month, 1);
        var toMonth = new DateTime(filter.To!.Value.Year, filter.To.Value.Month, 1);
        var records = ReportsMockData.Payroll.Where(p => p.Month >= fromMonth && p.Month <= toMonth).ToList();

        model.Rows = records.GroupBy(p => p.Employee).Select(g => new PayrollSummaryRow
        {
            Employee = g.Key,
            Role = g.First().Role,
            Months = g.Count(),
            Hours = g.Sum(p => p.RegularHours),
            OvertimeHours = g.Sum(p => p.OvertimeHours),
            Basic = g.Sum(p => p.Basic),
            OvertimePay = g.Sum(p => p.OvertimePay),
            Epf = g.Sum(p => p.Epf)
        }).OrderByDescending(r => r.Net).ToList();

        model.HasData = model.Rows.Count > 0;

        var byMonth = records.GroupBy(p => p.Month).OrderBy(g => g.Key).ToList();
        model.Chart = new ChartPayload
        {
            Labels = byMonth.Select(g => ReportFormat.Month(g.Key)).ToArray(),
            Datasets =
            {
                new ChartDataset { Label = "Net salaries", Data = byMonth.Select(g => g.Sum(p => p.Net)).ToArray() },
                new ChartDataset { Label = "EPF (8%)",     Data = byMonth.Select(g => g.Sum(p => p.Epf)).ToArray() }
            }
        };
        return View(model);
    }

    // ------------------------------------------------------------------ 3. month-end profit

    public IActionResult Profit(ProfitFilterViewModel filter)
    {
        var lastCompleted = new DateTime(Today.Year, Today.Month, 1).AddMonths(-1);
        var model = new ProfitViewModel { MonthOptions = BuildMonthOptions() };

        if (!Request.Query.ContainsKey(nameof(filter.Month)))
        {
            ModelState.Clear();
            filter.Month = lastCompleted.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        }
        model.Filter = filter;

        if (!ModelState.IsValid) return View(model);

        // Only months that are in the drop-down are accepted.
        if (!DateTime.TryParseExact(filter.Month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var month)
            || model.MonthOptions.All(o => o.Value != filter.Month))
        {
            ModelState.AddModelError(nameof(filter.Month), "Choose a month from the list.");
            return View(model);
        }

        var figures = ReportsMockData.Month(month);
        var previous = ReportsMockData.Month(month.AddMonths(-1));
        var monthEnd = month.AddMonths(1).AddDays(-1);

        model.MonthStart = month;
        model.IsCurrentMonth = month.Year == Today.Year && month.Month == Today.Month;
        model.Jobs = figures.Jobs;
        model.Income = figures.Income;
        model.PartsCost = figures.PartsCost;
        model.Salaries = figures.Salaries;
        model.Expenses = figures.Expenses;
        model.PreviousNetProfit = month > ReportsMockData.FirstMonth ? previous.NetProfit : null;
        model.HasData = figures.Jobs > 0 || figures.Expenses > 0;

        var monthExpenses = ReportsMockData.Expenses.Where(e => e.Date >= month && e.Date <= monthEnd).ToList();
        model.ExpenseRows = monthExpenses.GroupBy(e => e.Category).Select(g => new ExpenseCategoryRow
        {
            Category = g.Key,
            Items = g.Count(),
            Amount = g.Sum(e => e.Amount),
            SharePercent = figures.Expenses == 0 ? 0 : Math.Round(g.Sum(e => e.Amount) * 100m / figures.Expenses, 1)
        }).OrderByDescending(r => r.Amount).ToList();

        // Waterfall: income, then each cost taken off, ending at net profit.
        var afterParts = model.Income - model.PartsCost;
        var afterSalaries = afterParts - model.Salaries;
        model.Waterfall = new WaterfallPayload
        {
            Labels = new[] { "Income", "Parts cost", "Salaries", "Expenses", "Net profit" },
            Starts = new[] { 0m, model.Income, afterParts, afterSalaries, 0m },
            Ends = new[] { model.Income, afterParts, afterSalaries, model.NetProfit, model.NetProfit },
            NetIsLoss = model.NetProfit < 0
        };

        // Last 6 months ending with the chosen month (never before the first month we have data for).
        var trend = Enumerable.Range(0, 6).Select(i => month.AddMonths(i - 5))
            .Where(m => m >= ReportsMockData.FirstMonth).Select(ReportsMockData.Month).ToList();
        model.Trend = new ChartPayload
        {
            Labels = trend.Select(t => ReportFormat.Month(t.Month)).ToArray(),
            Datasets =
            {
                new ChartDataset { Label = "Income",     Data = trend.Select(t => t.Income).ToArray() },
                new ChartDataset { Label = "Total costs", Data = trend.Select(t => t.TotalCosts).ToArray() },
                new ChartDataset { Label = "Net profit", Data = trend.Select(t => t.NetProfit).ToArray() }
            }
        };

        return View(model);
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>
    /// First visit (no query string): fill in a sensible default period (last 6 months up to today)
    /// and hide the "required" errors. Later visits keep what the user typed so errors show properly.
    /// </summary>
    private ReportFilterViewModel PrepareFilter(ReportFilterViewModel filter)
    {
        var hasQuery = Request.Query.ContainsKey(nameof(filter.From))
                       || Request.Query.ContainsKey(nameof(filter.To))
                       || Request.Query.ContainsKey(nameof(filter.Top));
        if (!hasQuery)
        {
            ModelState.Clear();
            filter.From = new DateTime(Today.Year, Today.Month, 1).AddMonths(-5);
            filter.To = Today;
        }

        // "Show top" only matters for the parts report, but it needs a default there.
        if (ControllerContext.ActionDescriptor.ActionName == nameof(MostUsedParts) && !filter.Top.HasValue && ModelState.IsValid)
            filter.Top = 10;

        return filter;
    }

    private static List<(string Value, string Text)> BuildMonthOptions()
    {
        var current = new DateTime(Today.Year, Today.Month, 1);
        return Enumerable.Range(0, 12).Select(i => current.AddMonths(-i))
            .Select(m => (m.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                          ReportFormat.MonthLong(m) + (m == current ? " (month to date)" : "")))
            .ToList();
    }
}
