using System.Globalization;

namespace AutoTrack.Models.Payroll;

/// <summary>
/// Thread-safe in-memory mock data store for Payroll &amp; Expenses (Option 9).
/// Replace with EF Core / database queries when implementing the backend.
/// </summary>
public static class MockPayrollData
{
    public static readonly string[] ExpenseCategories =
    {
        "Electricity",
        "Rent",
        "Tools",
        "Water",
        "Workshop Supplies",
        "Equipment Maintenance",
        "Transport",
        "Miscellaneous"
    };

    private static readonly object Sync = new();

    // Tracks which yyyy-MM months have had payroll run in this session
    private static readonly Dictionary<string, (DateTime ProcessedAt, string ProcessedBy)> ProcessedMonths =
        InitializeProcessedMonths();

    // Base staff roster with Sri Lankan names and LKR hourly rates
    private static readonly List<(int Id, string Name, string Role, decimal Rate, double BaseHours, double BaseOt)> StaffSeed = new()
    {
        (1, "Nuwan Perera",        "Senior Mechanic", 950m,  176, 18.5),
        (2, "Kasun Fernando",      "Mechanic",        750m,  172, 14.0),
        (3, "Chaminda Silva",      "Senior Mechanic", 920m,  180, 22.0),
        (4, "Ruwan Jayawardena",   "Mechanic",        720m,  168, 10.5),
        (5, "Dilshan Bandara",     "Front Desk",      680m,  176,  6.0),
        (6, "Tharindu Wijesinghe", "Mechanic",        740m,  164, 12.0),
        (7, "Saman Kumara",        "Workshop Helper", 520m,  176, 16.0),
        (8, "Nadeesha Perera",     "Accountant",      850m,  168,  4.0),
        (9, "Isuru Rajapaksa",     "Workshop Helper", 540m,  160,  8.5)
    };

    private static readonly List<ExpenseRecord> Expenses = SeedExpenses();
    private static int _nextExpenseId = Expenses.Count + 1;

    private static Dictionary<string, (DateTime ProcessedAt, string ProcessedBy)> InitializeProcessedMonths()
    {
        var prevMonth = DateTime.Today.AddMonths(-1).ToString("yyyy-MM", CultureInfo.InvariantCulture);
        var twoMonthsAgo = DateTime.Today.AddMonths(-2).ToString("yyyy-MM", CultureInfo.InvariantCulture);
        return new Dictionary<string, (DateTime, string)>(StringComparer.Ordinal)
        {
            [twoMonthsAgo] = (DateTime.Today.AddMonths(-2).AddDays(27), "Owner"),
            [prevMonth] = (DateTime.Today.AddMonths(-1).AddDays(27), "Owner")
        };
    }

    private static List<ExpenseRecord> SeedExpenses()
    {
        var today = DateTime.Today;
        var currentMonthStart = new DateTime(today.Year, today.Month, 1);
        var prevMonthStart = currentMonthStart.AddMonths(-1);

        return new List<ExpenseRecord>
        {
            new()
            {
                Id = 1,
                Category = "Rent",
                Amount = 145000m,
                Date = currentMonthStart.AddDays(1),
                Notes = "Monthly garage bay & office rent - Battaramulla"
            },
            new()
            {
                Id = 2,
                Category = "Electricity",
                Amount = 38450m,
                Date = currentMonthStart.AddDays(4),
                Notes = "CEB 3-phase workshop power bill (compressor & lifts)"
            },
            new()
            {
                Id = 3,
                Category = "Tools",
                Amount = 24800m,
                Date = currentMonthStart.AddDays(6),
                Notes = "Torque wrench & OBD-II diagnostic cable for CAB-1234"
            },
            new()
            {
                Id = 4,
                Category = "Workshop Supplies",
                Amount = 18600m,
                Date = currentMonthStart.AddDays(8),
                Notes = "Brake cleaner, degreaser, shop rags & nitrile gloves"
            },
            new()
            {
                Id = 5,
                Category = "Water",
                Amount = 6400m,
                Date = currentMonthStart.AddDays(9),
                Notes = "National Water Supply bill - wash bay"
            },
            new()
            {
                Id = 6,
                Category = "Equipment Maintenance",
                Amount = 15200m,
                Date = currentMonthStart.AddDays(11),
                Notes = "Hydraulic two-post lift inspection & oil top-up"
            },
            new()
            {
                Id = 7,
                Category = "Transport",
                Amount = 4750m,
                Date = currentMonthStart.AddDays(13),
                Notes = "Spare parts pickup from Panchikawatta for WP-KI-4521"
            },
            new()
            {
                Id = 8,
                Category = "Tools",
                Amount = 11900m,
                Date = currentMonthStart.AddDays(15),
                Notes = "Impact socket set (1/2 inch metric) for bay 2"
            },
            new()
            {
                Id = 9,
                Category = "Miscellaneous",
                Amount = 5300m,
                Date = currentMonthStart.AddDays(17),
                Notes = "Staff tea, refreshments & first-aid kit refill"
            },
            new()
            {
                Id = 10,
                Category = "Rent",
                Amount = 145000m,
                Date = prevMonthStart.AddDays(1),
                Notes = "Previous month garage bay & office rent"
            },
            new()
            {
                Id = 11,
                Category = "Electricity",
                Amount = 36900m,
                Date = prevMonthStart.AddDays(5),
                Notes = "Previous month CEB 3-phase workshop bill"
            }
        };
    }

