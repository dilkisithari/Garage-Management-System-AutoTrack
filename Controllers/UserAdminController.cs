using AutoTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoTrack.Controllers;

[Authorize(Roles = "Owner")]
public class UserAdminController : Controller
{
    private static readonly string[] Roles = { "Owner", "FrontDesk", "Mechanic", "Customer" };

    [HttpGet]
    public IActionResult Index()
    {
        SetRoles();
        return View(new UserAdminIndexViewModel { Users = MockUserStore.All() });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create([Bind(Prefix = "NewUser")] CreateManagedUserViewModel model)
    {
        if (!Roles.Contains(model.Role, StringComparer.Ordinal))
            ModelState.AddModelError("NewUser.Role", "Select a valid role.");

        if (!ModelState.IsValid)
            return ShowIndex(model);

        var user = new MockUser
        {
            Username = model.Username.Trim(),
            Password = model.Password,
            FullName = model.FullName.Trim(),
            Phone = model.Phone?.Trim(),
            Role = model.Role,
            IsActive = true
        };
        if (!MockUserStore.Add(user))
        {
            ModelState.AddModelError("NewUser.Username", "A user with this username already exists.");
            return ShowIndex(model);
        }

        MockAuditLog.Add(CurrentUsername, "User created", $"Created {user.Username} with role {user.Role}.");
        TempData["Success"] = $"User '{user.Username}' created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult UpdateRole(string username, string role)
    {
        if (string.IsNullOrWhiteSpace(username))
            return AdminError("Select a user.");
        if (!Roles.Contains(role, StringComparer.Ordinal))
            return AdminError("Select a valid role.");

        if (string.Equals(username, CurrentUsername, StringComparison.OrdinalIgnoreCase))
            return AdminError("You cannot change your own role.");

        var user = MockUserStore.Get(username);
        if (user == null) return NotFound();
        if (user.Role == role)
            return RedirectToAction(nameof(Index));

        if (!MockUserStore.UpdateRole(username, role))
            return AdminError("The last active owner cannot be removed.");

        MockAuditLog.Add(CurrentUsername, "Role changed", $"Changed {username} from {user.Role} to {role}.");
        TempData["Success"] = $"Role for '{username}' updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult SetActive(string username, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(username))
            return AdminError("Select a user.");
        if (!isActive && string.Equals(username, CurrentUsername, StringComparison.OrdinalIgnoreCase))
            return AdminError("You cannot deactivate your own account.");

        var user = MockUserStore.Get(username);
        if (user == null) return NotFound();
        if (user.IsActive == isActive)
            return RedirectToAction(nameof(Index));

        if (!MockUserStore.SetActive(username, isActive))
            return AdminError("The last active owner cannot be deactivated.");

        var action = isActive ? "User activated" : "User deactivated";
        MockAuditLog.Add(CurrentUsername, action, $"{username} was {(isActive ? "activated" : "deactivated")}.");
        TempData["Success"] = $"User '{username}' {(isActive ? "activated" : "deactivated")}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult AuditLog(string? username, DateTime? date, string? action)
    {
        var entries = MockAuditLog.All();
        var model = new AuditLogViewModel
        {
            Users = MockUserStore.All().Select(user => user.Username).ToList(),
            Actions = entries.Select(entry => entry.Action).Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name).ToList(),
            SelectedUser = username,
            SelectedDate = date,
            SelectedAction = action,
            Entries = entries
                .Where(entry => string.IsNullOrEmpty(username) ||
                                string.Equals(entry.Username, username, StringComparison.OrdinalIgnoreCase))
                .Where(entry => !date.HasValue || entry.At.Date == date.Value.Date)
                .Where(entry => string.IsNullOrEmpty(action) ||
                                string.Equals(entry.Action, action, StringComparison.OrdinalIgnoreCase))
                .ToList()
        };
        return View(model);
    }

    private string CurrentUsername => User.Identity?.Name ?? "";

    private IActionResult ShowIndex(CreateManagedUserViewModel model)
    {
        SetRoles();
        return View("Index", new UserAdminIndexViewModel
        {
            Users = MockUserStore.All(),
            NewUser = model
        });
    }

    private void SetRoles() => ViewBag.Roles = Roles;

    private IActionResult AdminError(string message)
    {
        TempData["Error"] = message;
        return RedirectToAction(nameof(Index));
    }
}
