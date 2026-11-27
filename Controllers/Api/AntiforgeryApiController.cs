using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Antiforgery;

namespace AutoTrack.Controllers.Api;

[ApiController]
[Route("api/antiforgery-token")]
public class AntiforgeryApiController(IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet]
    public IActionResult GetToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }
}
