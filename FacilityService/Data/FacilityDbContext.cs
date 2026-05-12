using FacilityService.Models;
using Microsoft.EntityFrameworkCore;

namespace FacilityService.Data;

public class FacilityDbContext : DbContext
{
    public FacilityDbContext(DbContextOptions<FacilityDbContext> options) : base(options) { }

    public DbSet<Facility> Facilities => Set<Facility>();
    public DbSet<StorageZone> StorageZones => Set<StorageZone>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Facility>(entity =>
        {
            entity.ToTable("Facility");
            entity.HasKey(e => e.FacilityId).HasName("PK__Facility__5FB08B94A6B7EFA8");
            entity.Property(e => e.FacilityId).HasColumnName("FacilityID");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Type).HasMaxLength(50);
            entity.Property(e => e.Region).HasMaxLength(100);
        });

        mb.Entity<StorageZone>(entity =>
        {
            entity.ToTable("StorageZone");
            entity.HasKey(e => e.ZoneId).HasName("PK__StorageZ__601667959C48E490");
            entity.Property(e => e.ZoneId).HasColumnName("ZoneID");
            entity.Property(e => e.FacilityId).HasColumnName("FacilityID");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.TemperatureProfile).HasMaxLength(50);
            entity.Property(e => e.Capacity).HasColumnType("decimal(18, 2)");

            entity.HasOne(z => z.Facility)
                  .WithMany(f => f.StorageZones)
                  .HasForeignKey(z => z.FacilityId)
                  .HasConstraintName("FK__StorageZo__Facil__52593CB8");
        });
    }
}
