import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { InventoryService } from '../../../services/inventory/inventory.service';
import { ItemResponse } from '../../../services/inventory/inventory.models';

@Component({
  selector: 'app-items',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './items.component.html',
  styleUrl: './items.component.css',
})
export class ItemsComponent implements OnInit {
  items: ItemResponse[] = [];
  filtered: ItemResponse[] = [];
  isLoading = false;
  successMessage = '';
  errorMessage = '';
  search = '';
  categoryFilter = '';

  showModal = false;
  isSaving = false;
  editId: number | null = null;
  form: FormGroup;

  showDeleteConfirm = false;
  deleteTarget: ItemResponse | null = null;
  isDeleting = false;

  storageOptions = ['Ambient', 'Refrigerated', 'Freezer'];
  categories = ['Drug', 'Device', 'Consumable', 'PPE', 'Laboratory', 'Other'];

  constructor(private svc: InventoryService, private fb: FormBuilder) {
    this.form = this.fb.group({
      itemCode:           ['', [Validators.required, Validators.maxLength(50)]],
      name:               ['', [Validators.required, Validators.maxLength(150)]],
      category:           ['Drug', Validators.required],
      unit:               ['', [Validators.required, Validators.maxLength(20), Validators.pattern(/^[a-zA-Z/ ]+$/)]],
      storageRequirement: ['Ambient', Validators.required],
      safetyStock:        [0, Validators.min(0)],
    });
  }

  ngOnInit() { this.load(); }

  load() {
    this.isLoading = true;
    this.svc.getItems().subscribe({
      next: (data) => { this.items = data; this.applyFilter(); this.isLoading = false; },
      error: () => { this.errorMessage = 'Failed to load items.'; this.isLoading = false; },
    });
  }

  applyFilter() {
    const q = this.search.toLowerCase();
    this.filtered = this.items.filter(i =>
      (!q || i.name.toLowerCase().includes(q) || i.itemCode.toLowerCase().includes(q)) &&
      (!this.categoryFilter || i.category === this.categoryFilter)
    );
  }

  openAdd() {
    this.editId = null;
    this.form.reset({ category: 'Drug', storageRequirement: 'Ambient', safetyStock: 0 });
    this.form.get('itemCode')?.enable();
    this.showModal = true;
  }

  openEdit(item: ItemResponse) {
    this.editId = item.itemId;
    this.form.setValue({
      itemCode: item.itemCode,
      name: item.name,
      category: item.category,
      unit: item.unit,
      storageRequirement: item.storageRequirement,
      safetyStock: item.safetyStock,
    });
    this.form.get('itemCode')?.disable();
    this.showModal = true;
  }

  closeModal() { this.showModal = false; this.errorMessage = ''; }

  save() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.isSaving = true;
    const val = this.form.getRawValue();
    const safetyStock = val.safetyStock ?? 0;
    const obs = this.editId
      ? this.svc.updateItem(this.editId, { name: val.name, category: val.category, unit: val.unit, storageRequirement: val.storageRequirement, safetyStock })
      : this.svc.createItem({ ...val, safetyStock });
    obs.subscribe({
      next: () => { this.isSaving = false; this.closeModal(); this.showSuccess(this.editId ? 'Item updated.' : 'Item created.'); this.load(); },
      error: (e: HttpErrorResponse) => { this.isSaving = false; this.errorMessage = e.error?.message ?? 'Save failed.'; },
    });
  }

  confirmDelete(item: ItemResponse) { this.deleteTarget = item; this.showDeleteConfirm = true; }
  cancelDelete() { this.deleteTarget = null; this.showDeleteConfirm = false; }
  doDelete() {
    if (!this.deleteTarget) return;
    this.isDeleting = true;
    this.svc.deleteItem(this.deleteTarget.itemId).subscribe({
      next: () => { this.isDeleting = false; this.cancelDelete(); this.showSuccess('Item deleted.'); this.load(); },
      error: (e: HttpErrorResponse) => { this.isDeleting = false; this.errorMessage = e.error?.message ?? 'Delete failed.'; this.cancelDelete(); },
    });
  }

  storageBadge(s: string) {
    return { Ambient: 'bg-secondary', Refrigerated: 'bg-info', Freezer: 'bg-primary' }[s] ?? 'bg-secondary';
  }

  onUnitInput(event: Event) {
    const input = event.target as HTMLInputElement;
    const cleaned = input.value.replace(/[^a-zA-Z/ ]/g, '');
    if (input.value !== cleaned) {
      input.value = cleaned;
      this.form.get('unit')!.setValue(cleaned, { emitEvent: false });
    }
  }

  private showSuccess(msg: string) { this.successMessage = msg; this.errorMessage = ''; setTimeout(() => this.successMessage = '', 3500); }

  get uniqueCategories() { return [...new Set(this.items.map(i => i.category))]; }
  countByCategory(c: string) { return this.items.filter(i => i.category === c).length; }
  get lowStockCount() { return this.items.filter(i => i.totalStock < i.safetyStock).length; }
}
