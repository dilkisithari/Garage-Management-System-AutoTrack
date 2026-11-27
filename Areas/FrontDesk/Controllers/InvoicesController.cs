using AutoTrack.Models;
using AutoTrack.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoTrack.Areas.FrontDesk.Controllers;

[Area("FrontDesk")]
[Authorize(Roles = "FrontDesk,Owner")]
public class InvoicesController(AutoTrackDbContext db) : Controller
{
    // GET: /FrontDesk/Invoices
    public IActionResult Index(string? status, string? search)
    {
        var invoices = MockInvoiceData.All();

        if (!string.IsNullOrWhiteSpace(status))
        {
            invoices = invoices.Where(i => i.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            invoices = invoices.Where(i =>
                i.InvoiceNo.ToLower().Contains(term) ||
                (i.JobCardNo != null && i.JobCardNo.ToLower().Contains(term)) ||
                i.CustomerName.ToLower().Contains(term) ||
                i.VehicleNo.ToLower().Contains(term)
            ).ToList();
        }

        var all = MockInvoiceData.All();
        ViewBag.Status = status ?? "";
        ViewBag.Search = search ?? "";
        ViewBag.TotalBilled = all.Sum(i => i.TotalAmount);
        ViewBag.TotalPaid = all.Sum(i => i.PaidAmount);
        ViewBag.TotalBalance = all.Sum(i => i.BalanceDue);
        ViewBag.UnpaidCount = all.Count(i => i.Status == "Unpaid");
        ViewBag.PartialCount = all.Count(i => i.Status == "Partially Paid");
        ViewBag.PaidCount = all.Count(i => i.Status == "Paid");

        return View(invoices);
    }

    // GET: /FrontDesk/Invoices/Details/5 (Printable Layout)
    public IActionResult Details(int id)
    {
        var invoice = MockInvoiceData.Get(id);
        if (invoice == null) return NotFound();

        return View(invoice);
    }

    // GET/POST: /FrontDesk/Invoices/CreateFromJob?jobCardId=5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFromJob(int jobCardId)
    {
        var invoice = MockInvoiceData.CreateFromJobCard(jobCardId);
        if (invoice == null)
        {
            TempData["Error"] = "Job card not found or could not generate invoice.";
            return RedirectToAction("Board", "JobCards");
        }

        var storedInvoice = await db.Invoices.SingleOrDefaultAsync(item => item.InvoiceNo == invoice.InvoiceNo);
        if (storedInvoice == null)
        {
            db.Invoices.Add(new InvoiceRecord
            {
                InvoiceNo = invoice.InvoiceNo,
                IssuedAt = invoice.IssueDate,
                TotalAmount = invoice.TotalAmount,
                PaidAmount = invoice.PaidAmount,
                Status = invoice.Status
            });
            await db.SaveChangesAsync();
        }

        TempData["Success"] = $"Invoice {invoice.InvoiceNo} generated successfully for {invoice.JobCardNo}.";
        return RedirectToAction(nameof(Details), new { id = invoice.Id });
    }

    // GET: /FrontDesk/Invoices/Payment/5
    [HttpGet]
    public IActionResult Payment(int id)
    {
        var invoice = MockInvoiceData.Get(id);
        if (invoice == null) return NotFound();

        if (invoice.BalanceDue <= 0)
        {
            TempData["Success"] = $"Invoice {invoice.InvoiceNo} is already fully paid.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = new PaymentEntryViewModel
        {
            InvoiceId = invoice.Id,
            InvoiceNo = invoice.InvoiceNo,
            CustomerName = invoice.CustomerName,
            VehicleNo = invoice.VehicleNo,
            TotalAmount = invoice.TotalAmount,
            CurrentPaid = invoice.PaidAmount,
            BalanceDue = invoice.BalanceDue,
            Amount = invoice.BalanceDue, // Default to full remaining balance for convenience
            PaymentMethod = "Cash",
            PaymentDate = DateTime.Today,
            ReferenceNo = $"REC-{DateTime.Now:yyyyMMddHHmm}"
        };

        return View(model);
    }

    // POST: /FrontDesk/Invoices/Payment
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Payment(PaymentEntryViewModel model)
    {
        var invoice = MockInvoiceData.Get(model.InvoiceId);
        if (invoice == null) return NotFound();

        // Validate Amount against balance
        if (model.Amount <= 0)
        {
            ModelState.AddModelError("Amount", "Payment amount must be greater than zero.");
        }
        else if (model.Amount > invoice.BalanceDue)
        {
            ModelState.AddModelError("Amount", $"Payment amount (LKR {model.Amount:N2}) cannot exceed remaining balance (LKR {invoice.BalanceDue:N2}).");
        }

        if (!ModelState.IsValid)
        {
            model.InvoiceNo = invoice.InvoiceNo;
            model.CustomerName = invoice.CustomerName;
            model.VehicleNo = invoice.VehicleNo;
            model.TotalAmount = invoice.TotalAmount;
            model.CurrentPaid = invoice.PaidAmount;
            model.BalanceDue = invoice.BalanceDue;
            return View(model);
        }

        await using var transaction = await db.Database.BeginTransactionAsync();
        var databaseInvoice = await db.Invoices.SingleOrDefaultAsync(item => item.InvoiceNo == invoice.InvoiceNo);
        if (databaseInvoice == null)
        {
            databaseInvoice = new InvoiceRecord
            {
                InvoiceNo = invoice.InvoiceNo,
                IssuedAt = invoice.IssueDate,
                TotalAmount = invoice.TotalAmount,
                PaidAmount = invoice.PaidAmount,
                Status = invoice.Status
            };
            db.Invoices.Add(databaseInvoice);
            await db.SaveChangesAsync();
        }

        var cashierName = User.FindFirst("FullName")?.Value ?? User.Identity?.Name ?? "Front Desk Cashier";
        var paymentAmount = Math.Round(model.Amount, 2);
        databaseInvoice.PaidAmount = invoice.PaidAmount + paymentAmount;
        databaseInvoice.Status = databaseInvoice.PaidAmount >= databaseInvoice.TotalAmount
            ? "Paid"
            : "Partially Paid";
        db.Payments.Add(new PaymentRecord
        {
            Invoice = databaseInvoice,
            Amount = paymentAmount,
            Method = model.PaymentMethod,
            PaidAt = model.PaymentDate
        });
        await db.SaveChangesAsync();

        if (!MockInvoiceData.AddPayment(model, cashierName))
        {
            TempData["Error"] = "Unable to record payment.";
            return RedirectToAction(nameof(Details), new { id = model.InvoiceId });
        }

        await transaction.CommitAsync();

        TempData["Success"] = $"Payment of LKR {model.Amount:N2} via {model.PaymentMethod} recorded for Invoice {invoice.InvoiceNo}.";
        return RedirectToAction(nameof(Details), new { id = model.InvoiceId });
    }
}
