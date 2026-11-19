using System.Security.Claims;
using AutoTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoTrack.Controllers;

public class AppointmentsController : Controller
{
    // GET: /Appointments/Book
    [AllowAnonymous, HttpGet]
    public IActionResult Book(string? date = null, string? slot = null)
    {
        var model = new AppointmentBookingViewModel();

        // Default to tomorrow if not specified or if invalid
        if (DateTime.TryParse(date, out var parsedDate) && parsedDate.Date >= DateTime.Today)
        {
            model.AppointmentDate = parsedDate.Date;
        }
        else
        {
            model.AppointmentDate = DateTime.Today.AddDays(1);
        }

        if (!string.IsNullOrWhiteSpace(slot) && MockAppointmentData.TimeSlots.Contains(slot))
        {
            model.TimeSlot = slot;
        }

        PopulateFormHelpers(model);

        // Pre-fill user information if customer is logged in
        if (User.Identity?.IsAuthenticated == true)
        {
            var fullName = User.FindFirst("FullName")?.Value ?? User.Identity.Name ?? "";
            model.CustomerName = fullName;
            model.CustomerEmail = User.Identity.Name?.Contains("@") == true ? User.Identity.Name : "";

            // Suggest customer's known vehicles from existing mock job/invoice history
            var userVehicles = MockJobData.All()
                .Where(j => string.Equals(j.CustomerName, fullName, StringComparison.OrdinalIgnoreCase))
                .Select(j => j.VehicleNo)
                .Distinct()
                .ToList();

            if (userVehicles.Count == 0 && string.Equals(fullName, "Kasun Silva", StringComparison.OrdinalIgnoreCase))
            {
                userVehicles.Add("CAB-1234");
            }

            model.ExistingVehicles = userVehicles;
            if (userVehicles.Count > 0)
            {
                model.VehicleNo = userVehicles.First();
                model.VehicleModel = "Mitsubishi Montero Sport 2018";
            }
        }

        return View(model);
    }

    // POST: /Appointments/Book
    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public IActionResult Book(AppointmentBookingViewModel model)
    {
        if (model.AppointmentDate.Date < DateTime.Today)
        {
            ModelState.AddModelError(nameof(model.AppointmentDate), "Appointment date cannot be in the past.");
        }

        if (!string.IsNullOrWhiteSpace(model.TimeSlot) &&
            MockAppointmentData.IsSlotBooked(model.AppointmentDate, model.TimeSlot))
        {
            ModelState.AddModelError(nameof(model.TimeSlot), "This time slot is already booked. Please choose another slot.");
        }

        if (!ModelState.IsValid)
        {
            PopulateFormHelpers(model);
            return View(model);
        }

        var appointment = new AppointmentViewModel
        {
            CustomerName = model.CustomerName.Trim(),
            CustomerPhone = model.CustomerPhone.Trim(),
            CustomerEmail = model.CustomerEmail?.Trim(),
            VehicleNo = model.VehicleNo.Trim().ToUpperInvariant(),
            VehicleModel = model.VehicleModel?.Trim(),
            ServiceType = model.ServiceType,
            AppointmentDate = model.AppointmentDate.Date,
            TimeSlot = model.TimeSlot.Trim(),
            Notes = model.Notes?.Trim(),
            Status = "Pending",
            CreatedAt = DateTime.Now
        };

        MockAppointmentData.Save(appointment);

        TempData["Success"] = $"Booking request received! Your reference is {appointment.AppointmentNo}.";
        return RedirectToAction(nameof(Confirmation), new { id = appointment.Id });
    }

    // GET: /Appointments/Confirmation/5
    [AllowAnonymous, HttpGet]
    public IActionResult Confirmation(int id)
    {
        var appt = MockAppointmentData.Get(id);
        if (appt == null)
        {
            TempData["Error"] = "Appointment not found.";
            return RedirectToAction(nameof(Book));
        }

        return View(appt);
    }

    // GET: /Appointments/GetBookedSlots?date=2026-11-20
    [AllowAnonymous, HttpGet]
    public IActionResult GetBookedSlots(string? date)
    {
        if (!DateTime.TryParse(date, out var parsedDate))
        {
            return Json(new { success = false, message = "Invalid date format." });
        }

        var booked = MockAppointmentData.GetBookedSlots(parsedDate.Date);
        return Json(new { success = true, date = parsedDate.ToString("yyyy-MM-dd"), bookedSlots = booked });
    }

    // GET: /Appointments/My
    [Authorize, HttpGet]
    public IActionResult My()
    {
        var fullName = User.FindFirst("FullName")?.Value ?? User.Identity?.Name ?? "";
        var email = User.Identity?.Name ?? "";

        var appointments = MockAppointmentData.All()
            .Where(a =>
                string.Equals(a.CustomerName, fullName, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(a.CustomerEmail) && string.Equals(a.CustomerEmail, email, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        ViewBag.CustomerName = fullName;
        return View(appointments);
    }

    private static void PopulateFormHelpers(AppointmentBookingViewModel model)
    {
        model.AvailableServiceTypes = MockAppointmentData.ServiceTypes.ToList();
        model.AllTimeSlots = MockAppointmentData.TimeSlots.ToList();
        model.BookedSlotsForDate = MockAppointmentData.GetBookedSlots(model.AppointmentDate);
    }
}

