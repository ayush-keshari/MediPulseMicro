using Microsoft.EntityFrameworkCore;
using TelemetryService.Models;

namespace TelemetryService.Data;

// TelemetryService owns two tables: SensorDevice and TelemetryRecord.
// All FK constraints are enforced by EF Core within this single database.
//
//   SensorDevice ──< TelemetryRecord
//   (1)               (many)
//
// AssignedEntityId is a plain int — no EF navigation to FacilityService or LogisticsService.
public class TelemetryDbContext : DbContext
{
    public TelemetryDbContext(DbContextOptions<TelemetryDbContext> options) : base(options) { }

    public DbSet<SensorDevice> SensorDevices => Set<SensorDevice>();
    public DbSet<TelemetryRecord> TelemetryRecords => Set<TelemetryRecord>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // ── SensorDevice ──────────────────────────────────────────────────
        mb.Entity<SensorDevice>(entity =>
        {
            entity.ToTable("SensorDevice");
            entity.HasKey(e => e.SensorId);
            entity.Property(e => e.SensorId).HasColumnName("SensorID");
            entity.Property(e => e.DeviceType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.AssignedTo).IsRequired().HasMaxLength(50);
            entity.Property(e => e.AssignedEntityId).IsRequired(false);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50).HasDefaultValue("Active");
        });

        // ── TelemetryRecord ───────────────────────────────────────────────
        mb.Entity<TelemetryRecord>(entity =>
        {
            entity.ToTable("TelemetryRecord");
            entity.HasKey(e => e.TelemetryId);
            entity.Property(e => e.TelemetryId).HasColumnName("TelemetryID");
            entity.Property(e => e.SensorId).HasColumnName("SensorID").IsRequired();
            entity.Property(e => e.Timestamp).IsRequired().HasColumnType("datetime2");
            entity.Property(e => e.Temperature).HasColumnType("decimal(5,2)");
            entity.Property(e => e.Humidity).HasColumnType("decimal(5,2)");
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.IsExcursion).IsRequired().HasDefaultValue(false);

            // SensorDevice -> TelemetryRecords: Cascade delete
            entity.HasOne(t => t.SensorDevice)
                  .WithMany(s => s.TelemetryRecords)
                  .HasForeignKey(t => t.SensorId)
                  .HasConstraintName("FK_TelemetryRecord_SensorDevice")
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
