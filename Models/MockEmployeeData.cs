namespace AutoTrack.Models;

/// <summary>
/// Thread-safe in-memory store for Employees and Attendance (Option 8).
/// Replace with EF Core in the database phase.
/// </summary>
public static class MockEmployeeData
{
    public static readonly string[] Roles =
    {
        "Owner",
        "Manager",
        "Front Desk",
        "Accountant",
        "Senior Mechanic",
        "Mechanic",
        "Workshop Helper"
    };

    private static readonly object Sync = new();
    private static readonly List<EmployeeRecord> Employees = SeedEmployees();
    private static readonly List<AttendanceRecord> Attendance = SeedAttendance();

    // ── Employee queries & commands ────────────────────────────────────────────

    public static List<EmployeeRecord> All()
    {
        lock (Sync)
        {
            return Employees
                .OrderByDescending(e => e.IsActive)
                .ThenBy(e => e.FullName)
                .Select(CloneEmployee)
                .ToList();
        }
    }

    public static List<EmployeeRecord> Active()
    {
        lock (Sync)
        {
            return Employees
                .Where(e => e.IsActive)
                .OrderBy(e => e.FullName)
                .Select(CloneEmployee)
                .ToList();
        }
    }

    public static EmployeeRecord? Get(int id)
    {
        lock (Sync)
        {
            var match = Employees.FirstOrDefault(e => e.Id == id);
            return match == null ? null : CloneEmployee(match);
        }
    }

    public static EmployeeRecord? GetByUsername(string? username)
    {
        if (string.IsNullOrWhiteSpace(username)) return null;
        lock (Sync)
        {
            var match = Employees.FirstOrDefault(e =>
                !string.IsNullOrWhiteSpace(e.Username) &&
                string.Equals(e.Username.Trim(), username.Trim(), StringComparison.OrdinalIgnoreCase));
            return match == null ? null : CloneEmployee(match);
        }
    }

    public static List<EmployeeRecord> Search(string? term, string? status)
    {
        lock (Sync)
        {
            IEnumerable<EmployeeRecord> query = Employees;

            if (string.Equals(status, "active", StringComparison.OrdinalIgnoreCase))
                query = query.Where(e => e.IsActive);
            else if (string.Equals(status, "inactive", StringComparison.OrdinalIgnoreCase))
                query = query.Where(e => !e.IsActive);

            if (!string.IsNullOrWhiteSpace(term))
            {
                var t = term.Trim().ToLowerInvariant();
                query = query.Where(e =>
                    e.FullName.ToLowerInvariant().Contains(t) ||
                    e.Role.ToLowerInvariant().Contains(t) ||
                    e.Phone.ToLowerInvariant().Contains(t));
            }

            return query
                .OrderByDescending(e => e.IsActive)
                .ThenBy(e => e.FullName)
                .Select(CloneEmployee)
                .ToList();
        }
    }

    public static void Save(EmployeeRecord employee)
    {
        lock (Sync)
        {
            if (employee.Id == 0)
            {
                employee.Id = Employees.Count == 0 ? 1 : Employees.Max(e => e.Id) + 1;
                employee.JoinedDate = DateTime.Today;
                Employees.Add(CloneEmployee(employee));
            }
            else
            {
                var index = Employees.FindIndex(e => e.Id == employee.Id);
                if (index >= 0)
                {
                    employee.JoinedDate = Employees[index].JoinedDate;
                    Employees[index] = CloneEmployee(employee);
                }
            }
        }
    }

    public static bool SetActive(int id, bool isActive)
    {
        lock (Sync)
        {
            var employee = Employees.FirstOrDefault(e => e.Id == id);
            if (employee == null) return false;
            employee.IsActive = isActive;
            return true;
        }
    }

    // ── Attendance queries & commands ──────────────────────────────────────────

    public static AttendanceRecord? GetTodayEntry(int employeeId)
    {
        lock (Sync)
        {
            var today = DateTime.Today;
            var entry = Attendance.FirstOrDefault(a => a.EmployeeId == employeeId && a.Date.Date == today);
            return entry == null ? null : CloneAttendance(entry);
        }
    }

