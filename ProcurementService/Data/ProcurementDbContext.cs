using Microsoft.EntityFrameworkCore;
using ProcurementService.Models;

namespace ProcurementService.Data;

// ProcurementService owns three tables: Supplier, PurchaseOrder, Receipt.
// All relationships are enforced by EF Core — everything lives in the same DB.
//
//   Supplier  ──< PurchaseOrder ──< Receipt
//   (1)             (many)           (many)
//
// FacilityService has NO reference to Supplier — confirmed by the service boundary
// diagram: facility-service covers Facilities + StorageZones only.
public class ProcurementDbContext : DbContext
{
    public ProcurementDbContext(DbContextOptions<ProcurementDbContext> options) : base(options) { }

    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<Receipt> Receipts => Set<Receipt>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // ── Supplier ──────────────────────────────────────────────────────
        mb.Entity<Supplier>(entity =>
        {
            entity.ToTable("Supplier");
            entity.HasKey(e => e.SupplierId);
            entity.Property(e => e.SupplierId).HasColumnName("SupplierID");
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SupplierType).HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50).HasDefaultValue("Active");
        });

        // ── PurchaseOrder ─────────────────────────────────────────────────
        mb.Entity<PurchaseOrder>(entity =>
        {
            entity.ToTable("PurchaseOrder");
            entity.HasKey(e => e.PoId);
            entity.Property(e => e.PoId).HasColumnName("POID");
            entity.Property(e => e.SupplierId).HasColumnName("SupplierID");
            entity.Property(e => e.OrderDate).IsRequired().HasColumnType("datetime2");
            entity.Property(e => e.ExpectedDeliveryDate).HasColumnType("datetime2");
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50).HasDefaultValue("Draft");
            entity.Property(e => e.Notes).HasMaxLength(500);

            // Supplier -> PurchaseOrders: Restrict delete (cannot delete supplier
            // while active POs exist -- enforced at DB level)
            entity.HasOne(po => po.Supplier)
                  .WithMany(s => s.PurchaseOrders)
                  .HasForeignKey(po => po.SupplierId)
                  .HasConstraintName("FK_PurchaseOrder_Supplier")
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Receipt ───────────────────────────────────────────────────────
        mb.Entity<Receipt>(entity =>
        {
            entity.ToTable("Receipt");
            entity.HasKey(e => e.ReceiptId);
            entity.Property(e => e.ReceiptId).HasColumnName("ReceiptID");
            entity.Property(e => e.PoId).HasColumnName("POID");
            entity.Property(e => e.SupplierLot).HasMaxLength(100);
            entity.Property(e => e.ReceivedDate).IsRequired().HasColumnType("datetime2");
            entity.Property(e => e.ReceivedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.QualityStatus).IsRequired().HasMaxLength(50).HasDefaultValue("Accepted");
            entity.Property(e => e.QuantityReceived).IsRequired();

            // PurchaseOrder -> Receipts: Cascade delete
            entity.HasOne(r => r.PurchaseOrder)
                  .WithMany(po => po.Receipts)
                  .HasForeignKey(r => r.PoId)
                  .HasConstraintName("FK_Receipt_PurchaseOrder")
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
