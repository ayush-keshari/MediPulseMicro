import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { LogisticsService } from '../../../services/logistics/logistics.service';
import { TransferOrderDto } from '../../../services/logistics/logistics.models';
import { FacilityService } from '../../../services/facility/facility.service';
import { FacilityDto } from '../../../services/facility/facility.models';
import { InventoryService } from '../../../services/inventory/inventory.service';
import { ItemResponse } from '../../../services/inventory/inventory.models';

@Component({
  selector: 'app-transfer-orders',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './transfer-orders.component.html',
  styleUrl: './transfer-orders.component.css',
})
export class TransferOrdersComponent implements OnInit {
  orders: TransferOrderDto[] = [];
  filtered: TransferOrderDto[] = [];
  isLoading = false;
  successMessage = '';
  errorMessage = '';
  search = '';
  statusFilter = '';

  facilities: FacilityDto[] = [];
  items: ItemResponse[] = [];
  itemsForFacility: ItemResponse[] = [];          // filtered to source facility stock
  facilityStockMap = new Map<number, number>();   // itemId → total available qty
  facilityItemsLoading = false;

  showModal = false;
  isSaving = false;
  form: FormGroup;

  showStatusModal = false;
  statusTarget: TransferOrderDto | null = null;
  newStatus = '';
  isSavingStatus = false;

  selectedOrder: TransferOrderDto | null = null;

  statuses = ['Draft', 'Submitted', 'Approved', 'InTransit', 'Completed', 'Cancelled'];

  constructor(
    private svc: LogisticsService,
    private facilitySvc: FacilityService,
    private inventorySvc: InventoryService,
    private fb: FormBuilder,
  ) {
    this.form = this.fb.group({
      fromFacilityId: ['', Validators.required],
      toFacilityId:   ['', Validators.required],
      requestedBy:    ['', Validators.required],
      items: this.fb.array([this.newItemRow()]),
    });
  }

  newItemRow(): FormGroup {
    return this.fb.group({
      itemId:   ['', Validators.required],
      quantity: [1, [Validators.required, Validators.min(1)]],
    });
  }

  get itemsArray(): FormArray { return this.form.get('items') as FormArray; }
  addItemRow() { this.itemsArray.push(this.newItemRow()); }
  removeItemRow(i: number) { if (this.itemsArray.length > 1) this.itemsArray.removeAt(i); }

  ngOnInit() {
    this.load();
    this.facilitySvc.getFacilities().subscribe({ next: (d) => this.facilities = d });
    this.inventorySvc.getItems().subscribe({ next: (d) => this.items = d });

    this.form.get('fromFacilityId')!.valueChanges.subscribe(facilityId => {
      this.onSourceFacilityChange(facilityId);
    });
  }

  onSourceFacilityChange(facilityId: any) {
    // Reset all item selections when source facility changes
    this.itemsArray.controls.forEach(row => row.get('itemId')!.setValue('', { emitEvent: false }));
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

  getAvailableQty(itemId: any): number {
    return itemId ? (this.facilityStockMap.get(+itemId) ?? 0) : 0;
  }

  isQtyExceeded(index: number): boolean {
    const row = this.itemsArray.at(index);
    const itemId = row.get('itemId')?.value;
    const qty = row.get('quantity')?.value;
    if (!itemId || !qty) return false;
    return qty > this.getAvailableQty(itemId);
  }

  get displayItems(): ItemResponse[] {
    return this.form.get('fromFacilityId')?.value ? this.itemsForFacility : this.items;
  }

  load() {
    this.isLoading = true;
    this.svc.getTransferOrders().subscribe({
      next: (d) => { this.orders = d; this.applyFilter(); this.isLoading = false; },
      error: () => { this.errorMessage = 'Failed to load transfer orders.'; this.isLoading = false; },
    });
  }

  applyFilter() {
    const q = this.search.toLowerCase();
    this.filtered = this.orders.filter(o =>
      (!q || o.fromFacilityName.toLowerCase().includes(q) || o.toFacilityName.toLowerCase().includes(q) || o.requestedBy.toLowerCase().includes(q)) &&
      (!this.statusFilter || o.status === this.statusFilter)
    );
  }

  openAdd() {
    this.form.reset();
    while (this.itemsArray.length > 1) this.itemsArray.removeAt(1);
    this.itemsArray.at(0).reset({ quantity: 1 });
    this.itemsForFacility = [];
    this.facilityStockMap = new Map();
    this.showModal = true;
  }
  closeModal() { this.showModal = false; this.errorMessage = ''; }

  getFacilityName(id: any): string {
    return this.facilities.find(f => f.facilityId === +id)?.name ?? '';
  }

  getItemName(id: any): string {
    return this.items.find(i => i.itemId === +id)?.name ?? '';
  }

  save() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.isSaving = true;
    const v = this.form.value;
    this.svc.createTransferOrder({
      fromFacilityId:   +v.fromFacilityId,
      fromFacilityName: this.getFacilityName(v.fromFacilityId),
      toFacilityId:     +v.toFacilityId,
      toFacilityName:   this.getFacilityName(v.toFacilityId),
      requestedBy:      v.requestedBy,
      items: v.items.map((i: any) => ({
        itemId:   +i.itemId,
        itemName: this.getItemName(i.itemId),
        quantity: +i.quantity,
      })),
    }).subscribe({
      next: () => { this.isSaving = false; this.closeModal(); this.showSuccess('Transfer order created.'); this.load(); },
      error: (e: HttpErrorResponse) => { this.isSaving = false; this.errorMessage = e.error?.message ?? 'Save failed.'; },
    });
  }

  openStatus(o: TransferOrderDto) { this.statusTarget = o; this.newStatus = o.status; this.showStatusModal = true; }
  closeStatusModal() { this.showStatusModal = false; this.statusTarget = null; }

  saveStatus() {
    if (!this.newStatus || !this.statusTarget) return;
    this.isSavingStatus = true;
    this.svc.updateTransferStatus(this.statusTarget.transferOrderId, { status: this.newStatus }).subscribe({
      next: () => { this.isSavingStatus = false; this.closeStatusModal(); this.showSuccess('Status updated.'); this.load(); },
      error: (e: HttpErrorResponse) => { this.isSavingStatus = false; this.errorMessage = e.error?.message ?? 'Update failed.'; },
    });
  }

  deleteOrder(id: number) {
    this.svc.deleteTransferOrder(id).subscribe({
      next: () => { this.showSuccess('Order deleted.'); this.load(); },
    });
  }

  viewOrder(o: TransferOrderDto) { this.selectedOrder = this.selectedOrder?.transferOrderId === o.transferOrderId ? null : o; }

  statusClass(s: string) {
    return { Draft: 'bg-secondary', Submitted: 'bg-info', Approved: 'bg-primary', InTransit: 'bg-warning', Completed: 'bg-success', Cancelled: 'bg-danger' }[s] ?? 'bg-secondary';
  }

  private showSuccess(msg: string) { this.successMessage = msg; this.errorMessage = ''; setTimeout(() => this.successMessage = '', 3500); }

  get draftCount()     { return this.orders.filter(o => o.status === 'Draft').length; }
  get inTransitCount() { return this.orders.filter(o => o.status === 'InTransit').length; }
  get completedCount() { return this.orders.filter(o => o.status === 'Completed').length; }
}
