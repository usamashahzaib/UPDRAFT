using System.Text.Json;
using UpdraftBackend.Data;
using UpdraftBackend.Domain;

namespace UpdraftBackend.Endpoints;

public static class AnalyticsEndpoints
{
    static readonly HashSet<string> ClientEvents =
    [
        "first_open", "tutorial_started", "tutorial_step", "tutorial_completed",
        "queue_started", "queue_cancelled", "weapon_selected", "results_play_again_clicked",
        "purchase_started", "client_error",
    ];

    static readonly HashSet<string> ServerEvents =
    [
        "match_reserved", "match_started", "match_completed", "disconnect", "reconnect",
        "pad_placed", "pad_used", "pad_destroyed", "purchase_verified",
    ];

    public static void MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/analytics").RequireAuthorization();
        group.MapPost("/events", (AnalyticsEventRequest request, HttpContext ctx, UpdraftDbContext db) =>
            Record(request, "client", Guid.Parse(ctx.User.FindFirst("player_id")!.Value), ClientEvents, db));

        // The game client uploads its local queue in batches of up to 50 events.
        group.MapPost("/events/batch", async (List<ClientQueuedEvent> events, HttpContext ctx, UpdraftDbContext db) =>
        {
            if (events is null || events.Count is 0 or > 50) return Results.BadRequest(new { error = "batch_size" });
            var playerId = Guid.Parse(ctx.User.FindFirst("player_id")!.Value);
            int accepted = 0, rejected = 0;
            foreach (var e in events)
            {
                var request = new AnalyticsEventRequest(e.EventName ?? "", e.SchemaVersion, e.BuildVersion ?? "", e.BalanceVersion ?? "",
                    e.Region ?? "", e.DeviceTier ?? "", Math.Clamp(e.PartySize, 1, 3), e.HasBots, e.Cohort ?? "",
                    Guid.TryParse(e.MatchId, out var m) ? m : null, null, e.Payload);
                var result = Validate(request, ClientEvents);
                if (result is not null) { rejected++; continue; }
                db.AnalyticsEvents.Add(ToEntity(request, "client", playerId));
                accepted++;
            }
            await db.SaveChangesAsync();
            // Rejected events are acknowledged so a client never retries an invalid event forever.
            return Results.Accepted(value: new { accepted, rejected });
        });

        group.MapPost("/server-events", (AnalyticsEventRequest request, UpdraftDbContext db) =>
            Record(request, "server", request.PlayerId, ServerEvents, db))
            .RequireAuthorization(policy => policy.RequireClaim("service", "game-server"));
    }

    static string? Validate(AnalyticsEventRequest request, HashSet<string> allowedEvents)
    {
        if (!allowedEvents.Contains(request.EventName)) return "unsupported_event";
        if (request.SchemaVersion < 1 || request.PartySize is < 1 or > 3 ||
            string.IsNullOrWhiteSpace(request.BuildVersion) || string.IsNullOrWhiteSpace(request.BalanceVersion) ||
            string.IsNullOrWhiteSpace(request.Region) || string.IsNullOrWhiteSpace(request.DeviceTier))
            return "invalid_event_dimensions";
        if (PayloadText(request).Length > 4096) return "payload_too_large";
        return null;
    }

    static string PayloadText(AnalyticsEventRequest request) =>
        request.Payload.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null ? "{}" : request.Payload.GetRawText();

    static async Task<IResult> Record(AnalyticsEventRequest request, string source, Guid? playerId,
        HashSet<string> allowedEvents, UpdraftDbContext db)
    {
        var error = Validate(request, allowedEvents);
        if (error is not null) return Results.BadRequest(new { error });
        db.AnalyticsEvents.Add(ToEntity(request, source, playerId));
        await db.SaveChangesAsync();
        return Results.Accepted(value: new { recorded = true });
    }

    static AnalyticsEvent ToEntity(AnalyticsEventRequest request, string source, Guid? playerId)
    {
        var payload = PayloadText(request);
        return new AnalyticsEvent
        {
            EventName = request.EventName,
            SchemaVersion = request.SchemaVersion,
            Source = source,
            PlayerId = playerId,
            MatchId = request.MatchId,
            BuildVersion = request.BuildVersion,
            BalanceVersion = request.BalanceVersion,
            Region = request.Region,
            DeviceTier = request.DeviceTier,
            PartySize = request.PartySize,
            HasBots = request.HasBots,
            Cohort = request.Cohort,
            Timestamp = DateTimeOffset.UtcNow,
            PayloadJson = payload,
        };
    }

    /// <summary>The game client's local queue line format (see Unity Telemetry.cs).</summary>
    public sealed record ClientQueuedEvent(
        string? EventName, int SchemaVersion, string? Source, string? BuildVersion, string? BalanceVersion,
        string? PseudonymousId, string? MatchId, string? Region, string? DeviceTier, int PartySize, bool HasBots,
        string? Cohort, string? Timestamp, JsonElement Payload);

    public sealed record AnalyticsEventRequest(
        string EventName, int SchemaVersion, string BuildVersion, string BalanceVersion,
        string Region, string DeviceTier, int PartySize, bool HasBots, string Cohort,
        Guid? MatchId, Guid? PlayerId, JsonElement Payload);
}
