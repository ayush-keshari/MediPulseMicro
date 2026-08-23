using LogisticsService.Models;
using Microsoft.EntityFrameworkCore;

namespace LogisticsService.Data;

public class LogisticsDbContext : DbContext
{
    public LogisticsDbContext(DbContextOptions<LogisticsDbContext> options) : base(options) { }

    public DbSet<TransferOrder> TransferOrders => Set<TransferOrder>();
    public DbSet<TransferOrderItem> TransferOrderItems => Set<TransferOrderItem>();
    public DbSet<ConsumptionRecord> ConsumptionRecords => Set<ConsumptionRecord>();

    // Cross-service: same DB as InventoryService — exclude from migrations so EF
    // never tries to create/drop this table; we only read and update it.
    public DbSet<InventoryPosition> InventoryPositions => Set<InventoryPosition>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // ── TransferOrder ─────────────────────────────────────────────────
        mb.Entity<TransferOrder>(entity =>
        {
            entity.ToTable("TransferOrder");
            entity.HasKey(e => e.TransferOrderId);
            entity.Property(e => e.FromFacilityName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ToFacilityName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.RequestedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.RequestedDate).IsRequired().HasColumnType("datetime2");
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50).HasDefaultValue("Draft");
        });

        // ── TransferOrderItem ─────────────────────────────────────────────
        mb.Entity<TransferOrderItem>(entity =>
        {
            entity.ToTable("TransferOrderItem");
            entity.HasKey(e => e.TransferOrderItemId);
            entity.Property(e => e.ItemName).IsRequired().HasMaxLength(150);

            entity.HasOne(i => i.TransferOrder)
                  .WithMany(t => t.Items)
                  .HasForeignKey(i => i.TransferOrderId)
                  .HasConstraintName("FK_TransferOrderItem_TransferOrder")
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── ConsumptionRecord ─────────────────────────────────────────────
        mb.Entity<ConsumptionRecord>(entity =>
        {
            entity.ToTable("ConsumptionRecord");
            entity.HasKey(e => e.ConsumptionId);
            entity.Property(e => e.ItemName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.ConsumedDate).IsRequired().HasColumnType("datetime2");
            entity.Property(e => e.ConsumedBy).IsRequired().HasMaxLength(100);
        });

        // ── InventoryPositions (cross-service, no migration) ──────────────
        mb.Entity<InventoryPosition>(entity =>
        {
            entity.ToTable("InventoryPositions", t => t.ExcludeFromMigrations());
            entity.HasKey(e => e.PositionId);
            // Explicit mapping so EF always includes these NOT NULL columns in
            // INSERT statements — without this, convention treats string as nullable
            // and may omit the column, causing a SQL Server NOT NULL violation.
            entity.Property(e => e.LotId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.StorageZoneId).IsRequired();
        });
    }
}