    // ── Month helpers ──────────────────────────────────────────────────────────

    public static List<MonthSelectOption> BuildMonthOptions(int count = 6)
    {
        var list = new List<MonthSelectOption>(count);
        var current = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        for (var i = 0; i < count; i++)
        {
            var m = current.AddMonths(-i);
            list.Add(new MonthSelectOption
            {
                Value = m.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                Label = m.ToString("MMMM yyyy", CultureInfo.InvariantCulture)
            });
        }
        return list;
    }

    public static string NormalizeMonthKey(string? month, List<MonthSelectOption> options)
    {
        if (!string.IsNullOrWhiteSpace(month) && options.Any(o => o.Value == month))
            return month;

        return options.First().Value;
    }

    // ── Payroll queries & commands ─────────────────────────────────────────────

    public static PayrollRunViewModel GetPayrollRun(string? month)
    {
        var options = BuildMonthOptions();
        var monthKey = NormalizeMonthKey(month, options);
        var monthStart = DateTime.ParseExact(monthKey, "yyyy-MM", CultureInfo.InvariantCulture);

        lock (Sync)
        {
            var isProcessed = ProcessedMonths.TryGetValue(monthKey, out var info);
            var rows = BuildRowsForMonth(monthStart, isProcessed);

            return new PayrollRunViewModel
            {
                SelectedMonth = monthKey,
                SelectedMonthLabel = monthStart.ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                MonthOptions = options,
                Rows = rows,
                IsMonthProcessed = isProcessed,
                ProcessedAt = isProcessed ? info.ProcessedAt : null,
                ProcessedBy = isProcessed ? info.ProcessedBy : null
            };
        }
    }

    public static (bool Ok, string Message) RunPayroll(string monthKey, string runBy)
    {
        var options = BuildMonthOptions();
        if (!options.Any(o => o.Value == monthKey))
            return (false, "Invalid payroll month selected.");

        lock (Sync)
        {
            if (ProcessedMonths.ContainsKey(monthKey))
                return (false, $"Payroll for {monthKey} has already been processed.");

            ProcessedMonths[monthKey] = (DateTime.Now, string.IsNullOrWhiteSpace(runBy) ? "Owner" : runBy);
            var monthStart = DateTime.ParseExact(monthKey, "yyyy-MM", CultureInfo.InvariantCulture);
            return (true, $"Payroll for {monthStart:MMMM yyyy} processed successfully for {StaffSeed.Count} employees.");
        }
    }

    public static PayslipViewModel? GetPayslip(int employeeId, string? month)
    {
        var options = BuildMonthOptions();
        var monthKey = NormalizeMonthKey(month, options);
        var monthStart = DateTime.ParseExact(monthKey, "yyyy-MM", CultureInfo.InvariantCulture);
        var monthEnd = new DateTime(monthStart.Year, monthStart.Month, DateTime.DaysInMonth(monthStart.Year, monthStart.Month));

        lock (Sync)
        {
            var isProcessed = ProcessedMonths.TryGetValue(monthKey, out var info);
            var row = BuildRowsForMonth(monthStart, isProcessed).FirstOrDefault(r => r.EmployeeId == employeeId);
            if (row == null) return null;

            return new PayslipViewModel
            {
                MonthKey = monthKey,
                MonthLabel = monthStart.ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                PayPeriod = $"{monthStart:dd MMM yyyy} – {monthEnd:dd MMM yyyy}",
                PayslipNumber = $"PS-{monthStart:yyyyMM}-{employeeId:D3}",
                GeneratedDate = DateTime.Today,
                Row = row,
                PaidAt = isProcessed ? info.ProcessedAt : null
            };
        }
    }

