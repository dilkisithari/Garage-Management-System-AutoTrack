using AutoTrack.Models;
using AutoTrack.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AutoTrack.Areas.FrontDesk.Controllers;

[Area("FrontDesk")]
[Authorize(Roles = "FrontDesk,Owner")]
public class JobCardsController(
    AutoTrackDbContext db,
    UserManager<AutoTrackUser> users) : Controller
{
    // Page 24 - Job card list / board
    public IActionResult Board()
    {
        ViewBag.Mechanics = MockJobData.Mechanics;
        ViewBag.Statuses = MockJobData.Statuses;
        return View(MockJobData.All());
    }

    // Page 25 - Create job card
    [HttpGet]
    public IActionResult Create()
    {
        FillLists();
        var job = new JobCardViewModel
        {
            Tasks = { new JobTaskItem() },
            Parts = { new JobPartItem() },
            Labour = { new JobLabourItem() }
        };
        return View("Form", job);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create(JobCardViewModel model) => SaveJobAsync(model);

    // Page 25 - Edit job card
    [HttpGet]
    public IActionResult Edit(int id)
    {
        var job = MockJobData.Get(id);
        if (job == null) return NotFound();
        FillLists();
        return View("Form", job);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(int id, JobCardViewModel model)
    {
        model.Id = id;
        return SaveJobAsync(model);
    }

    private async Task<IActionResult> SaveJobAsync(JobCardViewModel model)
    {
        // Rows left empty in the form are dropped
        model.Tasks = (model.Tasks ?? new()).Where(t => !string.IsNullOrWhiteSpace(t.Description)).ToList();
        model.Parts = (model.Parts ?? new()).Where(p => !string.IsNullOrWhiteSpace(p.PartName)).ToList();
        model.Labour = (model.Labour ?? new()).Where(l => !string.IsNullOrWhiteSpace(l.Description)).ToList();

        if (!ModelState.IsValid)
        {
            FillLists();
            return View("Form", model);
        }

        var isNew = model.Id == 0;
        if (isNew && string.IsNullOrWhiteSpace(model.JobNo))
            model.JobNo = $"JC-{Guid.NewGuid():N}"[..15].ToUpperInvariant();
        await PersistServiceJobAsync(model);
        MockJobData.Save(model);
        TempData["Success"] = isNew ? "Job card created." : "Job card updated.";
        return RedirectToAction(nameof(Board));
    }

    private async Task PersistServiceJobAsync(JobCardViewModel model)
    {
        var normalizedVehicleNo = model.VehicleNo.Trim().ToUpperInvariant();
        var vehicle = await db.CustomerVehicles.SingleOrDefaultAsync(
            item => item.VehicleNo == normalizedVehicleNo);
        var customer = vehicle == null
            ? await users.Users.FirstOrDefaultAsync(user => user.FullName == model.CustomerName.Trim())
            : null;

        if (vehicle == null && customer != null)
        {
            vehicle = new CustomerVehicle
            {
                VehicleNo = normalizedVehicleNo,
                CustomerId = customer.Id
            };
            db.CustomerVehicles.Add(vehicle);
        }

        var job = await db.ServiceJobs.SingleOrDefaultAsync(item => item.JobNo == model.JobNo);
        if (job == null)
        {
            job = new ServiceJobRecord { JobNo = model.JobNo };
            db.ServiceJobs.Add(job);
        }

        job.VehicleNo = normalizedVehicleNo;
        job.CustomerId = vehicle?.CustomerId;
        job.DateOpened = model.DateOpened;
        job.Complaint = model.Complaint;
        job.WorkDone = string.Join('\n', model.Tasks
            .Where(task => task.Done && !string.IsNullOrWhiteSpace(task.Description))
            .Select(task => task.Description!.Trim()));
        job.PartsJson = JsonSerializer.Serialize(model.Parts
            .Where(part => !string.IsNullOrWhiteSpace(part.PartName))
            .Select(part => new ServiceHistoryPart
            {
                PartName = part.PartName!.Trim(),
                Quantity = part.Quantity,
                UnitPrice = part.UnitPrice
            }));
        job.LabourJson = JsonSerializer.Serialize(model.Labour
            .Where(labour => !string.IsNullOrWhiteSpace(labour.Description))
            .Select(labour => new ServiceHistoryLabour
            {
                Description = labour.Description!.Trim(),
                Hours = labour.Hours,
                Rate = labour.Rate
            }));
        job.TotalCost = model.GrandTotal;
        job.Status = model.Status;

        await db.SaveChangesAsync();
    }

    private void FillLists()
    {
        ViewBag.Mechanics = new SelectList(MockJobData.Mechanics);
        ViewBag.Statuses = new SelectList(MockJobData.Statuses);
    }
}
