namespace AutoTrack.Models.Reports;

/// <summary>
/// Fake data for the dashboard, reports and profit pages (no database yet).
/// Everything is generated with a fixed random seed relative to today's date, so the numbers
/// stay the same between runs and every date filter always has something to show.
/// All reports read from these same lists, so the totals always agree with each other.
/// </summary>
public static class ReportsMockData
{
    public static readonly string[] JobStatuses = { "Pending", "In Progress", "Waiting for Parts", "Completed" };
    public static readonly string[] Mechanics = { "Nuwan Perera", "Saman Kumara", "Kasun Silva", "Dilshan Fernando", "Chaminda Rathnayake" };

    private const decimal LabourRatePerHour = 3000m;

    public static DateTime Today { get; }
    public static DateTime FirstMonth { get; }          // oldest month that has data
    public static IReadOnlyList<ReportPart> Parts { get; }
    public static IReadOnlyList<ReportJob> Jobs { get; }
    public static IReadOnlyList<ReportPayroll> Payroll { get; }
    public static IReadOnlyList<ReportExpense> Expenses { get; }

    private static readonly (string Name, string Role, decimal Rate)[] Employees =
    {
        ("Nuwan Perera",        "Senior Mechanic",  700m),
        ("Saman Kumara",        "Mechanic",         660m),
        ("Kasun Silva",         "Mechanic",         640m),
        ("Dilshan Fernando",    "Mechanic",         620m),
        ("Chaminda Rathnayake", "Mechanic",         600m),
        ("Nimali Perera",       "Front Desk",       480m),
        ("Chamari Jayasinghe",  "Accountant",       560m),
        ("Ruwan Bandara",       "Workshop Helper",  400m),
        ("Tharindu Gamage",     "Workshop Helper",  400m),
    };

    static ReportsMockData()
    {
        Today = DateTime.Today;
        FirstMonth = new DateTime(Today.Year, Today.Month, 1).AddMonths(-11);
        Parts = BuildParts();
        Jobs = BuildJobs();
        Payroll = BuildPayroll();
        Expenses = BuildExpenses();
    }

    // ------------------------------------------------------------------ queries

    /// <summary>Completed jobs that were closed between the two dates (income is counted on the close date).</summary>
    public static IEnumerable<ReportJob> ClosedJobs(DateTime from, DateTime to) =>
        Jobs.Where(j => j.Closed.HasValue && j.Closed.Value.Date >= from.Date && j.Closed.Value.Date <= to.Date);

