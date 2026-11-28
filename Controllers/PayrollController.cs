using System.Globalization;
using AutoTrack.Models;
using AutoTrack.Models.Payroll;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoTrack.Controllers;

[Authorize(Roles = "Owner,FrontDesk")]
public class PayrollController : Controller
{
    // GET: /Payroll
    [HttpGet]
    public IActionResult Index(string? month)
    {
        var model = MockPayrollData.GetPayrollRun(month);
        return View(model);
    }

    // POST: /Payroll/RunPayroll
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult RunPayroll(string month)
    {
        var (ok, message) = MockPayrollData.RunPayroll(month, CurrentUsername);
        if (ok)
        {
            MockAuditLog.Add(CurrentUsername, "Payroll run", message);
            TempData["Success"] = message;
        }
        else
        {
            TempData["Error"] = message;
        }

        return RedirectToAction(nameof(Index), new { month });
    }

    // GET: /Payroll/Payslip?employeeId=1&month=2026-11
    [HttpGet]
    public IActionResult Payslip(int employeeId, string? month)
    {
        var model = MockPayrollData.GetPayslip(employeeId, month);
        if (model == null)
            return NotFound();

        return View(model);
    }

    // GET: /Payroll/Expenses
    [HttpGet]
    public IActionResult Expenses(string? month, string? category)
    {
        var model = MockPayrollData.GetExpensesPage(month, category);
        return View(model);
    }

    // POST: /Payroll/AddExpense
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult AddExpense(
        [Bind(Prefix = "Form")] ExpenseFormViewModel form,
        string? month,
        string? category)
    {
        if (!MockPayrollData.ExpenseCategories.Contains(form.Category, StringComparer.Ordinal))
            ModelState.AddModelError("Form.Category", "Please select a valid expense category.");

        if (form.Date > DateTime.Today.AddDays(30))
            ModelState.AddModelError("Form.Date", "Expense date cannot be more than 30 days in the future.");

        if (!ModelState.IsValid)
        {
            var invalidModel = MockPayrollData.GetExpensesPage(month, category, form);
            return View("Expenses", invalidModel);
        }

        var saved = MockPayrollData.AddExpense(form);
        var savedMonth = saved.Date.ToString("yyyy-MM", CultureInfo.InvariantCulture);

        MockAuditLog.Add(
            CurrentUsername,
            "Expense recorded",
            $"Recorded {saved.Category} expense of LKR {saved.Amount:N2} on {saved.Date:yyyy-MM-dd}.");

        TempData["Success"] = $"Expense '{saved.Category}' (LKR {saved.Amount:N2}) added for {saved.Date:dd MMM yyyy}.";
        return RedirectToAction(nameof(Expenses), new { month = savedMonth, category });
    }

    // POST: /Payroll/DeleteExpense
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult DeleteExpense(int id, string? month, string? category)
    {
        if (MockPayrollData.DeleteExpense(id))
        {
            MockAuditLog.Add(CurrentUsername, "Expense deleted", $"Removed expense #{id}.");
            TempData["Success"] = "Expense entry removed.";
        }
        else
        {
            TempData["Error"] = "Expense entry not found.";
        }

        return RedirectToAction(nameof(Expenses), new { month, category });
    }

    private string CurrentUsername => User.Identity?.Name ?? "Owner";
}
