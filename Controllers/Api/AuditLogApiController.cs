using AutoTrack.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoTrack.Controllers.Api;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = "Owner")]
public class AuditLogApiController(AutoTrackDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? user,
        [FromQuery] DateTime? date,
        [FromQuery] string? action)
    {
        var query = db.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(user))
            query = query.Where(log => log.ActorUsername == user);
        if (date.HasValue)
        {
            var start = date.Value.Date;
            var end = start.AddDays(1);
            query = query.Where(log => log.OccurredAt >= start && log.OccurredAt < end);
        }
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(log => log.Action == action.ToUpperInvariant());

        var entries = await query.OrderByDescending(log => log.OccurredAt)
            .Take(500)
            .Select(log => new
            {
                log.Id,
                User = log.ActorUsername,
                log.Action,
                log.TableName,
                log.OccurredAt,
                log.OldValues,
                log.NewValues
            })
            .ToListAsync();

        return Ok(entries);
    }
}
