using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HotdropBackend.Data;

// Lets `dotnet ef migrations` run against a Minimal API project without
// spinning up the full host — points at local dev Postgres by convention.
// Never used at runtime; the real app configures this via appsettings.json.
public class HotdropDbContextFactory : IDesignTimeDbContextFactory<HotdropDbContext>
{
    public HotdropDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<HotdropDbContext>()
            .UseNpgsql("Host=localhost;Database=hotdrop;Username=hotdrop;Password=CHANGE_ME_LOCAL_DEV_ONLY")
            .Options;
        return new HotdropDbContext(options);
    }
}
