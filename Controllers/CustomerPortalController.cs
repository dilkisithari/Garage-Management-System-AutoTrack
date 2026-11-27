using System.Security.Claims;
using AutoTrack.Data;
using AutoTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoTrack.Controllers;

[Authorize(Roles = "Customer")]
public class CustomerPortalController(
    AutoTrackDbContext db,
    UserManager<AutoTrackUser> users) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var customer = await users.GetUserAsync(User);
        if (customer == null || !customer.IsActive)
            return Forbid();

        var vehicles = await db.CustomerVehicles.AsNoTracking()
            .Where(vehicle => vehicle.CustomerId == customer.Id)
            .OrderBy(vehicle => vehicle.VehicleNo)
            .Select(vehicle => new CustomerVehicleViewModel
            {
                VehicleNo = vehicle.VehicleNo,
                VehicleModel = vehicle.Model ?? "Vehicle on file"
            })
            .ToListAsync();

        var appointments = await db.CustomerAppointments.AsNoTracking()
            .Where(appointment => appointment.CustomerId == customer.Id)
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

        return View(new CustomerPortalViewModel
        {
            Profile = new CustomerProfileViewModel
            {
                FullName = customer.FullName,
                Email = customer.Email ?? "",
                Phone = customer.PhoneNumber
            },
            Vehicles = vehicles,
            Appointments = appointments
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(CustomerProfileViewModel model)
    {
        ModelState.Remove(nameof(model.FullName));
        ModelState.Remove(nameof(model.Email));
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Enter a valid phone number.";
            return RedirectToAction(nameof(Index), new { tab = "profile" });
        }

        var customer = await users.GetUserAsync(User);
        if (customer == null || !customer.IsActive)
            return Forbid();

        customer.PhoneNumber = model.Phone?.Trim();
        var result = await users.UpdateAsync(customer);
        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join("; ", result.Errors.Select(error => error.Description));
            return RedirectToAction(nameof(Index), new { tab = "profile" });
        }

        TempData["Success"] = "Profile updated.";
        return RedirectToAction(nameof(Index), new { tab = "profile" });
    }
}
