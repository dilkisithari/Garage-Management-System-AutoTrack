using System.Security.Claims;
using AutoTrack.Data;
using AutoTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoTrack.Controllers;

public class AppointmentsController(
    AutoTrackDbContext db,
    UserManager<AutoTrackUser> users) : Controller
{
    // GET: /Appointments/Book
    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> Book(string? date = null, string? slot = null)
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

        await PopulateFormHelpersAsync(model);

        // Pre-fill user information if customer is logged in
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await users.GetUserAsync(User);
            var fullName = user?.FullName ?? User.Identity.Name ?? "";
            model.CustomerName = fullName;
            model.CustomerEmail = user?.Email ?? "";

            var userVehicles = user == null
                ? new List<string>()
                : await db.CustomerVehicles.AsNoTracking()
                    .Where(vehicle => vehicle.CustomerId == user.Id)
                    .Select(vehicle => vehicle.VehicleNo)
                    .Distinct()
                    .ToListAsync();

            model.ExistingVehicles = userVehicles;
            if (userVehicles.Count > 0)
            {
                model.VehicleNo = userVehicles.First();
                model.VehicleModel = await db.CustomerVehicles.AsNoTracking()
                    .Where(vehicle => vehicle.CustomerId == user!.Id && vehicle.VehicleNo == userVehicles.First())
                    .Select(vehicle => vehicle.Model)
                    .FirstOrDefaultAsync();
            }
        }

        return View(model);
    }

    // POST: /Appointments/Book
    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(AppointmentBookingViewModel model)
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
        else if (!string.IsNullOrWhiteSpace(model.TimeSlot) &&
                 await db.CustomerAppointments.AnyAsync(appointment =>
                     appointment.AppointmentDate == model.AppointmentDate.Date &&
                     appointment.TimeSlot == model.TimeSlot.Trim() &&
                     appointment.Status != "Cancelled"))
        {
            ModelState.AddModelError(nameof(model.TimeSlot), "This time slot is already booked. Please choose another slot.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateFormHelpersAsync(model);
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
        appointment.AppointmentNo = $"APT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..25];

        var customer = User.Identity?.IsAuthenticated == true && User.IsInRole("Customer")
            ? await users.GetUserAsync(User)
            : null;
        var normalizedVehicleNo = appointment.VehicleNo.ToUpperInvariant();
        var vehicle = customer == null
            ? null
            : await db.CustomerVehicles.SingleOrDefaultAsync(
                item => item.VehicleNo == normalizedVehicleNo);
        if (vehicle != null && vehicle.CustomerId != customer!.Id)
        {
            ModelState.AddModelError(nameof(model.VehicleNo), "That vehicle is registered to another customer.");
            await PopulateFormHelpersAsync(model);
            return View(model);
        }

        if (customer != null)
        {
            if (vehicle == null)
            {
                vehicle = new CustomerVehicle
                {
                    VehicleNo = normalizedVehicleNo,
                    Model = appointment.VehicleModel,
                    CustomerId = customer.Id
                };
                db.CustomerVehicles.Add(vehicle);
            }

            db.CustomerAppointments.Add(new CustomerAppointment
            {
                AppointmentNo = appointment.AppointmentNo,
                CustomerId = customer.Id,
                Vehicle = vehicle,
                ServiceType = appointment.ServiceType,
                AppointmentDate = appointment.AppointmentDate,
                TimeSlot = appointment.TimeSlot,
                Status = appointment.Status,
                CreatedAt = appointment.CreatedAt
            });
            await db.SaveChangesAsync();
        }

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
    public async Task<IActionResult> GetBookedSlots(string? date)
    {
        if (!DateTime.TryParse(date, out var parsedDate))
        {
            return Json(new { success = false, message = "Invalid date format." });
        }

        var booked = MockAppointmentData.GetBookedSlots(parsedDate.Date);
        booked.AddRange(await db.CustomerAppointments.AsNoTracking()
            .Where(appointment =>
                appointment.AppointmentDate == parsedDate.Date &&
                appointment.Status != "Cancelled")
            .Select(appointment => appointment.TimeSlot)
            .ToListAsync());
        booked = booked.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return Json(new { success = true, date = parsedDate.ToString("yyyy-MM-dd"), bookedSlots = booked });
    }

    // GET: /Appointments/My
    [Authorize, HttpGet]
    public async Task<IActionResult> My()
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var customer = await users.GetUserAsync(User);
        if (customer == null || customerId == null)
            return Forbid();

        var appointments = await db.CustomerAppointments.AsNoTracking()
            .Where(appointment => appointment.CustomerId == customerId)
            .OrderByDescending(appointment => appointment.AppointmentDate)
            .Select(appointment => new AppointmentViewModel
            {
                Id = appointment.Id,
                AppointmentNo = appointment.AppointmentNo,
                CustomerName = customer.FullName,
                CustomerPhone = customer.PhoneNumber ?? "",
                CustomerEmail = customer.Email,
                VehicleNo = appointment.Vehicle.VehicleNo,
                VehicleModel = appointment.Vehicle.Model,
                ServiceType = appointment.ServiceType,
                AppointmentDate = appointment.AppointmentDate,
                TimeSlot = appointment.TimeSlot,
                Status = appointment.Status,
                CreatedAt = appointment.CreatedAt
            })
            .ToListAsync();

        ViewBag.CustomerName = customer.FullName;
        return View(appointments);
    }

    private async Task PopulateFormHelpersAsync(AppointmentBookingViewModel model)
    {
        model.AvailableServiceTypes = MockAppointmentData.ServiceTypes.ToList();
        model.AllTimeSlots = MockAppointmentData.TimeSlots.ToList();
        model.BookedSlotsForDate = MockAppointmentData.GetBookedSlots(model.AppointmentDate);
        model.BookedSlotsForDate.AddRange(await db.CustomerAppointments.AsNoTracking()
            .Where(appointment =>
                appointment.AppointmentDate == model.AppointmentDate.Date &&
                appointment.Status != "Cancelled")
            .Select(appointment => appointment.TimeSlot)
            .ToListAsync());
        model.BookedSlotsForDate = model.BookedSlotsForDate
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

