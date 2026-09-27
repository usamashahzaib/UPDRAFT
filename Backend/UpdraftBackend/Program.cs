using Microsoft.EntityFrameworkCore;
using UpdraftBackend.Data;
using UpdraftBackend.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Spec section 21: one modular backend, not a microservice per feature.
var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? "Host=localhost;Database=updraft;Username=updraft;Password=updraft";

builder.Services.AddDbContext<UpdraftDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Spec section 23 API groups. Each maps to its own endpoint file; all share
// this one process/database rather than being split into microservices.
app.MapAuthEndpoints();
app.MapProfileEndpoints();
app.MapPartyEndpoints();
app.MapMatchmakingEndpoints();
app.MapMatchEndpoints();
app.MapCommerceEndpoints();
app.MapSupportEndpoints();

app.Run();
