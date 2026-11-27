using System.Security.Claims;
using AutoTrack.Data;
using AutoTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoTrack.Controllers.Api;

[ApiController]
[AutoValidateAntiforgeryToken]
[Route("api/customer-portal")]
[Authorize(Roles = "Customer")]
public class CustomerPortalApiController(
    AutoTrackDbContext db,
    UserManager<AutoTrackUser> users) : ControllerBase
{
    [HttpGet("profile")]
    public async Task<IActionResult> Profile()
    {
        var user = await CurrentCustomerAsync();
        if (user == null)
            return Unauthorized();

        return Ok(new
        {
            user.FullName,
            user.Email,
            Phone = user.PhoneNumber
        });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] CustomerProfileUpdateRequest request)
    {
        var user = await CurrentCustomerAsync();
        if (user == null)
            return Unauthorized();

        user.PhoneNumber = request.Phone?.Trim();
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(error => error.Description) });

        return NoContent();
    }

    [HttpGet("vehicles")]
    public async Task<IActionResult> Vehicles()
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var vehicles = await db.CustomerVehicles.AsNoTracking()
            .Where(vehicle => vehicle.CustomerId == customerId)
            .OrderBy(vehicle => vehicle.VehicleNo)
            .Select(vehicle => new
            {
                vehicle.VehicleNo,
                VehicleModel = vehicle.Model
            })
            .ToListAsync();

        return Ok(vehicles);
    }

    [HttpGet("appointments")]
    public async Task<IActionResult> Appointments()
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var appointments = await db.CustomerAppointments.AsNoTracking()
            .Where(appointment => appointment.CustomerId == customerId)
            .OrderByDescending(appointment => appointment.AppointmentDate)
            .Select(appointment => new
            {
                appointment.AppointmentNo,
                appointment.AppointmentDate,
                appointment.TimeSlot,
                appointment.Vehicle.VehicleNo,
                VehicleModel = appointment.Vehicle.Model,
                appointment.ServiceType,
                appointment.Status
            })
            .ToListAsync();

        return Ok(appointments);
    }

    private Task<AutoTrackUser?> CurrentCustomerAsync()
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return customerId == null ? Task.FromResult<AutoTrackUser?>(null) : users.FindByIdAsync(customerId);
    }
}

public class CustomerProfileUpdateRequest
{
    [System.ComponentModel.DataAnnotations.RegularExpression(
        @"^\+?[0-9\s\-]{9,15}$",
        ErrorMessage = "Enter a valid phone number.")]
    public string? Phone { get; set; }
}
