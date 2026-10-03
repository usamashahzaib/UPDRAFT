using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HotdropBackend.Data;
using HotdropBackend.Domain;
using HotdropBackend.Services;
using Xunit;

namespace HotdropBackend.Tests;

/// <summary>
/// Gate B / spec sections 12, 21, 22: offline practice grants no rating, XP or credits.
/// The only path to progression is a result submitted by the authenticated game server,
/// so a player (e.g. one reporting a practice win) cannot mint anything.
/// </summary>
public class OfflinePracticeGrantsNothingTests : IClassFixture<HotdropWebApplicationFactory>
{
    readonly HotdropWebApplicationFactory _factory;
    public OfflinePracticeGrantsNothingTests(HotdropWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task PlayerSubmittedResult_IsForbidden_AndChangesNothing()
    {
        var id = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HotdropDbContext>();
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
        var verify = check.ServiceProvider.GetRequiredService<HotdropDbContext>();
        var account = await verify.PlayerAccounts.AsNoTracking().SingleAsync(a => a.Id == id);
        Assert.Equal(0, account.ProfileXp);
        Assert.Equal(1000, account.HiddenMatchmakingRating);
        Assert.Equal(0, await verify.CreditLedgerEntries.CountAsync(e => e.PlayerId == id));
    }
}




