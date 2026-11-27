using AutoTrack.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoTrack.Middleware;

public class SqlAuditSessionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, AutoTrackDbContext db)
    {
        if (!RequiresSqlSession(context.Request.Path))
        {
            await next(context);
            return;
        }

        await db.Database.OpenConnectionAsync(context.RequestAborted);
        try
        {
            var actor = context.User.Identity?.Name ?? "Anonymous";
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"EXEC sys.sp_set_session_context @key=N'ActorUsername', @value={actor};",
                context.RequestAborted);
            await next(context);
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync(
                "EXEC sys.sp_set_session_context @key=N'ActorUsername', @value=NULL, @read_only=0;",
                CancellationToken.None);
            await db.Database.CloseConnectionAsync();
        }
    }

    private static bool RequiresSqlSession(PathString path) =>
        path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/Account", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/UserAdmin", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/CustomerPortal", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/FrontDesk", StringComparison.OrdinalIgnoreCase);
}
