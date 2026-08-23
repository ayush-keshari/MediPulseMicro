using InventoryService.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Data;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<Item> Items { get; set; }
    public DbSet<InventoryPosition> InventoryPositions { get; set; }
    public DbSet<ExceptionEvent> ExceptionEvents { get; set; }
    public DbSet<RecallAction> RecallActions { get; set; }
    public DbSet<Forecast> Forecasts { get; set; }
    public DbSet<ReplenishmentPlan> ReplenishmentPlans { get; set; }

    // Read-only cross-service view — owned by LogisticsService, same DB
    public DbSet<ConsumptionSummary> ConsumptionRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // ── Item ──────────────────────────────────────────────────────────
        mb.Entity<Item>()
            .HasIndex(i => i.ItemCode)
            .IsUnique();

        // ── InventoryPosition ─────────────────────────────────────────────
        mb.Entity<InventoryPosition>()
            .HasOne(p => p.Item)
            .WithMany(i => i.Positions)
            .HasForeignKey(p => p.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── ExceptionEvent ────────────────────────────────────────────────
        mb.Entity<ExceptionEvent>(entity =>
        {
            entity.ToTable("ExceptionEvent");
            entity.HasKey(e => e.ExceptionId);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ReferenceType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ItemName).HasMaxLength(150);
            entity.Property(e => e.LotId).HasMaxLength(50);
            entity.Property(e => e.Severity).IsRequired().HasMaxLength(20).HasDefaultValue("Medium");
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Open");
            entity.Property(e => e.DetectedDate).IsRequired().HasColumnType("datetime2");
            entity.HasIndex(e => new { e.Type, e.Status });
        });

        // ── RecallAction ──────────────────────────────────────────────────
        mb.Entity<RecallAction>(entity =>
        {
            entity.ToTable("RecallAction");
            entity.HasKey(e => e.RecallActionId);
            entity.Property(e => e.OwnerId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ActionDescription).IsRequired().HasMaxLength(500);
            entity.Property(e => e.DueDate).IsRequired().HasColumnType("datetime2");
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Pending");

            entity.HasOne(a => a.Exception)
                  .WithMany(e => e.Actions)
                  .HasForeignKey(a => a.ExceptionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Forecast ──────────────────────────────────────────────────────
        mb.Entity<Forecast>(entity =>
        {
            entity.ToTable("Forecast");
            entity.HasKey(e => e.ForecastId);
            entity.Property(e => e.Period).IsRequired().HasMaxLength(10);
            entity.Property(e => e.GeneratedDate).IsRequired().HasColumnType("datetime2");
            entity.HasIndex(e => new { e.FacilityId, e.ItemId, e.Period });
        });

        // ── ReplenishmentPlan ─────────────────────────────────────────────
        mb.Entity<ReplenishmentPlan>(entity =>
        {
            entity.ToTable("ReplenishmentPlan");
            entity.HasKey(e => e.PlanId);
            entity.Property(e => e.Priority).IsRequired().HasMaxLength(20).HasDefaultValue("Medium");
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Pending");
            entity.Property(e => e.GeneratedDate).IsRequired().HasColumnType("datetime2");
            entity.HasIndex(e => new { e.FacilityId, e.Status });
        });

        // ── ConsumptionSummary (read-only, no migration) ──────────────────
        mb.Entity<ConsumptionSummary>(entity =>
        {
            entity.HasNoKey();
            entity.ToTable("ConsumptionRecord", t => t.ExcludeFromMigrations());
        });
    }
}
