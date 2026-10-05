using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;
using HotdropBackend.Data;

namespace HotdropBackend.Tests;

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>();

        if (Request.Headers.TryGetValue("X-Test-PlayerId", out var playerHeader) &&
            Guid.TryParse(playerHeader.FirstOrDefault(), out var playerId))
        {
            claims.Add(new Claim("player_id", playerId.ToString()));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, playerId.ToString()));
        }

        if (Request.Headers.TryGetValue("X-Test-Service", out var serviceHeader) &&
            serviceHeader.FirstOrDefault() == "game-server")
        {
            claims.Add(new Claim("service", "game-server"));
            claims.Add(new Claim("server_id", "test-server"));
        }

        if (claims.Count == 0)
            return Task.FromResult(AuthenticateResult.NoResult());

        var identity = new ClaimsIdentity(claims, "Test");
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public class HotdropWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestServerKey = "test-only-game-server-key";
    readonly SqliteConnection _connection = new("DataSource=:memory:");

    public HotdropWebApplicationFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Features:PurchasesEnabled", "true");
        builder.UseSetting("GameServer:ApiKey", TestServerKey);
        builder.ConfigureTestServices(services =>
        {
            // Keep database failures visible in the test output. The Windows Event
            // Log provider requires machine-level permissions in restricted test
            // runners and can otherwise mask the original EF exception.
            services.AddLogging(logging => logging.ClearProviders());
            services.RemoveAll<IDbContextOptionsConfiguration<HotdropDbContext>>();
            services.RemoveAll<DbContextOptions<HotdropDbContext>>();
            services.RemoveAll<HotdropDbContext>();
            services.AddDbContext<HotdropDbContext>(options => options.UseSqlite(_connection));

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<HotdropDbContext>().Database.EnsureCreated();
        return host;
    }

    public HttpClient CreateClientWithPlayerId(Guid playerId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-PlayerId", playerId.ToString());
        return client;
    }

    public HttpClient CreateGameServerClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Service", "game-server");
        return client;
    }

    public HttpClient CreateUnauthenticatedClient() => CreateClient();

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}




