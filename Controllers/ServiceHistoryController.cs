using System.Security.Claims;
using System.Text.Json;
using AutoTrack.Data;
using AutoTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoTrack.Controllers;

[Authorize]
public class ServiceHistoryController(AutoTrackDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? vehicleNo)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var vehiclesQuery = db.CustomerVehicles.AsNoTracking().AsQueryable();
        if (User.IsInRole("Customer"))
            vehiclesQuery = vehiclesQuery.Where(vehicle => vehicle.CustomerId == customerId);

        if (string.IsNullOrWhiteSpace(vehicleNo))
        {
            var vehicles = await vehiclesQuery
                .OrderBy(vehicle => vehicle.VehicleNo)
                .Select(vehicle => new CustomerVehicleViewModel
                {
                    VehicleNo = vehicle.VehicleNo,
                    VehicleModel = vehicle.Model ?? "Vehicle on file"
                })
                .ToListAsync();

            return View(new ServiceHistoryViewModel { Vehicles = vehicles });
        }

        var normalizedVehicleNo = vehicleNo.Trim().ToUpperInvariant();
        var vehicle = await vehiclesQuery.SingleOrDefaultAsync(item => item.VehicleNo == normalizedVehicleNo);
        if (vehicle == null)
            return NotFound();

        var records = await db.ServiceJobs.AsNoTracking()
            .Where(job => job.VehicleNo == normalizedVehicleNo && job.Status == "Completed")
            .OrderByDescending(job => job.DateOpened)
            .ToListAsync();

        var jobs = records.Select(record =>
        {
            var parts = JsonSerializer.Deserialize<List<ServiceHistoryPart>>(record.PartsJson) ?? new();
            var partsTotal = parts.Sum(part => part.Quantity);
            return new JobCardViewModel
            {
                JobNo = record.JobNo,
                VehicleNo = record.VehicleNo,
                DateOpened = record.DateOpened,
                Complaint = record.Complaint,
                Status = record.Status,
                Tasks = string.IsNullOrWhiteSpace(record.WorkDone)
                    ? new()
                    : record.WorkDone.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                        .Select(description => new JobTaskItem { Description = description, Done = true })
                        .ToList(),
                Parts = parts.Select(part => new JobPartItem
                {
                    PartName = part.PartName,
                    Quantity = part.Quantity,
                    UnitPrice = part.UnitPrice
                }).ToList(),
                Labour = (JsonSerializer.Deserialize<List<ServiceHistoryLabour>>(record.LabourJson) ?? new())
                    .Select(labour => new JobLabourItem
                    {
                        Description = labour.Description,
                        Hours = labour.Hours,
                        Rate = labour.Rate
                    })
                    .ToList()
            };
        }).ToList();

        return View(new ServiceHistoryViewModel
        {
            VehicleNo = vehicle.VehicleNo,
            Jobs = jobs
        });
    }
}
