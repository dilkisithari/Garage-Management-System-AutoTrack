using AutoTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoTrack.Areas.FrontDesk.Controllers;

[Area("FrontDesk")]
[Authorize(Roles = "FrontDesk,Owner")]
public class SuppliersController : Controller
{
    // GET: /FrontDesk/Suppliers
    public IActionResult Index(string? search, string? status)
    {
        var suppliers = MockSupplierData.All();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status.Equals("active", StringComparison.OrdinalIgnoreCase))
                suppliers = suppliers.Where(s => s.IsActive).ToList();
            else if (status.Equals("inactive", StringComparison.OrdinalIgnoreCase))
                suppliers = suppliers.Where(s => !s.IsActive).ToList();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            suppliers = suppliers.Where(s =>
                s.Name.ToLower().Contains(term) ||
                s.PartsSupplied.ToLower().Contains(term) ||
                (s.ContactPerson != null && s.ContactPerson.ToLower().Contains(term)) ||
                (s.Address != null && s.Address.ToLower().Contains(term)) ||
                s.Phone.Contains(term)
            ).ToList();
        }

        ViewBag.Search = search ?? "";
        ViewBag.Status = status ?? "";
        ViewBag.TotalCount = MockSupplierData.All().Count;
        ViewBag.ActiveCount = MockSupplierData.All().Count(s => s.IsActive);

        return View(suppliers);
    }

    // GET: /FrontDesk/Suppliers/Create
    [HttpGet]
    public IActionResult Create()
    {
        return View("Form", new SupplierViewModel());
    }

    // POST: /FrontDesk/Suppliers/Create
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(SupplierViewModel model)
    {
        if (!ModelState.IsValid)
            return View("Form", model);

        MockSupplierData.Save(model);
        TempData["Success"] = $"Supplier '{model.Name}' added successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /FrontDesk/Suppliers/Edit/5
    [HttpGet]
    public IActionResult Edit(int id)
    {
        var supplier = MockSupplierData.Get(id);
        if (supplier == null) return NotFound();

        return View("Form", supplier);
    }

    // POST: /FrontDesk/Suppliers/Edit/5
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Edit(int id, SupplierViewModel model)
    {
        model.Id = id;

        if (!ModelState.IsValid)
            return View("Form", model);

        MockSupplierData.Save(model);
        TempData["Success"] = $"Supplier '{model.Name}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /FrontDesk/Suppliers/Delete/5
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        var supplier = MockSupplierData.Get(id);
        if (supplier == null) return NotFound();

        MockSupplierData.Delete(id);
        TempData["Success"] = $"Supplier '{supplier.Name}' deleted.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /FrontDesk/Suppliers/ToggleActive/5
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ToggleActive(int id)
    {
        var supplier = MockSupplierData.Get(id);
        if (supplier == null) return NotFound();

        MockSupplierData.ToggleActive(id);
        TempData["Success"] = $"Supplier '{supplier.Name}' status changed to {(supplier.IsActive ? "Inactive" : "Active")}.";
        return RedirectToAction(nameof(Index));
    }
}
