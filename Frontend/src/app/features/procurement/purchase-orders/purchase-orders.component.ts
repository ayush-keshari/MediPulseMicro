import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, FormGroup, Validators, AbstractControl } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ProcurementService } from '../../../services/procurement/procurement.service';
import { PurchaseOrderDto, SupplierDto } from '../../../services/procurement/procurement.models';

@Component({
  selector: 'app-purchase-orders',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './purchase-orders.component.html',
  styleUrl: './purchase-orders.component.css',
})
export class PurchaseOrdersComponent implements OnInit {
  orders: PurchaseOrderDto[] = [];
  filtered: PurchaseOrderDto[] = [];
  suppliers: SupplierDto[] = [];
  isLoading = false;
  successMessage = ''; errorMessage = '';
  search = ''; filterStatus = '';

  showModal = false; isSaving = false; editId: number | null = null;
  form: FormGroup;

  showStatusModal = false; statusOrderId: number | null = null; statusForm: FormGroup;

  showDeleteConfirm = false; deleteTarget: PurchaseOrderDto | null = null; isDeleting = false;

  statuses = ['Draft','Submitted','Approved','Shipped','PartiallyReceived','FullyReceived','Cancelled'];

  constructor(private svc: ProcurementService, private fb: FormBuilder) {
    this.form = this.fb.group({
      supplierId:           [null, Validators.required],
      orderDate:            [this.today(), Validators.required],
      expectedDeliveryDate: [''],
      notes:                [''],
    }, { validators: this.deliveryAfterOrderValidator });
    this.statusForm = this.fb.group({ status: ['', Validators.required] });
  }

  ngOnInit() { this.loadAll(); }

  loadAll() {
    this.isLoading = true;
    this.svc.getPurchaseOrders().subscribe({
      next: (d) => { this.orders = d; this.applyFilter(); this.isLoading = false; },
      error: () => { this.errorMessage = 'Failed to load purchase orders.'; this.isLoading = false; },
    });
    this.svc.getSuppliers().subscribe({ next: (s) => this.suppliers = s });
  }

  applyFilter() {
    const q = this.search.toLowerCase();
    this.filtered = this.orders.filter(o =>
      (!q || o.supplierName.toLowerCase().includes(q) || String(o.poId).includes(q)) &&
      (!this.filterStatus || o.status === this.filterStatus)
    );
  }

  openAdd() { this.editId = null; this.form.reset({ orderDate: this.today() }); this.showModal = true; }
  openEdit(o: PurchaseOrderDto) {
    this.editId = o.poId;
    this.form.setValue({ supplierId: o.supplierId, orderDate: this.toDate(o.orderDate), expectedDeliveryDate: o.expectedDeliveryDate ? this.toDate(o.expectedDeliveryDate) : '', notes: o.notes ?? '' });
    this.showModal = true;
  }
  closeModal() { this.showModal = false; }

  save() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.isSaving = true;
    const v = this.form.value;
    const payload = { supplierId: v.supplierId, orderDate: v.orderDate, expectedDeliveryDate: v.expectedDeliveryDate || undefined, notes: v.notes || undefined };
    const obs = this.editId ? this.svc.updatePurchaseOrder(this.editId, payload) : this.svc.createPurchaseOrder(payload);
    obs.subscribe({
      next: () => { this.isSaving = false; this.closeModal(); this.showSuccess(this.editId ? 'PO updated.' : 'PO created.'); this.loadAll(); },
      error: (e: HttpErrorResponse) => { this.isSaving = false; this.errorMessage = e.error?.message ?? 'Save failed.'; },
    });
  }

  openStatus(o: PurchaseOrderDto) { this.statusOrderId = o.poId; this.statusForm.setValue({ status: o.status }); this.showStatusModal = true; }
  closeStatus() { this.showStatusModal = false; }
  saveStatus() {
    if (!this.statusOrderId || this.statusForm.invalid) return;
    this.isSaving = true;
    this.svc.updatePoStatus(this.statusOrderId, this.statusForm.value).subscribe({
      next: () => { this.isSaving = false; this.closeStatus(); this.showSuccess('Status updated.'); this.loadAll(); },
      error: (e: HttpErrorResponse) => { this.isSaving = false; this.errorMessage = e.error?.message ?? 'Status update failed.'; },
    });
  }

  confirmDelete(o: PurchaseOrderDto) { this.deleteTarget = o; this.showDeleteConfirm = true; }
  cancelDelete() { this.deleteTarget = null; this.showDeleteConfirm = false; }
  doDelete() {
    if (!this.deleteTarget) return;
    this.isDeleting = true;
    this.svc.deletePurchaseOrder(this.deleteTarget.poId).subscribe({
      next: () => { this.isDeleting = false; this.cancelDelete(); this.showSuccess('PO deleted.'); this.loadAll(); },
      error: (e: HttpErrorResponse) => { this.isDeleting = false; this.errorMessage = e.error?.message ?? 'Delete failed.'; this.cancelDelete(); },
    });
  }

  // Only show Active suppliers in the create/edit dropdown
  get activeSuppliers(): SupplierDto[] { return this.suppliers.filter(s => s.status === 'Active'); }

  // Minimum selectable delivery date = day after order date
  get minDeliveryDate(): string {
    const orderDate = this.form.get('orderDate')?.value as string;
    if (!orderDate) return '';
    const d = new Date(orderDate);
    d.setDate(d.getDate() + 1);
    return d.toISOString().split('T')[0];
  }

  // Cross-field validator: expectedDeliveryDate must be strictly after orderDate
  private deliveryAfterOrderValidator(group: AbstractControl) {
    const order    = group.get('orderDate')?.value as string;
    const delivery = group.get('expectedDeliveryDate')?.value as string;
    if (order && delivery && delivery <= order)
      return { deliveryNotAfterOrder: true };
    return null;
  }

  statusBadge(s: string) {
    const m: Record<string,string> = { Draft:'bg-secondary', Submitted:'bg-primary', Approved:'bg-success', Shipped:'bg-info text-dark', PartiallyReceived:'bg-warning text-dark', FullyReceived:'bg-success', Cancelled:'bg-danger' };
    return m[s] ?? 'bg-secondary';
  }
  canEdit(o: PurchaseOrderDto) { return o.status === 'Draft'; }
  canDelete(o: PurchaseOrderDto) { return true; }
  private today() { return new Date().toISOString().split('T')[0]; }
  private toDate(d: string) { return d.split('T')[0]; }
  private showSuccess(msg: string) { this.successMessage = msg; this.errorMessage = ''; setTimeout(() => this.successMessage = '', 3500); }
}
