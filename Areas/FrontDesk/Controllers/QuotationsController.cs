using AutoTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AutoTrack.Areas.FrontDesk.Controllers;

[Area("FrontDesk")]
[Authorize(Roles = "FrontDesk,Owner")]
public class QuotationsController : Controller
{
    // List / Dashboard
    public IActionResult Index(string? status, string? search)
    {
        var quotes = MockQuotationData.All();

        if (!string.IsNullOrWhiteSpace(status))
            quotes = quotes.Where(q => q.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            quotes = quotes.Where(q =>
                q.QuoteNo.ToLower().Contains(s) ||
                q.VehicleNo.ToLower().Contains(s) ||
                q.CustomerName.ToLower().Contains(s)).ToList();
        }

        ViewBag.SelectedStatus = status ?? "";
        ViewBag.Search = search ?? "";
        ViewBag.PendingOwnerCount = MockQuotationData.All().Count(q => q.Status == "Pending Owner Approval");

        return View(quotes);
    }

    // Create quote
    [HttpGet]
    public IActionResult Create()
    {
        var quote = new QuotationViewModel
        {
            Parts = { new QuotationPartItem() },
            Labour = { new QuotationLabourItem() }
        };
        return View("Form", quote);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(QuotationViewModel model) => SaveQuote(model);

    // Edit quote
    [HttpGet]
    public IActionResult Edit(int id)
    {
        var quote = MockQuotationData.Get(id);
        if (quote == null) return NotFound();

        if (quote.Status == "Converted")
        {
            TempData["Error"] = "Converted quotations cannot be modified.";
            return RedirectToAction(nameof(Details), new { id });
        }

        return View("Form", quote);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Edit(int id, QuotationViewModel model)
    {
        model.Id = id;
        return SaveQuote(model);
    }

    // Details / Printable sheet
    [HttpGet]
    public IActionResult Details(int id)
    {
        var quote = MockQuotationData.Get(id);
        if (quote == null) return NotFound();

        ViewBag.Mechanics = new SelectList(MockJobData.Mechanics);
        return View(quote);
    }

    // Front Desk sends to Owner
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult SubmitForApproval(int id)
    {
        var username = User.Identity?.Name ?? "FrontDesk";
        if (MockQuotationData.SubmitForApproval(id, username))
            TempData["Success"] = "Quotation submitted to Owner for approval.";
        else
            TempData["Error"] = "Could not submit quotation.";

        return RedirectToAction(nameof(Details), new { id });
    }

    // Owner Approves
    [HttpPost, Authorize(Roles = "Owner"), ValidateAntiForgeryToken]
    public IActionResult OwnerApprove(int id, string remarks)
    {
        var username = User.Identity?.Name ?? "Owner";
        if (MockQuotationData.OwnerApprove(id, remarks, username))
            TempData["Success"] = "Quotation approved by Owner.";
        else
            TempData["Error"] = "Failed to approve quotation.";

        return RedirectToAction(nameof(Details), new { id });
    }

    // Owner Requests Revision
    [HttpPost, Authorize(Roles = "Owner"), ValidateAntiForgeryToken]
    public IActionResult OwnerReject(int id, string remarks)
    {
        if (string.IsNullOrWhiteSpace(remarks))
        {
            TempData["Error"] = "Please provide a reason or note for requested revisions.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var username = User.Identity?.Name ?? "Owner";
        if (MockQuotationData.OwnerReject(id, remarks, username))
            TempData["Warning"] = "Revision requested from Front Desk.";
        else
            TempData["Error"] = "Failed to update quotation.";

        return RedirectToAction(nameof(Details), new { id });
    }

    // Front Desk sends to Customer
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult SendToCustomer(int id)
    {
        var username = User.Identity?.Name ?? "FrontDesk";
        if (MockQuotationData.SendToCustomer(id, username))
            TempData["Success"] = "Quotation status updated to 'Sent to Customer'.";

        return RedirectToAction(nameof(Details), new { id });
    }

    // Convert to Job Card
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ConvertToJobCard(int id, string mechanic)
    {
        if (string.IsNullOrWhiteSpace(mechanic))
        {
            TempData["Error"] = "Please select a mechanic to assign the new job card.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var username = User.Identity?.Name ?? "FrontDesk";
        var jobNo = MockQuotationData.ConvertToJobCard(id, mechanic, username);

        if (jobNo != null)
        {
            TempData["Success"] = $"Quotation successfully converted to Job Card {jobNo}!";
            return RedirectToAction("Board", "JobCards");
        }

        TempData["Error"] = "Could not convert quotation to job card.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private IActionResult SaveQuote(QuotationViewModel model)
    {
        // Drop blank rows
        model.Parts = (model.Parts ?? new()).Where(p => !string.IsNullOrWhiteSpace(p.PartName)).ToList();
        model.Labour = (model.Labour ?? new()).Where(l => !string.IsNullOrWhiteSpace(l.Description)).ToList();

        if (!ModelState.IsValid)
            return View("Form", model);

        var username = User.Identity?.Name ?? "User";
        var isNew = model.Id == 0;
        MockQuotationData.Save(model, username);

        TempData["Success"] = isNew ? "Quotation created as Draft." : "Quotation updated.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }
}