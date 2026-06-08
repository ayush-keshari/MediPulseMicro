import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { LogisticsService } from '../../../services/logistics/logistics.service';
import { ConsumptionRecordDto } from '../../../services/logistics/logistics.models';
import { FacilityService } from '../../../services/facility/facility.service';
import { FacilityDto } from '../../../services/facility/facility.models';
import { InventoryService } from '../../../services/inventory/inventory.service';
import { ItemResponse } from '../../../services/inventory/inventory.models';

@Component({
  selector: 'app-consumption',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './consumption.component.html',
  styleUrl: './consumption.component.css',
})
export class ConsumptionComponent implements OnInit {
  records: ConsumptionRecordDto[] = [];
  filtered: ConsumptionRecordDto[] = [];
  isLoading = false;
  successMessage = '';
  errorMessage = '';
  search = '';

  facilities: FacilityDto[] = [];
  items: ItemResponse[] = [];
  itemsForFacility: ItemResponse[] = [];
  facilityStockMap = new Map<number, number>();
  facilityItemsLoading = false;

  showModal = false;
  isSaving = false;
  editId: number | null = null;
  form: FormGroup;

  showDeleteConfirm = false;
  deleteTarget: ConsumptionRecordDto | null = null;
  isDeleting = false;

  constructor(
    private svc: LogisticsService,
    private facilitySvc: FacilityService,
    private inventorySvc: InventoryService,
    private fb: FormBuilder,
  ) {
    this.form = this.fb.group({
      facilityId:       ['', Validators.required],
      wardId:           [''],
      itemId:           ['', Validators.required],
      quantityConsumed: [1, [Validators.required, Validators.min(1)]],
      consumedDate:     ['', Validators.required],
      consumedBy:       ['', Validators.required],
    });
  }

  ngOnInit() {
    this.load();
    this.facilitySvc.getFacilities().subscribe({ next: (d) => this.facilities = d });
    this.inventorySvc.getItems().subscribe({ next: (d) => this.items = d });

    this.form.get('facilityId')!.valueChanges.subscribe(facilityId => {
      this.onFacilityChange(facilityId);
    });
  }

  onFacilityChange(facilityId: any) {
    this.form.get('itemId')!.setValue('', { emitEvent: false });
    this.itemsForFacility = [];
    this.facilityStockMap = new Map();
    if (!facilityId) return;
    this.facilityItemsLoading = true;
    this.inventorySvc.getFacilityStock(+facilityId).subscribe({
      next: (stock) => {
        const ids = stock.map(s => s.itemId);
        this.itemsForFacility = this.items.filter(i => ids.includes(i.itemId));
        this.facilityStockMap = new Map(stock.map(s => [s.itemId, s.availableQty]));
        this.facilityItemsLoading = false;
      },
      error: () => { this.itemsForFacility = []; this.facilityItemsLoading = false; },
    });
  }

  get displayItems(): ItemResponse[] {
    return this.form.get('facilityId')?.value ? this.itemsForFacility : this.items;
  }

  load() {
    this.isLoading = true;
    this.svc.getConsumptions().subscribe({
      next: (d) => { this.records = d; this.applyFilter(); this.isLoading = false; },
      error: () => { this.errorMessage = 'Failed to load consumption records.'; this.isLoading = false; },
    });
  }

  applyFilter() {
    const q = this.search.toLowerCase();
    this.filtered = this.records.filter(r =>
      !q || r.itemName.toLowerCase().includes(q) || r.consumedBy.toLowerCase().includes(q)
    );
  }

  openAdd() {
    this.editId = null;
    this.itemsForFacility = [];
    this.facilityStockMap = new Map();
    const today = new Date().toISOString().substring(0, 10);
    this.form.reset({ quantityConsumed: 1, consumedDate: today });
    this.showModal = true;
  }

  openEdit(r: ConsumptionRecordDto) {
    this.editId = r.consumptionId;
    this.itemsForFacility = [];
    this.facilityStockMap = new Map();
    this.form.patchValue({
      facilityId:       r.facilityId,
      wardId:           r.wardId ?? '',
      itemId:           r.itemId,
      quantityConsumed: r.quantityConsumed,
      consumedDate:     r.consumedDate.substring(0, 10),
      consumedBy:       r.consumedBy,
    });
    // Load stock for the pre-selected facility so the item dropdown is populated
    this.onFacilityChange(r.facilityId);
    this.showModal = true;
  }

  closeModal() { this.showModal = false; this.errorMessage = ''; }

  getItemName(id: any): string {
    return this.items.find(i => i.itemId === +id)?.name ?? '';
  }

  save() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.isSaving = true;
    const v = this.form.value;
    const obs = this.editId
      ? this.svc.updateConsumption(this.editId, {
          quantityConsumed: +v.quantityConsumed,
          consumedDate:     v.consumedDate,
          consumedBy:       v.consumedBy,
        })
      : this.svc.createConsumption({
          facilityId:       +v.facilityId,
          wardId:           v.wardId ? +v.wardId : undefined,
          itemId:           +v.itemId,
          itemName:         this.getItemName(v.itemId),
          quantityConsumed: +v.quantityConsumed,
          consumedDate:     v.consumedDate,
          consumedBy:       v.consumedBy,
        });

    obs.subscribe({
      next: () => { this.isSaving = false; this.closeModal(); this.showSuccess(this.editId ? 'Record updated.' : 'Record added.'); this.load(); },
      error: (e: HttpErrorResponse) => { this.isSaving = false; this.errorMessage = e.error?.message ?? 'Save failed.'; },
    });
  }

  confirmDelete(r: ConsumptionRecordDto) { this.deleteTarget = r; this.showDeleteConfirm = true; }
  cancelDelete() { this.deleteTarget = null; this.showDeleteConfirm = false; }
  doDelete() {
    if (!this.deleteTarget) return;
    this.isDeleting = true;
    this.svc.deleteConsumption(this.deleteTarget.consumptionId).subscribe({
      next: () => { this.isDeleting = false; this.cancelDelete(); this.showSuccess('Record deleted.'); this.load(); },
      error: (e: HttpErrorResponse) => { this.isDeleting = false; this.errorMessage = e.error?.message ?? 'Delete failed.'; this.cancelDelete(); },
    });
  }

  private showSuccess(msg: string) { this.successMessage = msg; this.errorMessage = ''; setTimeout(() => this.successMessage = '', 3500); }

  get totalQty()        { return this.records.reduce((s, r) => s + r.quantityConsumed, 0); }
  get uniqueItems()     { return new Set(this.records.map(r => r.itemId)).size; }
  get uniqueFacilities(){ return new Set(this.records.map(r => r.facilityId)).size; }
}
