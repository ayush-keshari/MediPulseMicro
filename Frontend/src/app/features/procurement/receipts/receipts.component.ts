import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ProcurementService } from '../../../services/procurement/procurement.service';
import { ReceiptDto, PurchaseOrderDto } from '../../../services/procurement/procurement.models';

@Component({
  selector: 'app-receipts',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './receipts.component.html',
  styleUrl: './receipts.component.css',
})
export class ReceiptsComponent implements OnInit {
  receipts: ReceiptDto[] = [];
  filtered: ReceiptDto[] = [];
  orders: PurchaseOrderDto[] = [];
  isLoading = false;
  successMessage = ''; errorMessage = '';
  search = ''; filterQuality = '';

  showModal = false; isSaving = false; editId: number | null = null;
  form: FormGroup;
  showDeleteConfirm = false; deleteTarget: ReceiptDto | null = null; isDeleting = false;

  qualityStatuses = ['Accepted', 'Rejected', 'OnHold'];

  constructor(private svc: ProcurementService, private fb: FormBuilder) {
    this.form = this.fb.group({
      poId:             [null, Validators.required],
      supplierLot:      [''],
      receivedDate:     [this.today(), Validators.required],
      receivedBy:       ['', [Validators.required, Validators.maxLength(100)]],
      qualityStatus:    ['Accepted', Validators.required],
      quantityReceived: [null, [Validators.required, Validators.min(1)]],
    });
  }

  ngOnInit() { this.loadAll(); }

  loadAll() {
    this.isLoading = true;
    this.svc.getReceipts().subscribe({
      next: (d) => { this.receipts = d; this.applyFilter(); this.isLoading = false; },
      error: () => { this.errorMessage = 'Failed to load receipts.'; this.isLoading = false; },
    });
    this.svc.getPurchaseOrders().subscribe({ next: (o) => this.orders = o });
  }

  applyFilter() {
    const q = this.search.toLowerCase();
    this.filtered = this.receipts.filter(r =>
      (!q || r.receivedBy.toLowerCase().includes(q) || r.supplierName.toLowerCase().includes(q) || String(r.receiptId).includes(q)) &&
      (!this.filterQuality || r.qualityStatus === this.filterQuality)
    );
  }

  openAdd() { this.editId = null; this.form.reset({ receivedDate: this.today(), qualityStatus: 'Accepted' }); this.showModal = true; }
  openEdit(r: ReceiptDto) {
    this.editId = r.receiptId;
    this.form.setValue({ poId: r.poId, supplierLot: r.supplierLot ?? '', receivedDate: this.toDate(r.receivedDate), receivedBy: r.receivedBy, qualityStatus: r.qualityStatus, quantityReceived: r.quantityReceived });
    this.showModal = true;
  }
  closeModal() { this.showModal = false; }

  save() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.isSaving = true;
    const v = this.form.value;
    const payload = { poId: v.poId, supplierLot: v.supplierLot || undefined, receivedDate: v.receivedDate, receivedBy: v.receivedBy, qualityStatus: v.qualityStatus, quantityReceived: v.quantityReceived };
    const obs = this.editId
      ? this.svc.updateReceipt(this.editId, { supplierLot: payload.supplierLot, receivedDate: payload.receivedDate, receivedBy: payload.receivedBy, qualityStatus: payload.qualityStatus, quantityReceived: payload.quantityReceived })
      : this.svc.createReceipt(payload);
    obs.subscribe({
      next: () => { this.isSaving = false; this.closeModal(); this.showSuccess(this.editId ? 'Receipt updated.' : 'Receipt created.'); this.loadAll(); },
      error: (e: HttpErrorResponse) => { this.isSaving = false; this.errorMessage = e.error?.message ?? 'Save failed.'; },
    });
  }

  confirmDelete(r: ReceiptDto) { this.deleteTarget = r; this.showDeleteConfirm = true; }
  cancelDelete() { this.deleteTarget = null; this.showDeleteConfirm = false; }
  doDelete() {
    if (!this.deleteTarget) return;
    this.isDeleting = true;
    this.svc.deleteReceipt(this.deleteTarget.receiptId).subscribe({
      next: () => { this.isDeleting = false; this.cancelDelete(); this.showSuccess('Receipt deleted.'); this.loadAll(); },
      error: (e: HttpErrorResponse) => { this.isDeleting = false; this.errorMessage = e.error?.message ?? 'Delete failed.'; this.cancelDelete(); },
    });
  }

  qualityBadge(s: string) { return { Accepted: 'bg-success', Rejected: 'bg-danger', OnHold: 'bg-warning text-dark' }[s] ?? 'bg-secondary'; }
  private today() { return new Date().toISOString().split('T')[0]; }
  private toDate(d: string) { return d.split('T')[0]; }
  private showSuccess(msg: string) { this.successMessage = msg; this.errorMessage = ''; setTimeout(() => this.successMessage = '', 3500); }
}
