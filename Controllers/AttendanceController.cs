using System.Globalization;
using AutoTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoTrack.Controllers;

[Authorize(Roles = "Owner,FrontDesk,Mechanic")]
public class AttendanceController : Controller
{
    // GET: /Attendance/Clock
    [HttpGet]
    public IActionResult Clock(int? employeeId)
    {
        var canSwitch = CanSwitchEmployee;
        var activeEmployees = MockEmployeeData.Active();
        var selected = ResolveEmployee(employeeId, activeEmployees, canSwitch);

        var model = new ClockInOutViewModel
        {
            SelectedEmployee = selected,
            TodayEntry = selected == null ? null : MockEmployeeData.GetTodayEntry(selected.Id),
            EmployeeOptions = canSwitch
                ? activeEmployees
                : (selected == null ? new List<EmployeeRecord>() : new List<EmployeeRecord> { selected }),
            CanSwitchEmployee = canSwitch,
            ServerNow = DateTime.Now
        };

        return View(model);
    }

    // POST: /Attendance/ClockIn
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ClockIn(int employeeId, string? notes)
    {
        if (!CanActForEmployee(employeeId))
            return Forbid();

        var (ok, message, _) = MockEmployeeData.ClockIn(employeeId, notes);
        if (ok)
        {
            MockAuditLog.Add(CurrentUsername, "Clock in", message);
            TempData["Success"] = message;
        }
        else
        {
            TempData["Error"] = message;
        }

        return RedirectToAction(nameof(Clock), new { employeeId });
    }

    // POST: /Attendance/ClockOut
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ClockOut(int employeeId, string? notes)
    {
        if (!CanActForEmployee(employeeId))
            return Forbid();

        var (ok, message, _) = MockEmployeeData.ClockOut(employeeId, notes);
        if (ok)
        {
            MockAuditLog.Add(CurrentUsername, "Clock out", message);
            TempData["Success"] = message;
        }
        else
        {
            TempData["Error"] = message;
        }

        return RedirectToAction(nameof(Clock), new { employeeId });
    }

    // GET: /Attendance/Sheet
    [HttpGet]
    public IActionResult Sheet(int? employeeId, string? month)
    {
        var canSwitch = CanSwitchEmployee;
        var allEmployees = MockEmployeeData.All();
        var selected = ResolveEmployee(employeeId, allEmployees, canSwitch);

        var monthOptions = BuildMonthOptions();
        var targetMonthKey = !string.IsNullOrWhiteSpace(month) && monthOptions.Any(o => o.Value == month)
            ? month
            : DateTime.Today.ToString("yyyy-MM", CultureInfo.InvariantCulture);

        var monthStart = DateTime.ParseExact(targetMonthKey, "yyyy-MM", CultureInfo.InvariantCulture);
        var daysInMonth = DateTime.DaysInMonth(monthStart.Year, monthStart.Month);

        var entriesByDay = selected == null
            ? new Dictionary<int, AttendanceRecord>()
            : MockEmployeeData.GetMonthlyEntries(selected.Id, monthStart.Year, monthStart.Month)
                .ToDictionary(e => e.Date.Day);

        var days = new List<AttendanceSheetDayRow>(daysInMonth);
        for (var d = 1; d <= daysInMonth; d++)
        {
            var date = new DateTime(monthStart.Year, monthStart.Month, d);
            entriesByDay.TryGetValue(d, out var entry);
            days.Add(new AttendanceSheetDayRow
            {
                Date = date,
                Entry = entry
            });
        }

        var model = new AttendanceSheetViewModel
        {
            Employee = selected,
            SelectedEmployeeId = selected?.Id ?? 0,
            SelectedMonth = targetMonthKey,
            MonthStart = monthStart,
            CanSwitchEmployee = canSwitch,
            EmployeeOptions = canSwitch
                ? allEmployees
                : (selected == null ? new List<EmployeeRecord>() : new List<EmployeeRecord> { selected }),
            MonthOptions = monthOptions,
            Days = days
        };

        return View(model);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private string CurrentUsername => User.Identity?.Name ?? "";

    private bool CanSwitchEmployee => User.IsInRole("Owner") || User.IsInRole("FrontDesk");

    private EmployeeRecord? ResolveEmployee(int? requestedId, List<EmployeeRecord> pool, bool canSwitch)
    {
        var own = MockEmployeeData.GetByUsername(CurrentUsername);

        if (!canSwitch)
            return own;

        if (requestedId.HasValue)
        {
            var requested = pool.FirstOrDefault(e => e.Id == requestedId.Value) ?? MockEmployeeData.Get(requestedId.Value);
            if (requested != null) return requested;
        }

        return own ?? pool.FirstOrDefault();
    }

    private bool CanActForEmployee(int employeeId)
    {
        if (CanSwitchEmployee) return true;
        var own = MockEmployeeData.GetByUsername(CurrentUsername);
        return own != null && own.Id == employeeId;
    }

    private static List<(string Value, string Text)> BuildMonthOptions()
    {
        var current = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        return Enumerable.Range(0, 12)
            .Select(i => current.AddMonths(-i))
            .Select(m => (
                m.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                m.ToString("MMMM yyyy", CultureInfo.InvariantCulture)))
            .ToList();
    }
}
