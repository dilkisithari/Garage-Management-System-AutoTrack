using AutoTrack.Models;
using AutoTrack.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoTrack.Areas.FrontDesk.Controllers;

[Area("FrontDesk")]
[Authorize(Roles = "FrontDesk,Owner")]
public class AppointmentsController(AutoTrackDbContext db) : Controller
{
    // GET: /FrontDesk/Appointments
    public IActionResult Index(string? status, string? search, string? date)
    {
        var appointments = MockAppointmentData.All();

        if (!string.IsNullOrWhiteSpace(status))
        {
            appointments = appointments
                .Where(a => string.Equals(a.Status, status.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out var parsedDate))
        {
            appointments = appointments
                .Where(a => a.AppointmentDate.Date == parsedDate.Date)
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            appointments = appointments
                .Where(a =>
                    a.AppointmentNo.ToLowerInvariant().Contains(term) ||
                    a.CustomerName.ToLowerInvariant().Contains(term) ||
                    a.VehicleNo.ToLowerInvariant().Contains(term) ||
                    a.CustomerPhone.Contains(term) ||
                    a.ServiceType.ToLowerInvariant().Contains(term))
                .ToList();
        }

        var all = MockAppointmentData.All();
        ViewBag.Status = status ?? "";
        ViewBag.Search = search ?? "";
        ViewBag.Date = date ?? "";
        ViewBag.TotalCount = all.Count;
        ViewBag.PendingCount = all.Count(a => a.Status == "Pending");
        ViewBag.ConfirmedCount = all.Count(a => a.Status == "Confirmed");
        ViewBag.CancelledCount = all.Count(a => a.Status == "Cancelled");

        // Pass time slots to populate the reschedule modal
        ViewBag.AllTimeSlots = MockAppointmentData.TimeSlots;

        return View(appointments);
    }

    // POST: /FrontDesk/Appointments/Confirm/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id)
    {
        var appt = MockAppointmentData.Get(id);
        if (appt == null)
        {
            TempData["Error"] = "Appointment not found.";
            return RedirectToAction(nameof(Index));
        }

        if (MockAppointmentData.Confirm(id))
        {
            var storedAppointment = await db.CustomerAppointments
                .SingleOrDefaultAsync(appointment => appointment.AppointmentNo == appt.AppointmentNo);
            if (storedAppointment != null)
            {
                storedAppointment.Status = "Confirmed";
                await db.SaveChangesAsync();
            }
            TempData["Success"] = $"Appointment {appt.AppointmentNo} ({appt.VehicleNo}) confirmed successfully.";
        }
        else
        {
            TempData["Error"] = "Could not confirm appointment.";
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: /FrontDesk/Appointments/Reschedule
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reschedule(int id, DateTime newDate, string newSlot)
    {
        var appt = MockAppointmentData.Get(id);
        if (appt == null)
        {
            TempData["Error"] = "Appointment not found.";
            return RedirectToAction(nameof(Index));
        }

        if (newDate.Date < DateTime.Today)
        {
            TempData["Error"] = "Rescheduled date cannot be in the past.";
            return RedirectToAction(nameof(Index));
        }

        if (MockAppointmentData.IsSlotBooked(newDate, newSlot, excludeId: id))
        {
            TempData["Error"] = $"Slot '{newSlot}' is already booked on {newDate:yyyy-MM-dd}. Please choose a different slot.";
            return RedirectToAction(nameof(Index));
        }

        if (MockAppointmentData.Reschedule(id, newDate, newSlot))
        {
            var storedAppointment = await db.CustomerAppointments
                .SingleOrDefaultAsync(appointment => appointment.AppointmentNo == appt.AppointmentNo);
            if (storedAppointment != null)
            {
                storedAppointment.AppointmentDate = newDate.Date;
                storedAppointment.TimeSlot = newSlot.Trim();
                storedAppointment.Status = "Confirmed";
                await db.SaveChangesAsync();
            }
            TempData["Success"] = $"Appointment {appt.AppointmentNo} rescheduled to {newDate:yyyy-MM-dd} at {newSlot}.";
        }
        else
        {
            TempData["Error"] = "Failed to reschedule appointment.";
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: /FrontDesk/Appointments/Cancel
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? reason)
    {
        var appt = MockAppointmentData.Get(id);
        if (appt == null)
        {
            TempData["Error"] = "Appointment not found.";
            return RedirectToAction(nameof(Index));
        }

        if (MockAppointmentData.Cancel(id, reason))
        {
            var storedAppointment = await db.CustomerAppointments
                .SingleOrDefaultAsync(appointment => appointment.AppointmentNo == appt.AppointmentNo);
            if (storedAppointment != null)
            {
                storedAppointment.Status = "Cancelled";
                await db.SaveChangesAsync();
            }
            TempData["Success"] = $"Appointment {appt.AppointmentNo} has been cancelled.";
        }
        else
        {
            TempData["Error"] = "Failed to cancel appointment.";
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: /FrontDesk/Appointments/Calendar
    public IActionResult Calendar(string? view, string? date)
    {
        var mode = string.Equals(view, "day", StringComparison.OrdinalIgnoreCase) ? "day" : "week";

        var selected = DateTime.Today;
        if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out var parsedDate))
        {
            selected = parsedDate.Date;
        }

        // Calculate week starting on Monday
        int diff = (7 + (selected.DayOfWeek - DayOfWeek.Monday)) % 7;
        var weekStart = selected.AddDays(-1 * diff).Date;
        var weekEnd = weekStart.AddDays(6).Date;

        var weekDays = new List<DateTime>();
        for (int i = 0; i < 7; i++)
        {
            weekDays.Add(weekStart.AddDays(i));
        }

        var allAppts = MockAppointmentData.All();

        // Build quick lookup dictionary: "yyyy-MM-dd|TimeSlot" -> Appointment
        var lookup = new Dictionary<string, AppointmentViewModel>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in allAppts.Where(a => a.Status != "Cancelled"))
        {
            var key = $"{a.AppointmentDate:yyyy-MM-dd}|{a.TimeSlot.Trim()}";
            if (!lookup.ContainsKey(key))
            {
                lookup[key] = a;
            }
        }

        var calendarVm = new AppointmentCalendarViewModel
        {
            ViewMode = mode,
            SelectedDate = selected,
            WeekStartDate = weekStart,
            WeekEndDate = weekEnd,
            WeekDays = weekDays,
            TimeSlots = MockAppointmentData.TimeSlots.ToList(),
            Appointments = allAppts,
            BookedSlotsLookup = lookup
        };

        return View(calendarVm);
    }
}

