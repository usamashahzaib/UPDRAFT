namespace UpdraftBackend.Endpoints;

public static class MatchmakingEndpoints
{
    // Spec section 14: one public queue, one initial region. The real queue
    // evaluator (2s interval, rating-window widening, latency preference) needs
    // a background worker; this exposes the API shape now so the client can be
    // built against it while that worker is implemented.
    public static void MapMatchmakingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/matchmaking").RequireAuthorization();

        group.MapPost("/enqueue", (HttpContext ctx) =>
        {
            var playerId = ctx.User.FindFirst("player_id")!.Value;
            // TODO: enqueue into the real matching worker (rating window, latency, party composition rules).
            return Results.Ok(new { queued = true, playerId });
        });

        group.MapPost("/cancel", (HttpContext ctx) => Results.Ok(new { cancelled = true }));

        group.MapGet("/status", (HttpContext ctx) =>
            Results.Ok(new { state = "searching", elapsedSeconds = 0 }));

        group.MapGet("/ticket", (HttpContext ctx) =>
            // Spec section 22: short-lived, account-bound match join tickets.
            Results.Ok(new { ticket = (string?)null, note = "no reservation yet" }));
    }
}