    public static MonthFigures Month(DateTime anyDayInMonth)
    {
        var start = new DateTime(anyDayInMonth.Year, anyDayInMonth.Month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        var closed = ClosedJobs(start, end).ToList();

        return new MonthFigures
        {
            Month = start,
            Jobs = closed.Count,
            PartsRevenue = closed.Sum(j => j.PartsRevenue),
            LabourRevenue = closed.Sum(j => j.LabourCharge),
            PartsCost = closed.Sum(j => j.PartsCost),
            Salaries = Payroll.Where(p => p.Month == start).Sum(p => p.Gross),
            Expenses = Expenses.Where(e => e.Date >= start && e.Date <= end).Sum(e => e.Amount)
        };
    }

    /// <summary>Figures for the last <paramref name="count"/> months, oldest first, ending with the current month.</summary>
    public static List<MonthFigures> LastMonths(int count)
    {
        var first = new DateTime(Today.Year, Today.Month, 1).AddMonths(-(count - 1));
        return Enumerable.Range(0, count).Select(i => Month(first.AddMonths(i))).ToList();
    }

    // ------------------------------------------------------------------ builders

    private static List<ReportPart> BuildParts() => new()
    {
        new() { Code = "PRT-001", Name = "Engine oil 5W-30 (4 L)",  Category = "Lubricants",   UnitCost = 9800,  UnitPrice = 12500, Stock = 24, ReorderLevel = 10, Supplier = "Ceylon Lubricants" },
        new() { Code = "PRT-002", Name = "Oil filter",              Category = "Filters",      UnitCost = 1450,  UnitPrice = 2100,  Stock = 31, ReorderLevel = 12, Supplier = "Colombo Motor Spares" },
        new() { Code = "PRT-003", Name = "Air filter",              Category = "Filters",      UnitCost = 1900,  UnitPrice = 2800,  Stock = 6,  ReorderLevel = 8,  Supplier = "Colombo Motor Spares" },
        new() { Code = "PRT-004", Name = "Front brake pads",        Category = "Brakes",       UnitCost = 6200,  UnitPrice = 8900,  Stock = 9,  ReorderLevel = 6,  Supplier = "Pettah Auto Parts" },
        new() { Code = "PRT-005", Name = "Spark plug set (4)",      Category = "Ignition",     UnitCost = 4800,  UnitPrice = 6900,  Stock = 4,  ReorderLevel = 6,  Supplier = "Lanka Auto Traders" },
        new() { Code = "PRT-006", Name = "Car battery 45Ah",        Category = "Electrical",   UnitCost = 18500, UnitPrice = 24500, Stock = 5,  ReorderLevel = 4,  Supplier = "Exide Lanka Dealers" },
        new() { Code = "PRT-007", Name = "Wiper blades (pair)",     Category = "Body",         UnitCost = 1800,  UnitPrice = 2900,  Stock = 14, ReorderLevel = 6,  Supplier = "Pettah Auto Parts" },
        new() { Code = "PRT-008", Name = "Clutch plate kit",        Category = "Transmission", UnitCost = 22000, UnitPrice = 29500, Stock = 2,  ReorderLevel = 3,  Supplier = "Lanka Auto Traders" },
        new() { Code = "PRT-009", Name = "Coolant 1 L",             Category = "Fluids",       UnitCost = 1100,  UnitPrice = 1650,  Stock = 18, ReorderLevel = 10, Supplier = "Ceylon Lubricants" },
        new() { Code = "PRT-010", Name = "Timing belt kit",         Category = "Engine",       UnitCost = 14500, UnitPrice = 19800, Stock = 3,  ReorderLevel = 3,  Supplier = "Colombo Motor Spares" },
    };

    private static List<ReportJob> BuildJobs()
    {
        var rng = new Random(2026);

        string[] firstNames = { "Kasun", "Nimal", "Sanduni", "Amal", "Dilani", "Ruwan", "Chathura", "Tharushi", "Mohamed", "Fathima",
                                "Ravindran", "Priyanka", "Sachith", "Hiruni", "Lakmal", "Ishara", "Janaka", "Malini", "Rizwan", "Anjali" };
        string[] lastNames = { "Perera", "Fernando", "Silva", "Jayawardena", "Wickramasinghe", "Gunasekara", "Bandara", "Senanayake",
                               "Rahman", "Selvarajah", "Kumar", "De Silva", "Herath", "Dissanayake", "Amarasinghe" };
        string[] platePrefix = { "CAB", "CAC", "CAD", "CBA", "CBG", "KV", "KY", "KQ", "PE", "QL", "CAF", "BEC" };
        double[] mechanicWeight = { 0.26, 0.22, 0.20, 0.17, 0.15 };
        double[] seasonal = { 0.92, 0.95, 1.00, 1.04, 0.98, 1.06, 1.10, 1.02, 1.00, 0.96, 1.08, 1.12 };

        // Weighted pick of parts: oil and filters are used far more often than a clutch kit.
        double[] partWeight = { 0.22, 0.20, 0.12, 0.10, 0.08, 0.05, 0.07, 0.03, 0.08, 0.03 };

        ReportJob NewJob(DateTime opened, DateTime? closed, string status)
        {
            var hours = Math.Round(2.0 + rng.NextDouble() * 8.0, 1);
            var lines = new List<ReportJobPart>();
            var wanted = 1 + rng.Next(0, 3) + (rng.NextDouble() < 0.2 ? 1 : 0);
            for (var n = 0; n < wanted; n++)
            {
                var idx = Pick(rng, partWeight);
                var part = Parts[idx];
                if (lines.Any(l => l.Code == part.Code)) continue;
                var qty = part.Code == "PRT-009" ? rng.Next(1, 4) : (rng.NextDouble() < 0.15 ? 2 : 1);
                lines.Add(new ReportJobPart { Code = part.Code, Name = part.Name, Qty = qty, UnitCost = part.UnitCost, UnitPrice = part.UnitPrice });
            }

            return new ReportJob
            {
                VehicleNo = $"{platePrefix[rng.Next(platePrefix.Length)]}-{rng.Next(1000, 9999)}",
                Customer = $"{firstNames[rng.Next(firstNames.Length)]} {lastNames[rng.Next(lastNames.Length)]}",
                Mechanic = Mechanics[Pick(rng, mechanicWeight)],
                Status = status,
                Opened = opened,
                Closed = closed,
                Hours = hours,
                LabourCharge = Math.Round((decimal)hours * LabourRatePerHour / 100m) * 100m,
                Parts = lines
            };
        }

        string OpenStatus() => new[] { "Pending", "In Progress", "In Progress", "Waiting for Parts" }[rng.Next(4)];

        var jobs = new List<ReportJob>();

        for (var m = 0; m < 12; m++)
        {
            var monthStart = FirstMonth.AddMonths(m);
            var daysInMonth = DateTime.DaysInMonth(monthStart.Year, monthStart.Month);
            var lastDay = monthStart.AddMonths(1).AddDays(-1);
            var limit = lastDay > Today ? Today : lastDay;
            var daysAvailable = (limit - monthStart).Days + 1;
            var growth = 1.0 + m * 0.012;
            var count = (int)Math.Round((54 + rng.Next(0, 9)) * seasonal[m] * growth * daysAvailable / daysInMonth);

            for (var i = 0; i < count; i++)
            {
                var opened = monthStart.AddDays(rng.Next(daysAvailable));
                if (opened.DayOfWeek == DayOfWeek.Sunday)                       // the garage is closed on Sundays
                    opened = opened.AddDays(opened < limit ? 1 : -1);

                var age = (Today - opened).Days;
                var stillOpen = age == 0 || (age <= 5 && rng.NextDouble() < 0.55);

                if (stillOpen)
                {
                    jobs.Add(NewJob(opened, null, OpenStatus()));
                }
                else
                {
                    var closed = opened.AddDays(rng.Next(0, 4));
                    if (closed > Today) closed = Today;
                    jobs.Add(NewJob(opened, closed, "Completed"));
                }
            }
        }

        // The dashboard should always show a few vehicles that came in today.
        var openedToday = jobs.Count(j => j.Opened.Date == Today);
        for (var i = openedToday; i < 6; i++) jobs.Add(NewJob(Today, null, OpenStatus()));

        var ordered = jobs.OrderBy(j => j.Opened).ThenBy(j => j.VehicleNo).ToList();
        for (var i = 0; i < ordered.Count; i++) ordered[i].JobNo = $"JC-{i + 1:0000}";
        return ordered;
    }

    private static List<ReportPayroll> BuildPayroll()
    {
        var rng = new Random(77);
        var list = new List<ReportPayroll>();

        for (var m = 0; m < 12; m++)
        {
            var monthStart = FirstMonth.AddMonths(m);
            var isCurrent = monthStart.Year == Today.Year && monthStart.Month == Today.Month;
            // This month's pay is "month to date", so only the days that have passed are counted.
            var fraction = isCurrent ? (double)Today.Day / DateTime.DaysInMonth(Today.Year, Today.Month) : 1.0;

            foreach (var e in Employees)
            {
                var isMechanic = e.Role.Contains("Mechanic");
                var regular = Math.Round((176 - rng.Next(0, 9)) * fraction, 1);
                var overtime = Math.Round((isMechanic ? rng.Next(4, 20) : rng.Next(0, 8)) * fraction, 1);

                list.Add(new ReportPayroll
                {
                    Employee = e.Name,
                    Role = e.Role,
                    Month = monthStart,
                    HourlyRate = e.Rate,
                    RegularHours = regular,
                    OvertimeHours = overtime,
                    Basic = Math.Round(e.Rate * (decimal)regular),
                    OvertimePay = Math.Round(e.Rate * 1.5m * (decimal)overtime)
                });
            }
        }
        return list;
    }

    private static List<ReportExpense> BuildExpenses()
    {
        var rng = new Random(31);
        var list = new List<ReportExpense>();
        string[] tools = { "Torque wrench set", "Hydraulic jack service", "OBD scanner update", "Impact wrench", "Workshop creeper and stands", "Tyre changer parts" };

        for (var m = 0; m < 12; m++)
        {
            var d = FirstMonth.AddMonths(m);

            void Add(int day, string category, string description, decimal amount)
            {
                var date = new DateTime(d.Year, d.Month, Math.Min(day, DateTime.DaysInMonth(d.Year, d.Month)));
                if (date <= Today && amount > 0)
                    list.Add(new ReportExpense { Date = date, Category = category, Description = description, Amount = amount });
            }

            Add(1,  "Rent",                  "Workshop rent, Battaramulla", 150000);
            Add(10, "Electricity",           "CEB electricity bill", rng.Next(36000, 48000));
            Add(12, "Water",                 "Water Board bill", rng.Next(5500, 8500));
            Add(15, "Internet & Phone",      "Fibre broadband and mobile lines", 9500);
            Add(rng.Next(5, 25), "Tools & Equipment", tools[rng.Next(tools.Length)], rng.Next(12000, 55000));
            Add(20, "Marketing",             "Facebook and Google ads", rng.NextDouble() < 0.7 ? rng.Next(8000, 22000) : 0);
            Add(25, "Cleaning & Consumables", "Workshop cleaning, rags, gloves", rng.Next(7000, 13000));
        }
        return list.OrderBy(e => e.Date).ToList();
    }

    private static int Pick(Random rng, double[] weights)
    {
        var roll = rng.NextDouble() * weights.Sum();
        for (var i = 0; i < weights.Length; i++)
        {
            roll -= weights[i];
            if (roll <= 0) return i;
        }
        return weights.Length - 1;
    }
}
