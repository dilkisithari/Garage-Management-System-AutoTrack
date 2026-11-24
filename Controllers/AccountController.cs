using System.Security.Claims;
using AutoTrack.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoTrack.Controllers;

public class AccountController : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Welcome", "Home");

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var user = MockUserStore.Validate(model.Username, model.Password);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        await SignInAsync(user, model.RememberMe);
        MockAuditLog.Add(user.Username, "Signed in", "Signed in to AutoTrack.");

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction("Welcome", "Home");
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Welcome", "Home");
        return View(new RegisterViewModel());
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public IActionResult Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (MockUserStore.Exists(model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this e-mail already exists.");
            return View(model);
        }

        var user = new MockUser
        {
            Username = model.Email.Trim(),
            Password = model.Password,
            FullName = model.FullName.Trim(),
            Phone = model.Phone.Trim(),
            Role = "Customer"
        };
        if (!MockUserStore.Add(user))
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this e-mail already exists.");
            return View(model);
        }

        MockAuditLog.Add(user.Username, "Account created", "Created a customer account.");
        TempData["Success"] = "Account created. You can sign in now.";
        return RedirectToAction(nameof(Login));
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        MockAuditLog.Add(User.Identity?.Name ?? "", "Signed out", "Signed out of AutoTrack.");
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [Authorize, HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public IActionResult ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var username = User.Identity?.Name ?? "";
        if (!MockUserStore.ChangePassword(username, model.OldPassword, model.NewPassword))
        {
            ModelState.AddModelError(nameof(model.OldPassword), "Current password is incorrect.");
            return View(model);
        }

        MockAuditLog.Add(username, "Password changed", "Updated account password.");
        TempData["Success"] = "Password updated.";
        return RedirectToAction("Welcome", "Home");
    }

    private async Task SignInAsync(MockUser user, bool remember)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new("FullName", user.FullName),
            new(ClaimTypes.Role, user.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = remember });
    }
}
