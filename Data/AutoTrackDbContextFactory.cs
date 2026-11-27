using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AutoTrack.Data;

public class AutoTrackDbContextFactory : IDesignTimeDbContextFactory<AutoTrackDbContext>
{
    public AutoTrackDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AutoTrackDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=AutoTrack;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new AutoTrackDbContext(options);
    }
}
