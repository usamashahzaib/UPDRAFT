using Microsoft.EntityFrameworkCore;
using UpdraftBackend.Domain;

namespace UpdraftBackend.Data;

public class UpdraftDbContext : DbContext
{
    public UpdraftDbContext(DbContextOptions<UpdraftDbContext> options) : base(options) { }

    public DbSet<PlayerAccount> PlayerAccounts => Set<PlayerAccount>();
    public DbSet<CreditLedgerEntry> CreditLedgerEntries => Set<CreditLedgerEntry>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<MatchParticipation> MatchParticipations => Set<MatchParticipation>();
    public DbSet<PurchaseTransaction> PurchaseTransactions => Set<PurchaseTransaction>();
    public DbSet<PlayerReport> PlayerReports => Set<PlayerReport>();
    public DbSet<AccountDeletionRequest> AccountDeletionRequests => Set<AccountDeletionRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Spec section 23: a source event can never grant the same ledger reward twice.
        modelBuilder.Entity<CreditLedgerEntry>()
            .HasIndex(e => new { e.Reason, e.SourceId })
            .IsUnique();

        modelBuilder.Entity<MatchParticipation>()
            .HasKey(m => new { m.MatchId, m.PlayerId });

        modelBuilder.Entity<PurchaseTransaction>()
            .HasIndex(p => p.GooglePurchaseToken)
            .IsUnique();
    }
}
