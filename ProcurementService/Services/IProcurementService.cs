using ProcurementService.DTOs;

namespace ProcurementService.Services;

// ProcurementService owns: Supplier, PurchaseOrder, Receipt.
// All EF navigation is local — no cross-service HTTP calls needed.
public interface IProcurementService
{
    // ── Suppliers ─────────────────────────────────────────────────────────
    Task<IEnumerable<SupplierDto>> GetAllSuppliersAsync();
    Task<SupplierDto?> GetSupplierByIdAsync(int id);
    Task<bool> CreateSupplierAsync(CreateSupplierRequest request);
    Task<bool> UpdateSupplierAsync(int id, UpdateSupplierRequest request);

    // Delete blocked when supplier has associated POs (DB-level Restrict)
    Task<bool> DeleteSupplierAsync(int id);

    // ── PurchaseOrders ────────────────────────────────────────────────────
    Task<IEnumerable<PurchaseOrderDto>> GetAllPurchaseOrdersAsync();
    Task<IEnumerable<PurchaseOrderDto>> GetPurchaseOrdersBySupplierAsync(int supplierId);
    Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(int id);
    Task<bool> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request);
    Task<bool> UpdatePurchaseOrderAsync(int id, UpdatePurchaseOrderRequest request);
    Task<bool> UpdatePoStatusAsync(int id, UpdatePoStatusRequest request);

    // Hard-delete: only Draft or Cancelled orders
    Task<bool> DeletePurchaseOrderAsync(int id);

    // ── Receipts (GRNs) ───────────────────────────────────────────────────
    Task<IEnumerable<ReceiptDto>> GetAllReceiptsAsync();
    Task<IEnumerable<ReceiptDto>> GetReceiptsByPoAsync(int poId);
    Task<ReceiptDto?> GetReceiptByIdAsync(int id);
    Task<bool> CreateReceiptAsync(CreateReceiptRequest request);
    Task<bool> UpdateReceiptAsync(int id, UpdateReceiptRequest request);
    Task<bool> DeleteReceiptAsync(int id);
}
