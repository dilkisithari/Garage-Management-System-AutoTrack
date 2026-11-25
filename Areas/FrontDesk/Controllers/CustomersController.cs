using AutoTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoTrack.Areas.FrontDesk.Controllers;

[Area("FrontDesk")]
[Authorize(Roles = "FrontDesk,Owner")]
public class CustomersController : Controller
{
    private const int PageSize = 10;

    // GET: /FrontDesk/Customers
    [HttpGet]
    public IActionResult Index(string? search, int page = 1)
    {
        var all = MockCustomerData.Search(search);
        var totalCount = all.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
        if (totalPages < 1) totalPages = 1;
        page = Math.Clamp(page, 1, totalPages);

        var items = all.Skip((page - 1) * PageSize).Take(PageSize).ToList();

        var model = new CustomerListViewModel
        {
            Items = items,
            Search = search ?? "",
            Page = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            PageSize = PageSize
        };

        return View(model);
    }

    // GET: /FrontDesk/Customers/Create
    [HttpGet]
    public IActionResult Create() => View("Form", new CustomerFormViewModel());

    // POST: /FrontDesk/Customers/Create
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(CustomerFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View("Form", model);

        var record = new CustomerRecord
        {
            FullName = model.FullName.Trim(),
            Phone = model.Phone.Trim(),
            Email = model.Email?.Trim(),
            Address = model.Address?.Trim()
        };

        MockCustomerData.Save(record);
        TempData["Success"] = $"Customer '{record.FullName}' added successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /FrontDesk/Customers/Edit/5
    [HttpGet]
    public IActionResult Edit(int id)
    {
        var record = MockCustomerData.Get(id);
        if (record == null) return NotFound();

        var model = new CustomerFormViewModel
        {
            Id = record.Id,
            FullName = record.FullName,
            Phone = record.Phone,
            Email = record.Email,
            Address = record.Address
        };

        return View("Form", model);
    }

    // POST: /FrontDesk/Customers/Edit/5
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Edit(int id, CustomerFormViewModel model)
    {
        model.Id = id;

        if (!ModelState.IsValid)
            return View("Form", model);

        var record = new CustomerRecord
        {
            Id = id,
            FullName = model.FullName.Trim(),
            Phone = model.Phone.Trim(),
            Email = model.Email?.Trim(),
            Address = model.Address?.Trim()
        };

        MockCustomerData.Save(record);
        TempData["Success"] = $"Customer '{record.FullName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /FrontDesk/Customers/SetActive/5
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult SetActive(int id, bool isActive)
    {
        var record = MockCustomerData.Get(id);
        if (record == null) return NotFound();
        MockCustomerData.SetActive(id, isActive);
        TempData["Success"] = $"Customer '{record.FullName}' {(isActive ? "reactivated" : "deactivated")}. Their vehicles and history were kept.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /FrontDesk/Customers/5/Vehicles
    [HttpGet]
    [Route("FrontDesk/Customers/{id}/Vehicles")]
    public IActionResult Vehicles(int id)
    {
        var customer = MockCustomerData.Get(id);
        if (customer == null) return NotFound();

        var vehicles = MockVehicleData.GetByOwner(id);
        ViewBag.Customer = customer;
        return View(vehicles);
    }
}
