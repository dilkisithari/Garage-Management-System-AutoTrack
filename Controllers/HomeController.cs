using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using AutoTrack.Models;
using Microsoft.AspNetCore.Mvc;

namespace AutoTrack.Controllers;

public class HomeController : Controller
{
    [AllowAnonymous]
    public IActionResult Index() => View();

    /// <summary>Landing page after login (until each role has its own dashboard).</summary>
    [Authorize]
    public IActionResult Welcome() => View();

    /// <summary>Placeholder for pages other team members are still building.</summary>
    [Authorize]
    public IActionResult ComingSoon(string? page)
    {
        ViewData["PageName"] = string.IsNullOrWhiteSpace(page) ? "This page" : page;
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
