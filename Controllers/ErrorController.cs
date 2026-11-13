using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoTrack.Controllers;

[AllowAnonymous]
public class ErrorController : Controller
{
    /// <summary>Shown when a signed-in role opens a page it is not allowed to see.</summary>
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    /// <summary>Used by UseStatusCodePagesWithReExecute: /Error/Status/404 ...</summary>
    public IActionResult Status(int id)
    {
        Response.StatusCode = id;
        return id switch
        {
            404 => View("NotFound"),
            401 or 403 => View("AccessDenied"),
            _ => View("Generic")
        };
    }
}
