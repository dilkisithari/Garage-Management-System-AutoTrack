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
[Route("api/users")]
[Authorize(Roles = "Owner")]
public class UserManagementApiController(
    AutoTrackDbContext db,
    UserManager<AutoTrackUser> users,
    RoleManager<IdentityRole> roles) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var accounts = await users.Users.AsNoTracking().OrderBy(user => user.Email).ToListAsync();
        var result = new List<object>(accounts.Count);
        foreach (var account in accounts)
        {
            result.Add(new
            {
                account.Id,
                account.Email,
                account.FullName,
                Phone = account.PhoneNumber,
                account.IsActive,
                Roles = await users.GetRolesAsync(account)
            });
        }

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var account = await users.FindByIdAsync(id);
        if (account == null)
            return NotFound();

        return Ok(new
        {
            account.Id,
            account.Email,
            account.FullName,
            Phone = account.PhoneNumber,
            account.IsActive,
            Roles = await users.GetRolesAsync(account)
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateManagedIdentityUserRequest request)
    {
        if (!await roles.RoleExistsAsync(request.Role))
            return BadRequest(new { error = "Select a valid role." });

        var account = new AutoTrackUser
        {
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            FullName = request.FullName.Trim(),
            PhoneNumber = request.Phone?.Trim(),
            IsActive = true
        };

        await using var transaction = await db.Database.BeginTransactionAsync();
        var created = await users.CreateAsync(account, request.Password);
        if (!created.Succeeded)
            return BadRequest(new { errors = created.Errors.Select(error => error.Description) });

        var assigned = await users.AddToRoleAsync(account, request.Role);
        if (!assigned.Succeeded)
            return BadRequest(new { errors = assigned.Errors.Select(error => error.Description) });

        await transaction.CommitAsync();
        return CreatedAtAction(nameof(Get), new { id = account.Id }, new
        {
            account.Id,
            account.Email,
            account.FullName,
            Role = request.Role,
            account.IsActive
        });
    }

    [HttpPut("{id}/role")]
    public async Task<IActionResult> AssignRole(string id, [FromBody] AssignIdentityRoleRequest request)
    {
        if (!await roles.RoleExistsAsync(request.Role))
            return BadRequest(new { error = "Select a valid role." });

        var account = await users.FindByIdAsync(id);
        if (account == null)
            return NotFound();
        if (string.Equals(id, User.FindFirstValue(ClaimTypes.NameIdentifier), StringComparison.Ordinal))
            return Conflict(new { error = "You cannot change your own role." });

        var currentRoles = await users.GetRolesAsync(account);
        if (currentRoles.Count == 1 && string.Equals(currentRoles[0], request.Role, StringComparison.Ordinal))
            return NoContent();
        if (currentRoles.Contains("Owner") &&
            !string.Equals(request.Role, "Owner", StringComparison.Ordinal) &&
            await IsLastActiveOwnerAsync(account))
            return Conflict(new { error = "The last active owner cannot be removed." });

        await using var transaction = await db.Database.BeginTransactionAsync();
        var removed = await users.RemoveFromRolesAsync(account, currentRoles);
        if (!removed.Succeeded)
            return BadRequest(new { errors = removed.Errors.Select(error => error.Description) });

        var added = await users.AddToRoleAsync(account, request.Role);
        if (!added.Succeeded)
            return BadRequest(new { errors = added.Errors.Select(error => error.Description) });

        var stamp = await users.UpdateSecurityStampAsync(account);
        if (!stamp.Succeeded)
            return BadRequest(new { errors = stamp.Errors.Select(error => error.Description) });
        await transaction.CommitAsync();
        return NoContent();
    }

    [HttpPut("{id}/active")]
    public async Task<IActionResult> SetActive(string id, [FromBody] SetIdentityActiveRequest request)
    {
        var account = await users.FindByIdAsync(id);
        if (account == null)
            return NotFound();
        if (account.IsActive == request.IsActive)
            return NoContent();
        if (!request.IsActive &&
            string.Equals(id, User.FindFirstValue(ClaimTypes.NameIdentifier), StringComparison.Ordinal))
            return Conflict(new { error = "You cannot deactivate your own account." });

        if (!request.IsActive &&
            (await users.GetRolesAsync(account)).Contains("Owner") &&
            await IsLastActiveOwnerAsync(account))
            return Conflict(new { error = "The last active owner cannot be deactivated." });

        account.IsActive = request.IsActive;
        var result = await users.UpdateAsync(account);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(error => error.Description) });
        var stamp = await users.UpdateSecurityStampAsync(account);
        if (!stamp.Succeeded)
            return BadRequest(new { errors = stamp.Errors.Select(error => error.Description) });

        return NoContent();
    }

    private async Task<bool> IsLastActiveOwnerAsync(AutoTrackUser account)
    {
        if (!account.IsActive)
            return false;

        var owners = await users.GetUsersInRoleAsync("Owner");
        return owners.Count(owner => owner.IsActive) <= 1;
    }
}
