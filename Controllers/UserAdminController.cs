using AutoTrack.Data;
using AutoTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoTrack.Controllers;

[Authorize(Roles = "Owner")]
public class UserAdminController(
    AutoTrackDbContext db,
    UserManager<AutoTrackUser> users,
    RoleManager<IdentityRole> roles) : Controller
{
    private static readonly string[] AvailableRoles = { "Owner", "FrontDesk", "Mechanic", "Customer" };

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        SetRoles();
        return View(await BuildIndexModelAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "NewUser")] CreateManagedUserViewModel model)
    {
        if (!AvailableRoles.Contains(model.Role, StringComparer.Ordinal))
            ModelState.AddModelError("NewUser.Role", "Select a valid role.");
        if (!ModelState.IsValid)
            return await ShowIndexAsync(model);

        var username = model.Username.Trim();
        var user = new AutoTrackUser
        {
            UserName = username,
            Email = username.Contains('@') ? username : null,
            EmailConfirmed = username.Contains('@'),
            FullName = model.FullName.Trim(),
            PhoneNumber = model.Phone?.Trim(),
            IsActive = true
        };

        await using var transaction = await db.Database.BeginTransactionAsync();
        var created = await users.CreateAsync(user, model.Password);
        if (!created.Succeeded)
        {
            AddIdentityErrors(created, "NewUser.Username");
            return await ShowIndexAsync(model);
        }

        var assigned = await users.AddToRoleAsync(user, model.Role);
        if (!assigned.Succeeded)
        {
            AddIdentityErrors(assigned, "NewUser.Role");
            return await ShowIndexAsync(model);
        }

        await transaction.CommitAsync();
        TempData["Success"] = $"User '{username}' created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRole(string username, string role)
    {
        if (string.IsNullOrWhiteSpace(username))
            return AdminError("Select a user.");
        if (!AvailableRoles.Contains(role, StringComparer.Ordinal) || !await roles.RoleExistsAsync(role))
            return AdminError("Select a valid role.");

        var user = await FindUserAsync(username);
        if (user == null)
            return NotFound();
        if (string.Equals(user.Id, CurrentUserId, StringComparison.Ordinal))
            return AdminError("You cannot change your own role.");

        var previousRoles = await users.GetRolesAsync(user);
        if (previousRoles.Count == 1 && string.Equals(previousRoles[0], role, StringComparison.Ordinal))
            return RedirectToAction(nameof(Index));
        if (previousRoles.Contains("Owner") && role != "Owner" && await IsLastActiveOwnerAsync(user))
            return AdminError("The last active owner cannot be removed.");

        await using var transaction = await db.Database.BeginTransactionAsync();
        var removed = await users.RemoveFromRolesAsync(user, previousRoles);
        if (!removed.Succeeded)
            return IdentityError(removed);
        var added = await users.AddToRoleAsync(user, role);
        if (!added.Succeeded)
            return IdentityError(added);
        var stamp = await users.UpdateSecurityStampAsync(user);
        if (!stamp.Succeeded)
            return IdentityError(stamp);

        await transaction.CommitAsync();
        TempData["Success"] = $"Role for '{username}' updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(string username, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(username))
            return AdminError("Select a user.");

        var user = await FindUserAsync(username);
        if (user == null)
            return NotFound();
        if (user.IsActive == isActive)
            return RedirectToAction(nameof(Index));
        if (!isActive && string.Equals(user.Id, CurrentUserId, StringComparison.Ordinal))
            return AdminError("You cannot deactivate your own account.");
        if (!isActive && await IsLastActiveOwnerAsync(user))
            return AdminError("The last active owner cannot be deactivated.");

        user.IsActive = isActive;
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded)
            return IdentityError(result);
        var stamp = await users.UpdateSecurityStampAsync(user);
        if (!stamp.Succeeded)
            return IdentityError(stamp);

        TempData["Success"] = $"User '{username}' {(isActive ? "activated" : "deactivated")}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> AuditLog(string? username, DateTime? date, string? action)
    {
        var query = db.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(username))
            query = query.Where(entry => entry.ActorUsername == username);
        if (date.HasValue)
        {
            var start = date.Value.Date;
            query = query.Where(entry => entry.OccurredAt >= start && entry.OccurredAt < start.AddDays(1));
        }
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(entry => entry.Action == action.ToUpperInvariant());

        var entries = await query.OrderByDescending(entry => entry.OccurredAt)
            .Take(500)
            .Select(entry => new AuditLogEntry
            {
                At = entry.OccurredAt,
                Username = entry.ActorUsername,
                Action = entry.Action,
                TableName = entry.TableName,
                OldValues = entry.OldValues,
                NewValues = entry.NewValues
            })
            .ToListAsync();
        var model = new AuditLogViewModel
        {
            Users = await users.Users.Select(user => user.UserName!).Distinct().OrderBy(name => name).ToListAsync(),
            Actions = await db.AuditLogs.Select(entry => entry.Action).Distinct().OrderBy(name => name).ToListAsync(),
            SelectedUser = username,
            SelectedDate = date,
            SelectedAction = action,
            Entries = entries
        };
        return View(model);
    }

    private string? CurrentUserId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    private async Task<UserAdminIndexViewModel> BuildIndexModelAsync(CreateManagedUserViewModel? createUser = null)
    {
        var accounts = await users.Users.AsNoTracking().OrderBy(user => user.FullName).ToListAsync();
        var userModels = new List<ManagedUserViewModel>(accounts.Count);
        foreach (var account in accounts)
        {
            var assignedRoles = await users.GetRolesAsync(account);
            userModels.Add(new ManagedUserViewModel
            {
                Id = account.Id,
                Username = account.UserName ?? account.Email ?? "",
                FullName = account.FullName,
                Phone = account.PhoneNumber,
                Role = assignedRoles.FirstOrDefault() ?? "",
                IsActive = account.IsActive
            });
        }

        return new UserAdminIndexViewModel
        {
            Users = userModels,
            NewUser = createUser ?? new CreateManagedUserViewModel()
        };
    }

    private async Task<IActionResult> ShowIndexAsync(CreateManagedUserViewModel model)
    {
        SetRoles();
        return View("Index", await BuildIndexModelAsync(model));
    }

    private async Task<AutoTrackUser?> FindUserAsync(string username) =>
        await users.FindByNameAsync(username.Trim()) ?? await users.FindByEmailAsync(username.Trim());

    private async Task<bool> IsLastActiveOwnerAsync(AutoTrackUser user)
    {
        if (!user.IsActive || !(await users.GetRolesAsync(user)).Contains("Owner"))
            return false;
        var owners = await users.GetUsersInRoleAsync("Owner");
        return owners.Count(owner => owner.IsActive) <= 1;
    }

    private void AddIdentityErrors(IdentityResult result, string key)
    {
        foreach (var error in result.Errors)
            ModelState.AddModelError(key, error.Description);
    }

    private IActionResult IdentityError(IdentityResult result)
    {
        TempData["Error"] = string.Join("; ", result.Errors.Select(error => error.Description));
        return RedirectToAction(nameof(Index));
    }

    private void SetRoles() => ViewBag.Roles = AvailableRoles;

    private IActionResult AdminError(string message)
    {
        TempData["Error"] = message;
        return RedirectToAction(nameof(Index));
    }
}
