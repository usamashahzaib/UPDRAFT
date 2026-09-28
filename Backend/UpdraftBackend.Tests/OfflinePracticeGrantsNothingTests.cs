using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UpdraftBackend.Data;
using UpdraftBackend.Domain;
using UpdraftBackend.Services;
using Xunit;

namespace UpdraftBackend.Tests;

/// <summary>
/// Gate B / spec sections 12, 21, 22: offline practice grants no rating, XP or credits.
/// The only path to progression is a result submitted by the authenticated game server,
/// so a player (e.g. one reporting a practice win) cannot mint anything.
/// </summary>
public class OfflinePracticeGrantsNothingTests : IClassFixture<UpdraftWebApplicationFactory>
{
    readonly UpdraftWebApplicationFactory _factory;
    public OfflinePracticeGrantsNothingTests(UpdraftWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task PlayerSubmittedResult_IsForbidden_AndChangesNothing()
    {
        var id = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UpdraftDbContext>();
            db.PlayerAccounts.Add(new PlayerAccount { Id = id, HiddenMatchmakingRating = 1000, ProfileXp = 0, Level = 1 });
            await db.SaveChangesAsync();
        }

        var request = new SubmitResultRequest(false,
            [new ParticipantResult(id, true, true, false, "A")], HasBots: true);
        var asPlayer = await _factory.CreateClientWithPlayerId(id).PostAsJsonAsync($"/api/matches/{Guid.NewGuid()}/result", request);
        var anonymous = await _factory.CreateUnauthenticatedClient().PostAsJsonAsync($"/api/matches/{Guid.NewGuid()}/result", request);

        Assert.Equal(HttpStatusCode.Forbidden, asPlayer.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        using var check = _factory.Services.CreateScope();
        var verify = check.ServiceProvider.GetRequiredService<UpdraftDbContext>();
        var account = await verify.PlayerAccounts.AsNoTracking().SingleAsync(a => a.Id == id);
        Assert.Equal(0, account.ProfileXp);
        Assert.Equal(1000, account.HiddenMatchmakingRating);
        Assert.Equal(0, await verify.CreditLedgerEntries.CountAsync(e => e.PlayerId == id));
    }
}
