using AutoTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoTrack.Controllers;

[Authorize(Roles = "Customer")]
public class CustomerPortalController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var username = User.Identity?.Name ?? "";
        var user = MockUserStore.Get(username);
        if (user == null || !user.IsActive) return Forbid();

        var appointments = MockAppointmentData.All()
            .Where(a =>
                string.Equals(a.CustomerName, user.FullName, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(a.CustomerEmail) &&
                 string.Equals(a.CustomerEmail, user.Username, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var jobs = MockJobData.All()
            .Where(j => string.Equals(j.CustomerName, user.FullName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var vehicles = jobs.Select(j => new CustomerVehicleViewModel
            {
                VehicleNo = j.VehicleNo,
                VehicleModel = "Vehicle on file"
            })
            .Concat(appointments.Select(a => new CustomerVehicleViewModel
            {
                VehicleNo = a.VehicleNo,
                VehicleModel = a.VehicleModel ?? "Vehicle on file"
            }))
            .GroupBy(v => v.VehicleNo, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(v => v.VehicleNo)
            .ToList();

        return View(new CustomerPortalViewModel
        {
            Profile = new CustomerProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Username.Contains('@') ? user.Username : "",
                Phone = user.Phone
            },
            Vehicles = vehicles,
            Appointments = appointments
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult UpdateProfile(CustomerProfileViewModel model)
    {
        var username = User.Identity?.Name ?? "";
        var user = MockUserStore.Get(username);
        if (user == null || !user.IsActive) return Forbid();

        ModelState.Remove(nameof(model.FullName));
        ModelState.Remove(nameof(model.Email));
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Enter a valid phone number.";
            return RedirectToAction(nameof(Index), new { tab = "profile" });
        }

        if (!MockUserStore.UpdateProfile(username, user.FullName, model.Phone))
            return NotFound();

        MockAuditLog.Add(username, "Profile updated", "Updated contact phone number.");
        TempData["Success"] = "Profile updated.";
        return RedirectToAction(nameof(Index), new { tab = "profile" });
    }
}
