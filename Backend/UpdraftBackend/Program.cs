using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
using UpdraftBackend.Data;
using UpdraftBackend.Endpoints;
using UpdraftBackend.Services;

var builder = WebApplication.CreateBuilder(args);

// Spec section 21: one modular backend, not a microservice per feature.
var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? "Host=localhost;Database=updraft;Username=updraft;Password=updraft";

builder.Services.AddDbContext<UpdraftDbContext>(options =>
    options.UseNpgsql(connectionString));

var signingKey = builder.Configuration["Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(signingKey))
{
    if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing"))
        throw new InvalidOperationException("Jwt:SigningKey is required outside Development/Testing and must be supplied through secret configuration.");
    signingKey = "UPDRAFT-DEVELOPMENT-ONLY-SIGNING-KEY-CHANGE-ME";
}

var jwtOptions = new JwtOptions(
    builder.Configuration["Jwt:Issuer"] ?? "project-updraft",
    builder.Configuration["Jwt:Audience"] ?? "project-updraft-client",
    signingKey,
    TimeSpan.FromMinutes(15),
    TimeSpan.FromDays(30));
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<AuthTokenService>();
builder.Services.Configure<GooglePlayGamesOptions>(builder.Configuration.GetSection("GooglePlayGames"));
builder.Services.AddHttpClient<IGooglePlayGamesIdentityVerifier, GooglePlayGamesIdentityVerifier>();

var matchmakingOptions = new MatchmakingOptions();
builder.Configuration.GetSection("Matchmaking").Bind(matchmakingOptions);
builder.Services.AddSingleton(matchmakingOptions);
builder.Services.AddSingleton<MatchmakingComposer>();

var progressionOptions = new ProgressionOptions();
builder.Configuration.GetSection("Progression").Bind(progressionOptions);
builder.Services.AddSingleton(progressionOptions);
builder.Services.AddSingleton<MatchResultProcessor>();

var allocatorOptions = new AllocatorOptions();
builder.Configuration.GetSection("Allocator").Bind(allocatorOptions);
builder.Services.AddSingleton(allocatorOptions);
builder.Services.AddSingleton<MatchTicketService>();
builder.Services.Configure<FeatureFlags>(builder.Configuration.GetSection("Features"));

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHostedService<AnalyticsRetentionService>();
    builder.Services.AddHostedService<MatchmakingWorker>();
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtOptions.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
    };
});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // Spec section 21: rate-limit every API, per authenticated player (or per IP before login).
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirst("player_id")?.Value ?? context.User.FindFirst("server_id")?.Value
                ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 300, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        }));
});

var app = builder.Build();

// Spec section 17 "update required" / section 31 client-server compatibility check.
int minClientProtocol = builder.Configuration.GetValue("Compatibility:MinClientProtocol", 1);
app.Use(async (context, next) =>
{
    if (context.Request.Headers.TryGetValue("X-Client-Protocol", out var value) &&
        int.TryParse(value, out int protocol) && protocol < minClientProtocol)
    {
        context.Response.StatusCode = StatusCodes.Status426UpgradeRequired;
        await context.Response.WriteAsJsonAsync(new { error = "update_required", minClientProtocol });
        return;
    }
    await next();
});

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapGet("/health/live", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/health/ready", async (UpdraftDbContext db, CancellationToken cancellationToken) =>
    await db.Database.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "ready" })
        : Results.Problem("Database is unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable));

// Spec section 23 API groups. Each maps to its own endpoint file; all share
// this one process/database rather than being split into microservices.
app.MapAuthEndpoints();
app.MapProfileEndpoints();
app.MapPartyEndpoints();
app.MapMatchmakingEndpoints();
app.MapMatchEndpoints();
app.MapCommerceEndpoints();
app.MapSupportEndpoints();
app.MapAnalyticsEndpoints();

app.Run();

public partial class Program;