    public static (bool Ok, string Message, AttendanceRecord? Entry) ClockIn(int employeeId, string? notes = null)
    {
        lock (Sync)
        {
            var employee = Employees.FirstOrDefault(e => e.Id == employeeId);
            if (employee == null)
                return (false, "Employee not found.", null);
            if (!employee.IsActive)
                return (false, $"{employee.FullName} is currently inactive.", null);

            var today = DateTime.Today;
            var existing = Attendance.FirstOrDefault(a => a.EmployeeId == employeeId && a.Date.Date == today);
            if (existing != null && existing.ClockIn.HasValue)
                return (false, $"{employee.FullName} already clocked in today at {FormatTime(existing.ClockIn.Value)}.", CloneAttendance(existing));

            var now = DateTime.Now.TimeOfDay;
            var nowRounded = new TimeSpan(now.Hours, now.Minutes, now.Seconds);

            var record = new AttendanceRecord
            {
                Id = Attendance.Count == 0 ? 1 : Attendance.Max(a => a.Id) + 1,
                EmployeeId = employeeId,
                Date = today,
                ClockIn = nowRounded,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
            };
            Attendance.Add(record);

            var msg = record.IsLate
                ? $"{employee.FullName} clocked in at {FormatTime(nowRounded)} (Late by {record.LateMinutes} min)."
                : $"{employee.FullName} clocked in at {FormatTime(nowRounded)}.";
            return (true, msg, CloneAttendance(record));
        }
    }

    public static (bool Ok, string Message, AttendanceRecord? Entry) ClockOut(int employeeId, string? notes = null)
    {
        lock (Sync)
        {
            var employee = Employees.FirstOrDefault(e => e.Id == employeeId);
            if (employee == null)
                return (false, "Employee not found.", null);

            var today = DateTime.Today;
            var existing = Attendance.FirstOrDefault(a => a.EmployeeId == employeeId && a.Date.Date == today);
            if (existing == null || !existing.ClockIn.HasValue)
                return (false, $"{employee.FullName} has not clocked in today.", null);
            if (existing.ClockOut.HasValue)
                return (false, $"{employee.FullName} already clocked out today at {FormatTime(existing.ClockOut.Value)}.", CloneAttendance(existing));

            var now = DateTime.Now.TimeOfDay;
            var nowRounded = new TimeSpan(now.Hours, now.Minutes, now.Seconds);
            if (nowRounded <= existing.ClockIn.Value)
                nowRounded = existing.ClockIn.Value.Add(TimeSpan.FromMinutes(1));

            existing.ClockOut = nowRounded;
            if (!string.IsNullOrWhiteSpace(notes))
            {
                existing.Notes = string.IsNullOrWhiteSpace(existing.Notes)
                    ? notes.Trim()
                    : $"{existing.Notes} | {notes.Trim()}";
            }

            var msg = existing.IsOvertime
                ? $"{employee.FullName} clocked out at {FormatTime(nowRounded)}. Worked {existing.HoursWorked:0.##} hrs ({existing.OvertimeHours:0.##} hrs OT)."
                : $"{employee.FullName} clocked out at {FormatTime(nowRounded)}. Worked {existing.HoursWorked:0.##} hrs.";
            return (true, msg, CloneAttendance(existing));
        }
    }

    public static List<AttendanceRecord> GetMonthlyEntries(int employeeId, int year, int month)
    {
        lock (Sync)
        {
            return Attendance
                .Where(a => a.EmployeeId == employeeId && a.Date.Year == year && a.Date.Month == month)
                .OrderBy(a => a.Date)
                .Select(CloneAttendance)
                .ToList();
        }
    }

    public static string FormatTime(TimeSpan? time) =>
        time.HasValue ? DateTime.Today.Add(time.Value).ToString("hh:mm tt") : "—";

    // ── Cloning & Seed Data ────────────────────────────────────────────────────

    private static EmployeeRecord CloneEmployee(EmployeeRecord e) => new()
    {
        Id = e.Id,
        FullName = e.FullName,
        Role = e.Role,
        HourlyRate = e.HourlyRate,
        Phone = e.Phone,
        IsActive = e.IsActive,
        Username = e.Username,
        JoinedDate = e.JoinedDate
    };

    private static AttendanceRecord CloneAttendance(AttendanceRecord a) => new()
    {
        Id = a.Id,
        EmployeeId = a.EmployeeId,
        Date = a.Date,
        ClockIn = a.ClockIn,
        ClockOut = a.ClockOut,
        Notes = a.Notes
    };

