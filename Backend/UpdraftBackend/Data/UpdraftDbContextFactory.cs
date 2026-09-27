using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UpdraftBackend.Data;

// Lets `dotnet ef migrations` run against a Minimal API project without
// spinning up the full host — points at local dev Postgres by convention.
// Never used at runtime; the real app configures this via appsettings.json.
public class UpdraftDbContextFactory : IDesignTimeDbContextFactory<UpdraftDbContext>
{
    public UpdraftDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<UpdraftDbContext>()
            .UseNpgsql("Host=localhost;Database=updraft;Username=updraft;Password=CHANGE_ME_LOCAL_DEV_ONLY")
            .Options;
        return new UpdraftDbContext(options);
    }
}
