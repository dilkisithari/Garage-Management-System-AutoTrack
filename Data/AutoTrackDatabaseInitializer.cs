using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AutoTrack.Data;

public static partial class AutoTrackDatabaseInitializer
{
    private static readonly string[] Roles = { "Owner", "FrontDesk", "Mechanic", "Customer" };

    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AutoTrackDbContext>();
        await db.Database.MigrateAsync();

        var scriptPath = Path.Combine(environment.ContentRootPath, "Database", "Option3AuditTriggers.sql");
        var script = await File.ReadAllTextAsync(scriptPath);
        foreach (var batch in GoBatchRegex().Split(script).Where(batch => !string.IsNullOrWhiteSpace(batch)))
            await db.Database.ExecuteSqlRawAsync(batch);

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                EnsureSucceeded(result, $"Could not create role '{role}'.");
            }
        }

        var email = configuration["BootstrapOwner:Email"];
        var password = configuration["BootstrapOwner:Password"];
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password))
            return;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Set both BootstrapOwner:Email and BootstrapOwner:Password to provision an owner.");

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AutoTrackUser>>();
        var existingOwner = await userManager.FindByEmailAsync(email);
        if (existingOwner != null)
        {
            if (!await userManager.IsInRoleAsync(existingOwner, "Owner"))
            {
                EnsureSucceeded(await userManager.AddToRoleAsync(existingOwner, "Owner"), "Could not assign the Owner role.");
                EnsureSucceeded(await userManager.UpdateSecurityStampAsync(existingOwner), "Could not refresh the Owner security stamp.");
            }
            return;
        }

        var owner = new AutoTrackUser
        {
            UserName = email,
            Email = email,
            FullName = configuration["BootstrapOwner:FullName"] ?? "AutoTrack Owner",
            EmailConfirmed = true
        };
        EnsureSucceeded(await userManager.CreateAsync(owner, password), "Could not create the bootstrap owner.");
        EnsureSucceeded(await userManager.AddToRoleAsync(owner, "Owner"), "Could not assign the Owner role.");
    }

    private static void EnsureSucceeded(IdentityResult result, string message)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException($"{message} {string.Join("; ", result.Errors.Select(error => error.Description))}");
    }

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex GoBatchRegex();
}
