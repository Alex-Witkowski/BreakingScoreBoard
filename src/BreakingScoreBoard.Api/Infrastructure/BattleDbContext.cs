using BreakingScoreBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BreakingScoreBoard.Api.Infrastructure;

/// <summary>
/// Entity Framework Core database context for the Battle application.
/// </summary>
public class BattleDbContext : DbContext
{
    public BattleDbContext(DbContextOptions<BattleDbContext> options) : base(options)
    {
    }

    public DbSet<BattleEvent> BattleEvents => Set<BattleEvent>();
    public DbSet<AgeCategory> AgeCategories => Set<AgeCategory>();
    public DbSet<Breaker> Breakers => Set<Breaker>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<Battle> Battles => Set<Battle>();
    public DbSet<JudgeScore> JudgeScores => Set<JudgeScore>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // BattleEvent configuration
        modelBuilder.Entity<BattleEvent>(entity =>
        {
            entity.ToTable("BattleEvents");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Location)
                .HasMaxLength(500);

            entity.Property(e => e.AdminPinHash)
                .IsRequired()
                .HasMaxLength(64); // SHA256 hex string

            entity.Property(e => e.JudgePinHash)
                .IsRequired()
                .HasMaxLength(64);

            entity.HasMany(e => e.Categories)
                .WithOne(c => c.Event)
                .HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AgeCategory configuration
        modelBuilder.Entity<AgeCategory>(entity =>
        {
            entity.ToTable("AgeCategories");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(e => e.EventId)
                .HasDatabaseName("IX_AgeCategories_EventId");

            entity.HasIndex(e => new { e.EventId, e.Name })
                .IsUnique()
                .HasDatabaseName("UX_AgeCategories_EventId_Name");

            entity.HasMany(e => e.Registrations)
                .WithOne(r => r.Category)
                .HasForeignKey(r => r.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Battles)
                .WithOne(b => b.Category)
                .HasForeignKey(b => b.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Breaker configuration
        modelBuilder.Entity<Breaker>(entity =>
        {
            entity.ToTable("Breakers");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasMany(e => e.Registrations)
                .WithOne(r => r.Breaker)
                .HasForeignKey(r => r.BreakerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Registration configuration
        modelBuilder.Entity<Registration>(entity =>
        {
            entity.ToTable("Registrations");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.BreakerId, e.CategoryId })
                .IsUnique()
                .HasDatabaseName("UX_Registrations_BreakerId_CategoryId");

            entity.HasIndex(e => e.CategoryId)
                .HasDatabaseName("IX_Registrations_CategoryId");
        });

        // Battle configuration
        modelBuilder.Entity<Battle>(entity =>
        {
            entity.ToTable("Battles");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.CategoryId)
                .HasDatabaseName("IX_Battles_CategoryId");

            entity.HasIndex(e => new { e.CategoryId, e.BracketLevel })
                .HasDatabaseName("IX_Battles_CategoryId_BracketLevel");

            entity.HasOne(e => e.Breaker1)
                .WithMany()
                .HasForeignKey(e => e.Breaker1Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Breaker2)
                .WithMany()
                .HasForeignKey(e => e.Breaker2Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Winner)
                .WithMany()
                .HasForeignKey(e => e.WinnerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.OriginalBattle)
                .WithMany()
                .HasForeignKey(e => e.OriginalBattleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Scores)
                .WithOne(s => s.Battle)
                .HasForeignKey(s => s.BattleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // JudgeScore configuration
        modelBuilder.Entity<JudgeScore>(entity =>
        {
            entity.ToTable("JudgeScores");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.JudgeIdentifier)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(e => e.BattleId)
                .HasDatabaseName("IX_JudgeScores_BattleId");

            entity.HasIndex(e => new { e.BattleId, e.JudgeIdentifier, e.BreakerId })
                .HasDatabaseName("IX_JudgeScores_BattleId_JudgeIdentifier_BreakerId");

            entity.HasOne(e => e.Breaker)
                .WithMany()
                .HasForeignKey(e => e.BreakerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
