using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UpdraftBackend.Data;
using UpdraftBackend.Domain;
using Xunit;

namespace UpdraftBackend.Tests;

// Spec section 22/28: "A unique constraint must prevent the same source from
// granting the same reward twice." SQLite in-memory actually enforces unique
// indexes (unlike EF's InMemory provider), so this is a real regression test
// for that guarantee.
public class CreditLedgerTests
{
    static UpdraftDbContext NewContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<UpdraftDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new UpdraftDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public void DuplicateSourceAndReason_IsRejectedByUniqueConstraint()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using var context = NewContext(connection);

        var playerId = Guid.NewGuid();
        context.CreditLedgerEntries.Add(new CreditLedgerEntry
        {
            PlayerId = playerId,
            ChangeAmount = 20,
            Reason = "match_completion",
            SourceId = "match-123",
        });
        context.SaveChanges();

        context.CreditLedgerEntries.Add(new CreditLedgerEntry
        {
            PlayerId = playerId,
            ChangeAmount = 20,
            Reason = "match_completion",
            SourceId = "match-123", // same source + reason as above
        });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }

    [Fact]
    public void SameSource_DifferentReason_IsAllowed()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using var context = NewContext(connection);

        var playerId = Guid.NewGuid();
        context.CreditLedgerEntries.Add(new CreditLedgerEntry
        {
            PlayerId = playerId,
            ChangeAmount = 20,
            Reason = "match_completion",
            SourceId = "match-123",
        });
        context.CreditLedgerEntries.Add(new CreditLedgerEntry
        {
            PlayerId = playerId,
            ChangeAmount = 5,
            Reason = "win_bonus",
            SourceId = "match-123", // same match, different reward reason — allowed
        });

        context.SaveChanges();

        Assert.Equal(2, context.CreditLedgerEntries.Count());
    }
}
