using System.Security.Claims;
using System.Text.Json;
using AutoTrack.Data;
using AutoTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoTrack.Controllers.Api;

[ApiController]
[Route("api/service-history")]
[Authorize]
public class ServiceHistoryApiController(AutoTrackDbContext db) : ControllerBase
{
    [HttpGet("{vehicleNo}")]
    public async Task<ActionResult<IReadOnlyList<ServiceHistoryItem>>> Get(string vehicleNo)
    {
        var normalizedVehicleNo = vehicleNo.Trim().ToUpperInvariant();
        var vehicle = await db.CustomerVehicles.AsNoTracking()
            .SingleOrDefaultAsync(item => item.VehicleNo == normalizedVehicleNo);
        if (vehicle == null)
            return NotFound();

        if (User.IsInRole("Customer") &&
            !string.Equals(vehicle.CustomerId, User.FindFirstValue(ClaimTypes.NameIdentifier), StringComparison.Ordinal))
            return NotFound();

        var jobs = await db.ServiceJobs.AsNoTracking()
            .Where(job => job.VehicleNo == normalizedVehicleNo && job.Status == "Completed")
            .OrderByDescending(job => job.DateOpened)
            .ToListAsync();

        return Ok(jobs.Select(job => new ServiceHistoryItem
        {
            JobNo = job.JobNo,
            Date = job.DateOpened,
            Complaint = job.Complaint,
            WorkDone = string.Join("; ",
                job.WorkDone.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Concat((JsonSerializer.Deserialize<List<ServiceHistoryLabour>>(job.LabourJson) ?? new())
                        .Select(labour => labour.Description))),
            Parts = JsonSerializer.Deserialize<List<ServiceHistoryPart>>(job.PartsJson) ?? new(),
            TotalCost = job.TotalCost
        }).ToList());
    }
}
