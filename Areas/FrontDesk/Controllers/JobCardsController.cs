using AutoTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AutoTrack.Areas.FrontDesk.Controllers;

[Area("FrontDesk")]
[Authorize(Roles = "FrontDesk,Owner")]
public class JobCardsController : Controller
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
    public IActionResult Create(JobCardViewModel model) => SaveJob(model);

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
    public IActionResult Edit(int id, JobCardViewModel model)
    {
        model.Id = id;
        return SaveJob(model);
    }

    private IActionResult SaveJob(JobCardViewModel model)
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
        MockJobData.Save(model);
        TempData["Success"] = isNew ? "Job card created." : "Job card updated.";
        return RedirectToAction(nameof(Board));
    }

    private void FillLists()
    {
        ViewBag.Mechanics = new SelectList(MockJobData.Mechanics);
        ViewBag.Statuses = new SelectList(MockJobData.Statuses);
    }
}
