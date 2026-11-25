using AutoTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoTrack.Areas.FrontDesk.Controllers;

[Area("FrontDesk")]
[Authorize(Roles = "FrontDesk,Owner")]
public class VehiclesController : Controller
{
    // GET: /FrontDesk/Vehicles  (global vehicle list – used by navbar link)
    [HttpGet]
    public IActionResult Index(string? search)
    {
        var vehicles = MockVehicleData.All();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            vehicles = vehicles.Where(v =>
                v.RegistrationNo.ToLower().Contains(term) ||
                v.Make.ToLower().Contains(term) ||
                v.Model.ToLower().Contains(term) ||
                (v.Colour != null && v.Colour.ToLower().Contains(term))
            ).ToList();
        }

        // Build owner lookup for display
        var customers = MockCustomerData.All().ToDictionary(c => c.Id, c => c.FullName);
        ViewBag.Customers = customers;
        ViewBag.Search = search ?? "";
        ViewBag.TotalCount = MockVehicleData.Count();

        return View(vehicles);
    }

    // GET: /FrontDesk/Vehicles/Create?ownerId=5
    [HttpGet]
    public IActionResult Create(int? ownerId)
    {
        var model = new VehicleFormViewModel
        {
            OwnerId = ownerId ?? 0,
            OwnerOptions = BuildOwnerOptions()
        };
        return View("Form", model);
    }

    // POST: /FrontDesk/Vehicles/Create
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(VehicleFormViewModel model)
    {
        model.OwnerOptions = BuildOwnerOptions();

        if (!ModelState.IsValid)
            return View("Form", model);

        // Verify the chosen owner exists
        if (MockCustomerData.Get(model.OwnerId) == null)
        {
            ModelState.AddModelError(nameof(model.OwnerId), "Selected owner not found. Please choose a valid customer.");
            return View("Form", model);
        }

        var record = new VehicleRecord
        {
            RegistrationNo = model.RegistrationNo.Trim().ToUpperInvariant(),
            Make = model.Make.Trim(),
            Model = model.VehicleModel.Trim(),
            Year = model.Year,
            Colour = model.Colour?.Trim(),
            Mileage = model.Mileage,
            OwnerId = model.OwnerId
        };

        MockVehicleData.Save(record);
        TempData["Success"] = $"Vehicle '{record.RegistrationNo}' registered successfully.";

        // Return to the customer's vehicle list if we know who the owner is
        return RedirectToAction("Vehicles", "Customers", new { id = record.OwnerId });
    }

    // GET: /FrontDesk/Vehicles/Edit/5
    [HttpGet]
    public IActionResult Edit(int id)
    {
        var record = MockVehicleData.Get(id);
        if (record == null) return NotFound();

        var model = new VehicleFormViewModel
        {
            Id = record.Id,
            RegistrationNo = record.RegistrationNo,
            Make = record.Make,
            VehicleModel = record.Model,
            Year = record.Year,
            Colour = record.Colour,
            Mileage = record.Mileage,
            OwnerId = record.OwnerId,
            OwnerOptions = BuildOwnerOptions()
        };

        return View("Form", model);
    }

    // POST: /FrontDesk/Vehicles/Edit/5
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Edit(int id, VehicleFormViewModel model)
    {
        model.Id = id;
        model.OwnerOptions = BuildOwnerOptions();

        if (!ModelState.IsValid)
            return View("Form", model);

        if (MockCustomerData.Get(model.OwnerId) == null)
        {
            ModelState.AddModelError(nameof(model.OwnerId), "Selected owner not found. Please choose a valid customer.");
            return View("Form", model);
        }

        var record = new VehicleRecord
        {
            Id = id,
            RegistrationNo = model.RegistrationNo.Trim().ToUpperInvariant(),
            Make = model.Make.Trim(),
            Model = model.VehicleModel.Trim(),
            Year = model.Year,
            Colour = model.Colour?.Trim(),
            Mileage = model.Mileage,
            OwnerId = model.OwnerId
        };

        MockVehicleData.Save(record);
        TempData["Success"] = $"Vehicle '{record.RegistrationNo}' updated successfully.";
        return RedirectToAction("Vehicles", "Customers", new { id = record.OwnerId });
    }

    // POST: /FrontDesk/Vehicles/Delete/5
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Delete(int id, int returnOwnerId = 0)
    {
        var record = MockVehicleData.Get(id);
        if (record == null) return NotFound();

        var ownerId = record.OwnerId;
        MockVehicleData.Delete(id);
        TempData["Success"] = $"Vehicle '{record.RegistrationNo}' deleted.";

        // If we know which customer page to return to, go there
        if (ownerId > 0)
            return RedirectToAction("Vehicles", "Customers", new { id = ownerId });

        return RedirectToAction(nameof(Index));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static List<VehicleOwnerOption> BuildOwnerOptions() =>
        MockCustomerData.Active()
            .Select(c => new VehicleOwnerOption { Id = c.Id, DisplayName = $"{c.FullName} ({c.Phone})" })
            .ToList();
}
