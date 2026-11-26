using AutoTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoTrack.Controllers;

[Authorize(Roles = "Owner,FrontDesk")]
public class EmployeesController : Controller
{
    // GET: /Employees
    [HttpGet]
    public IActionResult Index(string? search, string? status)
    {
        var all = MockEmployeeData.All();
        var items = MockEmployeeData.Search(search, status);

        var model = new EmployeeListViewModel
        {
            Items = items,
            Search = search ?? "",
            Status = status ?? "",
            TotalCount = all.Count,
            ActiveCount = all.Count(e => e.IsActive),
            InactiveCount = all.Count(e => !e.IsActive)
        };

        return View(model);
    }

    // GET: /Employees/Create
    [HttpGet]
    public IActionResult Create()
    {
        return View("Form", new EmployeeFormViewModel
        {
            RoleOptions = MockEmployeeData.Roles.ToList()
        });
    }

    // POST: /Employees/Create
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(EmployeeFormViewModel model)
    {
        model.RoleOptions = MockEmployeeData.Roles.ToList();

        if (!MockEmployeeData.Roles.Contains(model.Role, StringComparer.Ordinal))
            ModelState.AddModelError(nameof(model.Role), "Select a valid role.");

        if (!ModelState.IsValid)
            return View("Form", model);

        var record = new EmployeeRecord
        {
            FullName = model.FullName.Trim(),
            Role = model.Role.Trim(),
            HourlyRate = Math.Round(model.HourlyRate, 2),
            Phone = model.Phone.Trim(),
            IsActive = model.IsActive,
            Username = string.IsNullOrWhiteSpace(model.Username) ? null : model.Username.Trim()
        };

        MockEmployeeData.Save(record);
        MockAuditLog.Add(CurrentUsername, "Employee added", $"Added {record.FullName} ({record.Role}, LKR {record.HourlyRate:N2}/hr).");
        TempData["Success"] = $"Employee '{record.FullName}' added successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Employees/Edit/5
    [HttpGet]
    public IActionResult Edit(int id)
    {
        var record = MockEmployeeData.Get(id);
        if (record == null) return NotFound();

        var model = new EmployeeFormViewModel
        {
            Id = record.Id,
            FullName = record.FullName,
            Role = record.Role,
            HourlyRate = record.HourlyRate,
            Phone = record.Phone,
            IsActive = record.IsActive,
            Username = record.Username,
            RoleOptions = MockEmployeeData.Roles.ToList()
        };

        return View("Form", model);
    }

    // POST: /Employees/Edit/5
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Edit(int id, EmployeeFormViewModel model)
    {
        model.Id = id;
        model.RoleOptions = MockEmployeeData.Roles.ToList();

        if (MockEmployeeData.Get(id) == null)
            return NotFound();

        if (!MockEmployeeData.Roles.Contains(model.Role, StringComparer.Ordinal))
            ModelState.AddModelError(nameof(model.Role), "Select a valid role.");

        if (!ModelState.IsValid)
            return View("Form", model);

        var record = new EmployeeRecord
        {
            Id = id,
            FullName = model.FullName.Trim(),
            Role = model.Role.Trim(),
            HourlyRate = Math.Round(model.HourlyRate, 2),
            Phone = model.Phone.Trim(),
            IsActive = model.IsActive,
            Username = string.IsNullOrWhiteSpace(model.Username) ? null : model.Username.Trim()
        };

        MockEmployeeData.Save(record);
        MockAuditLog.Add(CurrentUsername, "Employee updated", $"Updated {record.FullName} ({record.Role}, LKR {record.HourlyRate:N2}/hr).");
        TempData["Success"] = $"Employee '{record.FullName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Employees/SetActive/5
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult SetActive(int id, bool isActive)
    {
        var record = MockEmployeeData.Get(id);
        if (record == null) return NotFound();

        MockEmployeeData.SetActive(id, isActive);
        MockAuditLog.Add(
            CurrentUsername,
            isActive ? "Employee activated" : "Employee deactivated",
            $"{record.FullName} set to {(isActive ? "Active" : "Inactive")}.");

        TempData["Success"] = $"Employee '{record.FullName}' marked {(isActive ? "Active" : "Inactive")}.";
        return RedirectToAction(nameof(Index));
    }

    private string CurrentUsername => User.Identity?.Name ?? "";
}