    private static List<EmployeeRecord> SeedEmployees() => new()
    {
        new() { Id = 1,  FullName = "Nuwan Perera",        Role = "Senior Mechanic", HourlyRate = 700m, Phone = "+94 77 210 3344", IsActive = true,  Username = "mechanic", JoinedDate = DateTime.Today.AddMonths(-24) },
        new() { Id = 2,  FullName = "Saman Kumara",        Role = "Mechanic",        HourlyRate = 660m, Phone = "+94 71 342 8890", IsActive = true,  JoinedDate = DateTime.Today.AddMonths(-18) },
        new() { Id = 3,  FullName = "Kasun Silva",         Role = "Mechanic",        HourlyRate = 640m, Phone = "+94 76 551 7721", IsActive = true,  JoinedDate = DateTime.Today.AddMonths(-15) },
        new() { Id = 4,  FullName = "Dilshan Fernando",    Role = "Mechanic",        HourlyRate = 620m, Phone = "+94 77 612 9084", IsActive = true,  JoinedDate = DateTime.Today.AddMonths(-12) },
        new() { Id = 5,  FullName = "Chaminda Rathnayake", Role = "Mechanic",        HourlyRate = 600m, Phone = "+94 75 409 1122", IsActive = true,  JoinedDate = DateTime.Today.AddMonths(-10) },
        new() { Id = 6,  FullName = "Nimali Perera",       Role = "Front Desk",      HourlyRate = 480m, Phone = "+94 77 889 4501", IsActive = true,  Username = "desk", JoinedDate = DateTime.Today.AddMonths(-20) },
        new() { Id = 7,  FullName = "Chamari Jayasinghe",  Role = "Accountant",      HourlyRate = 560m, Phone = "+94 71 992 3104", IsActive = true,  JoinedDate = DateTime.Today.AddMonths(-14) },
        new() { Id = 8,  FullName = "Ruwan Bandara",       Role = "Workshop Helper", HourlyRate = 400m, Phone = "+94 70 223 6741", IsActive = true,  JoinedDate = DateTime.Today.AddMonths(-8) },
        new() { Id = 9,  FullName = "Tharindu Gamage",     Role = "Workshop Helper", HourlyRate = 400m, Phone = "+94 78 334 5519", IsActive = true,  JoinedDate = DateTime.Today.AddMonths(-6) },
        new() { Id = 10, FullName = "Dilshan Wijesinghe",  Role = "Owner",           HourlyRate = 950m, Phone = "+94 77 100 0001", IsActive = true,  Username = "owner", JoinedDate = DateTime.Today.AddMonths(-36) },
        new() { Id = 11, FullName = "Mahesh Gunasekara",   Role = "Mechanic",        HourlyRate = 580m, Phone = "+94 72 778 9023", IsActive = false, JoinedDate = DateTime.Today.AddMonths(-22) }
    };

    private static List<AttendanceRecord> SeedAttendance()
    {
        var list = new List<AttendanceRecord>();
        var rng = new Random(808);
        var today = DateTime.Today;
        var currentMonthStart = new DateTime(today.Year, today.Month, 1);
        var startDate = currentMonthStart.AddMonths(-1);
        var id = 1;

        foreach (var emp in Employees.Where(e => e.IsActive))
        {
            // Seed yesterday back to start of previous month; leave today empty so users can test Clock In / Out live.
            for (var day = startDate; day < today; day = day.AddDays(1))
            {
                if (day.DayOfWeek == DayOfWeek.Sunday) continue;
                // Occasional leave day (~7%)
                if (rng.NextDouble() < 0.07) continue;

                // ~22% late arrivals (between 08:35 AM and 09:20 AM); otherwise on time (08:10 AM - 08:29 AM)
                var isLate = rng.NextDouble() < 0.22;
                var inMinutes = isLate
                    ? 8 * 60 + 35 + rng.Next(0, 46)
                    : 8 * 60 + 10 + rng.Next(0, 20);

                // Standard ~8h shift, with ~28% overtime days (9.0 to 10.75 hours)
                var isOt = rng.NextDouble() < 0.28;
                var durationMinutes = isOt
                    ? 540 + rng.Next(0, 105)     // 9.0 - 10.75 hrs
                    : 480 + rng.Next(-10, 10);   // ~8.0 hrs

                var clockIn = TimeSpan.FromMinutes(inMinutes);
                var clockOut = TimeSpan.FromMinutes(inMinutes + durationMinutes);

                list.Add(new AttendanceRecord
                {
                    Id = id++,
                    EmployeeId = emp.Id,
                    Date = day,
                    ClockIn = clockIn,
                    ClockOut = clockOut
                });
            }
        }

        return list;
    }
}
