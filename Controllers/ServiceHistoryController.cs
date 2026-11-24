using AutoTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoTrack.Controllers;

[Authorize]
public class ServiceHistoryController : Controller
{
    [HttpGet]
    public IActionResult Index(string? vehicleNo)
    {
        var jobs = MockJobData.All();
        if (User.IsInRole("Customer"))
        {
            var customerName = User.FindFirst("FullName")?.Value ?? User.Identity?.Name ?? "";
            jobs = jobs.Where(job =>
                string.Equals(job.CustomerName, customerName, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (string.IsNullOrWhiteSpace(vehicleNo))
        {
            return View(new ServiceHistoryViewModel
            {
                Vehicles = jobs
                    .GroupBy(job => job.VehicleNo, StringComparer.OrdinalIgnoreCase)
                    .Select(group => new CustomerVehicleViewModel
                    {
                        VehicleNo = group.Key,
                        VehicleModel = "Vehicle on file"
                    })
                    .OrderBy(vehicle => vehicle.VehicleNo)
                    .ToList()
            });
        }

        var vehicleJobs = jobs.Where(job =>
            string.Equals(job.VehicleNo, vehicleNo.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        if (vehicleJobs.Count == 0)
        {
            if (!User.IsInRole("Customer"))
                return NotFound();

            var customerName = User.FindFirst("FullName")?.Value ?? User.Identity?.Name ?? "";
            var email = User.Identity?.Name ?? "";
            var hasAppointment = MockAppointmentData.All().Any(appointment =>
                string.Equals(appointment.VehicleNo, vehicleNo.Trim(), StringComparison.OrdinalIgnoreCase) &&
                (string.Equals(appointment.CustomerName, customerName, StringComparison.OrdinalIgnoreCase) ||
                 (!string.IsNullOrEmpty(appointment.CustomerEmail) &&
                  string.Equals(appointment.CustomerEmail, email, StringComparison.OrdinalIgnoreCase))));
            if (!hasAppointment) return NotFound();

            return View(new ServiceHistoryViewModel { VehicleNo = vehicleNo.Trim() });
        }

        return View(new ServiceHistoryViewModel
        {
            VehicleNo = vehicleJobs[0].VehicleNo,
            Jobs = vehicleJobs
                .Where(job => string.Equals(job.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(job => job.DateOpened)
                .ToList()
        });
    }
}
