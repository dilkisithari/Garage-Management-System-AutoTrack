using AutoTrack.Models.Parts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoTrack.Controllers;

/// <summary>Option 6 - Parts inventory (mock data, no database).</summary>
[Authorize(Roles = "Owner,FrontDesk")]
public class PartsController : Controller
{
    private const int PageSize = 8;

    // GET: /Parts
    [HttpGet]
    public IActionResult Index(string? search, string? category, string? stock, int page = 1)
    {
        var all = MockPartData.All();
        IEnumerable<PartRecord> query = all;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                p.Code.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Supplier.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category == category);

        query = stock switch
        {
            "low" => query.Where(p => p.IsLowStock),
            "out" => query.Where(p => p.IsOutOfStock),
            "ok" => query.Where(p => !p.IsLowStock),
            _ => query
        };

        var filtered = query.ToList();
        var totalPages = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var model = new PartListViewModel
        {
            Items = filtered.Skip((page - 1) * PageSize).Take(PageSize).ToList(),
            Search = search ?? "",
            Category = category ?? "",
            Stock = stock ?? "",
            Categories = MockPartData.Categories.ToList(),
            Page = page,
            PageSize = PageSize,
            TotalPages = totalPages,
            FilteredCount = filtered.Count,
            TotalCount = all.Count,
            LowStockCount = all.Count(p => p.IsLowStock),
            OutOfStockCount = all.Count(p => p.IsOutOfStock),
            StockValue = all.Sum(p => p.StockValue)
        };

        return View(model);
    }

    // GET: /Parts/Create
    [HttpGet]
    public IActionResult Create()
    {
        return View("Form", WithOptions(new PartFormViewModel()));
    }

    // POST: /Parts/Create
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(PartFormViewModel model)
    {
        ValidateRules(model);
        if (!ModelState.IsValid)
            return View("Form", WithOptions(model));

        var record = ToRecord(model);
        MockPartData.Save(record);
        TempData["Success"] = $"Part '{record.Name}' ({record.Code}) added successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Parts/Edit/5
    [HttpGet]
    public IActionResult Edit(int id)
    {
        var part = MockPartData.Get(id);
        if (part == null) return NotFound();

        return View("Form", WithOptions(new PartFormViewModel
        {
            Id = part.Id,
            Code = part.Code,
            Name = part.Name,
            Category = part.Category,
            Stock = part.Stock,
            ReorderLevel = part.ReorderLevel,
            UnitCost = part.UnitCost,
            Price = part.Price,
            Supplier = part.Supplier,
            IsActive = part.IsActive
        }));
    }

    // POST: /Parts/Edit/5
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Edit(int id, PartFormViewModel model)
    {
        if (MockPartData.Get(id) == null) return NotFound();
        model.Id = id;

        ValidateRules(model);
        if (!ModelState.IsValid)
            return View("Form", WithOptions(model));

        var record = ToRecord(model);
        MockPartData.Save(record);
        TempData["Success"] = $"Part '{record.Name}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Parts/Delete/5
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        var part = MockPartData.Get(id);
        if (part == null) return NotFound();

        MockPartData.Delete(id);
        TempData["Success"] = $"Part '{part.Name}' deleted.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Parts/LowStock
    [HttpGet]
    public IActionResult LowStock()
    {
        var items = MockPartData.All()
            .Where(p => p.IsActive && p.IsLowStock)
            .OrderBy(p => p.Stock == 0 ? 0 : 1)
            .ThenBy(p => p.Stock - p.ReorderLevel)
            .ToList();

        return View(new LowStockViewModel
        {
            Items = items,
            RecentReorders = MockPartData.RecentReorders(),
            OutOfStockCount = items.Count(p => p.IsOutOfStock),
            EstimatedReorderCost = items.Sum(p => SuggestedQuantity(p) * p.UnitCost)
        });
    }

    // POST: /Parts/CreateReorder
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult CreateReorder(ReorderFormModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = ModelState.Values
                .SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault()
                ?? "Could not create the reorder.";
            return RedirectToAction(nameof(LowStock));
        }

        var reorder = MockPartData.AddReorder(model.PartId, model.Quantity!.Value);
        if (reorder == null) return NotFound();

        TempData["Success"] =
            $"Reorder created: {reorder.Quantity} x {reorder.PartName} from {reorder.Supplier} (est. LKR {reorder.EstimatedCost:N2}).";
        return RedirectToAction(nameof(LowStock));
    }

    /// <summary>Order enough to reach twice the reorder level.</summary>
    public static int SuggestedQuantity(PartRecord p) => Math.Max(1, p.ReorderLevel * 2 - p.Stock);

    private void ValidateRules(PartFormViewModel model)
    {
        if (!string.IsNullOrWhiteSpace(model.Code) && MockPartData.CodeExists(model.Code.Trim(), model.Id))
            ModelState.AddModelError(nameof(model.Code), "This part code already exists.");

        if (!string.IsNullOrWhiteSpace(model.Category) && !MockPartData.Categories.Contains(model.Category))
            ModelState.AddModelError(nameof(model.Category), "Select a valid category.");

        if (!string.IsNullOrWhiteSpace(model.Supplier) && !MockPartData.SupplierNames().Contains(model.Supplier))
            ModelState.AddModelError(nameof(model.Supplier), "Select a valid supplier.");

        if (model.UnitCost.HasValue && model.Price.HasValue && model.Price.Value < model.UnitCost.Value)
            ModelState.AddModelError(nameof(model.Price), "Selling price cannot be lower than the unit cost.");
    }

    private static PartFormViewModel WithOptions(PartFormViewModel model)
    {
        model.CategoryOptions = MockPartData.Categories.ToList();
        model.SupplierOptions = MockPartData.SupplierNames();
        return model;
    }

    private static PartRecord ToRecord(PartFormViewModel m) => new()
    {
        Id = m.Id,
        Code = m.Code.Trim().ToUpperInvariant(),
        Name = m.Name.Trim(),
        Category = m.Category,
        Stock = m.Stock ?? 0,
        ReorderLevel = m.ReorderLevel ?? 0,
        UnitCost = Math.Round(m.UnitCost ?? 0, 2),
        Price = Math.Round(m.Price ?? 0, 2),
        Supplier = m.Supplier,
        IsActive = m.IsActive
    };
}
