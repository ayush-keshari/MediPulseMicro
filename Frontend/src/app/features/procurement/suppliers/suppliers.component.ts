import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ProcurementService } from '../../../services/procurement/procurement.service';
import { SupplierDto } from '../../../services/procurement/procurement.models';

@Component({
  selector: 'app-suppliers',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './suppliers.component.html',
  styleUrl: './suppliers.component.css',
})
export class SuppliersComponent implements OnInit {
  suppliers: SupplierDto[] = [];
  filtered: SupplierDto[] = [];
  isLoading = false;
  successMessage = ''; errorMessage = '';
  search = ''; filterStatus = '';

  showModal = false; isSaving = false; editId: number | null = null;
  form: FormGroup;
  showDeleteConfirm = false; deleteTarget: SupplierDto | null = null; isDeleting = false;

  types   = ['Manufacturer', 'Distributor', '3PL'];
  statuses = ['Active', 'Inactive', 'OnHold'];

  constructor(private svc: ProcurementService, private fb: FormBuilder) {
    this.form = this.fb.group({
      name:         ['', [Validators.required, Validators.maxLength(100)]],
      supplierType: ['Manufacturer', Validators.required],
      status:       ['Active', Validators.required],
    });
  }

  ngOnInit() { this.load(); }

  load() {
    this.isLoading = true;
    this.svc.getSuppliers().subscribe({
      next: (d) => { this.suppliers = d; this.applyFilter(); this.isLoading = false; },
      error: () => { this.errorMessage = 'Failed to load suppliers.'; this.isLoading = false; },
    });
  }

  applyFilter() {
    const q = this.search.toLowerCase();
    this.filtered = this.suppliers.filter(s =>
      (!q || s.name.toLowerCase().includes(q)) &&
      (!this.filterStatus || s.status === this.filterStatus)
    );
  }

  openAdd() { this.editId = null; this.form.reset({ supplierType: 'Manufacturer', status: 'Active' }); this.showModal = true; }
  openEdit(s: SupplierDto) { this.editId = s.supplierId; this.form.setValue({ name: s.name, supplierType: s.supplierType ?? 'Manufacturer', status: s.status }); this.showModal = true; }
  closeModal() { this.showModal = false; }

  save() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.isSaving = true;
    const val = this.form.value;
    const obs = this.editId ? this.svc.updateSupplier(this.editId, val) : this.svc.createSupplier(val);
    obs.subscribe({
      next: () => { this.isSaving = false; this.closeModal(); this.showSuccess(this.editId ? 'Supplier updated.' : 'Supplier created.'); this.load(); },
      error: (e: HttpErrorResponse) => { this.isSaving = false; this.errorMessage = e.error?.message ?? 'Save failed.'; },
    });
  }

  confirmDelete(s: SupplierDto) { this.deleteTarget = s; this.showDeleteConfirm = true; }
  cancelDelete() { this.deleteTarget = null; this.showDeleteConfirm = false; }
  doDelete() {
    if (!this.deleteTarget) return;
    this.isDeleting = true;
    this.svc.deleteSupplier(this.deleteTarget.supplierId).subscribe({
      next: () => { this.isDeleting = false; this.cancelDelete(); this.showSuccess('Supplier deleted.'); this.load(); },
      error: (e: HttpErrorResponse) => { this.isDeleting = false; this.errorMessage = e.error?.message ?? 'Delete failed.'; this.cancelDelete(); },
    });
  }

  count(status: string) { return this.suppliers.filter(s => s.status === status).length; }
  statusBadge(s: string) { return { Active: 'bg-success', Inactive: 'bg-secondary', OnHold: 'bg-warning text-dark' }[s] ?? 'bg-secondary'; }
  typeBadge(t?: string)  { return { Manufacturer: 'bg-primary', Distributor: 'bg-info text-dark', '3PL': 'bg-warning text-dark' }[t ?? ''] ?? 'bg-secondary'; }
  private showSuccess(msg: string) { this.successMessage = msg; this.errorMessage = ''; setTimeout(() => this.successMessage = '', 3500); }
}
