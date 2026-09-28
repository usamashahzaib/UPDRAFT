using Microsoft.EntityFrameworkCore;
using UpdraftBackend.Domain;

namespace UpdraftBackend.Data;

public class UpdraftDbContext : DbContext
{
    public UpdraftDbContext(DbContextOptions<UpdraftDbContext> options) : base(options) { }

    public DbSet<PlayerAccount> PlayerAccounts => Set<PlayerAccount>();
    public DbSet<PlayerSession> PlayerSessions => Set<PlayerSession>();
    public DbSet<Party> Parties => Set<Party>();
    public DbSet<PartyMember> PartyMembers => Set<PartyMember>();
    public DbSet<CreditLedgerEntry> CreditLedgerEntries => Set<CreditLedgerEntry>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<MatchParticipation> MatchParticipations => Set<MatchParticipation>();
    public DbSet<PurchaseTransaction> PurchaseTransactions => Set<PurchaseTransaction>();
    public DbSet<PlayerReport> PlayerReports => Set<PlayerReport>();
    public DbSet<AccountDeletionRequest> AccountDeletionRequests => Set<AccountDeletionRequest>();
    public DbSet<AnalyticsEvent> AnalyticsEvents => Set<AnalyticsEvent>();
    public DbSet<AnalyticsDailyAggregate> AnalyticsDailyAggregates => Set<AnalyticsDailyAggregate>();
    public DbSet<MatchmakingTicket> MatchmakingTickets => Set<MatchmakingTicket>();
    public DbSet<MatchAllocation> MatchAllocations => Set<MatchAllocation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // SQLite has no native DateTimeOffset ordering. Store UTC ticks for the
        // test provider; PostgreSQL keeps its native timestamptz mapping.
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            var utcTicks = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTimeOffset, long>(
                value => value.UtcTicks, value => new DateTimeOffset(value, TimeSpan.Zero));
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
                foreach (var property in entity.GetProperties())
                    if (property.ClrType == typeof(DateTimeOffset) || property.ClrType == typeof(DateTimeOffset?))
                        property.SetValueConverter(utcTicks);
        }
        // Spec section 23: a source event can never grant the same ledger reward twice.
        modelBuilder.Entity<CreditLedgerEntry>()
            .HasIndex(e => new { e.PlayerId, e.Reason, e.SourceId })
            .IsUnique();

        modelBuilder.Entity<PlayerSession>()
            .HasIndex(s => s.RefreshTokenHash)
            .IsUnique();

        modelBuilder.Entity<PlayerSession>()
            .HasIndex(s => new { s.PlayerId, s.ExpiresAt });

        modelBuilder.Entity<Party>()
            .HasIndex(p => p.Code)
            .IsUnique();

        modelBuilder.Entity<PartyMember>()
            .HasKey(m => new { m.PartyId, m.PlayerId });

        modelBuilder.Entity<PartyMember>()
            .HasIndex(m => m.PlayerId)
            .IsUnique();

        modelBuilder.Entity<MatchParticipation>()
            .HasKey(m => new { m.MatchId, m.PlayerId });

        modelBuilder.Entity<PurchaseTransaction>()
            .HasIndex(p => p.GooglePurchaseToken)
            .IsUnique();

        modelBuilder.Entity<AnalyticsEvent>()
            .HasIndex(e => e.Timestamp);

        modelBuilder.Entity<AnalyticsDailyAggregate>()
            .HasIndex(e => new { e.EventDate, e.EventName, e.Region, e.DeviceTier })
            .IsUnique();

        modelBuilder.Entity<InventoryItem>()
            .HasIndex(i => new { i.PlayerId, i.CosmeticId })
            .IsUnique();

        modelBuilder.Entity<MatchmakingTicket>()
            .HasIndex(t => t.Status);
        modelBuilder.Entity<MatchmakingTicket>()
            .HasIndex(t => t.PlayerId);
        modelBuilder.Entity<MatchmakingTicket>()
            .HasIndex(t => t.PartyId);
        modelBuilder.Entity<MatchAllocation>()
            .HasKey(a => a.MatchId);
        modelBuilder.Entity<MatchAllocation>()
            .HasIndex(a => a.ServerId);
    }
}