    private static List<PayrollEmployeeRow> BuildRowsForMonth(DateTime monthStart, bool isProcessed)
    {
        // Deterministic slight variation per month so switching months shows realistic changes
        var monthSeed = (monthStart.Year * 12 + monthStart.Month) % 5;

        return StaffSeed.Select((s, idx) =>
        {
            var hourDelta = ((idx + monthSeed) % 3 - 1) * 4;
            var otDelta = ((idx + monthSeed) % 4 - 1) * 1.5;

            return new PayrollEmployeeRow
            {
                EmployeeId = s.Id,
                EmployeeName = s.Name,
                Role = s.Role,
                HourlyRate = s.Rate,
                RegularHours = Math.Max(140, s.BaseHours + hourDelta),
                OvertimeHours = Math.Max(0, Math.Round(s.BaseOt + otDelta, 1)),
                IsPaid = isProcessed
            };
        }).ToList();
    }

    // ── Expense queries & commands ─────────────────────────────────────────────

    public static ExpensePageViewModel GetExpensesPage(string? month, string? category, ExpenseFormViewModel? form = null)
    {
        var options = BuildMonthOptions();
        var monthKey = NormalizeMonthKey(month, options);
        var monthStart = DateTime.ParseExact(monthKey, "yyyy-MM", CultureInfo.InvariantCulture);
        var normalizedCategory = string.IsNullOrWhiteSpace(category) ? "" : category.Trim();

        lock (Sync)
        {
            var monthItems = Expenses
                .Where(e => e.Date.Year == monthStart.Year && e.Date.Month == monthStart.Month)
                .OrderByDescending(e => e.Date)
                .ThenByDescending(e => e.Id)
                .Select(CloneExpense)
                .ToList();

            var filtered = string.IsNullOrEmpty(normalizedCategory)
                ? monthItems
                : monthItems.Where(e => string.Equals(e.Category, normalizedCategory, StringComparison.OrdinalIgnoreCase)).ToList();

            var categoryTotals = monthItems
                .GroupBy(e => e.Category)
                .Select(g => new ExpenseCategorySummary
                {
                    Category = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(x => x.Amount)
                })
                .OrderByDescending(c => c.TotalAmount)
                .ToList();

            return new ExpensePageViewModel
            {
                SelectedMonth = monthKey,
                SelectedMonthLabel = monthStart.ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                SelectedCategory = normalizedCategory,
                MonthOptions = options,
                CategoryOptions = ExpenseCategories.ToList(),
                Items = filtered,
                CategoryTotals = categoryTotals,
                AllCategoriesMonthlyTotal = monthItems.Sum(e => e.Amount),
                AllCategoriesCount = monthItems.Count,
                Form = form ?? new ExpenseFormViewModel { Date = DateTime.Today }
            };
        }
    }

    public static ExpenseRecord AddExpense(ExpenseFormViewModel form)
    {
        lock (Sync)
        {
            var record = new ExpenseRecord
            {
                Id = _nextExpenseId++,
                Category = form.Category.Trim(),
                Amount = Math.Round(form.Amount ?? 0m, 2),
                Date = form.Date.Date,
                Notes = form.Notes.Trim()
            };
            Expenses.Add(record);
            return CloneExpense(record);
        }
    }

    public static bool DeleteExpense(int id)
    {
        lock (Sync)
        {
            var match = Expenses.FirstOrDefault(e => e.Id == id);
            if (match == null) return false;
            Expenses.Remove(match);
            return true;
        }
    }

    private static ExpenseRecord CloneExpense(ExpenseRecord e) => new()
    {
        Id = e.Id,
        Category = e.Category,
        Amount = e.Amount,
        Date = e.Date,
        Notes = e.Notes
    };
}
