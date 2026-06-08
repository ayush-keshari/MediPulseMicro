import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { InventoryService } from '../../../services/inventory/inventory.service';
import { PositionResponse, ItemResponse } from '../../../services/inventory/inventory.models';
import { FacilityService } from '../../../services/facility/facility.service';
import { FacilityDto, StorageZoneDto } from '../../../services/facility/facility.models';

@Component({
  selector: 'app-stock-positions',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './stock-positions.component.html',
  styleUrl: './stock-positions.component.css',
})
export class StockPositionsComponent implements OnInit {
  positions: PositionResponse[] = [];
  filtered: PositionResponse[] = [];
  items: ItemResponse[] = [];
  facilities: FacilityDto[] = [];
  allZones: StorageZoneDto[] = [];
  filteredZones: StorageZoneDto[] = [];

  isLoading = false;
  successMessage = '';
  errorMessage = '';
  search = '';
  statusFilter = '';

  showModal = false;
  isSaving = false;
  editId: number | null = null;
  form: FormGroup;

  showDeleteConfirm = false;
  deleteTarget: PositionResponse | null = null;
  isDeleting = false;

  constructor(
    private svc: InventoryService,
    private facilitySvc: FacilityService,
    private fb: FormBuilder,
  ) {
    this.form = this.fb.group({
      itemId:        ['', Validators.required],
      lotId:         ['', [Validators.required, Validators.maxLength(50)]],
      expiryDate:    ['', Validators.required],
      quantity:      [1, [Validators.required, Validators.min(1)]],
      facilityId:    ['', Validators.required],
      storageZoneId: ['', Validators.required],
      safetyStock:   [0, Validators.min(0)],
    });
  }

  ngOnInit() { this.loadAll(); }

  loadAll() {
    this.isLoading = true;
    this.svc.getPositions().subscribe({
      next: (data) => { this.positions = data; this.applyFilter(); this.isLoading = false; },
      error: () => { this.errorMessage = 'Failed to load stock positions.'; this.isLoading = false; },
    });
    this.svc.getItems().subscribe({ next: (d) => this.items = d });
    this.facilitySvc.getFacilities().subscribe({ next: (d) => this.facilities = d });
    this.facilitySvc.getZones().subscribe({ next: (d) => { this.allZones = d; this.filteredZones = d; } });
  }

  applyFilter() {
    const q = this.search.toLowerCase();
    this.filtered = this.positions.filter(p => {
      const matchSearch = !q || p.itemName.toLowerCase().includes(q) || p.itemCode.toLowerCase().includes(q) || p.lotId.toLowerCase().includes(q);
      const matchStatus = !this.statusFilter ||
        (this.statusFilter === 'expired'  && p.isExpired) ||
        (this.statusFilter === 'expiring' && p.isExpiringSoon && !p.isExpired) ||
        (this.statusFilter === 'low'      && p.isBelowSafetyStock) ||
        (this.statusFilter === 'ok'       && !p.isExpired && !p.isBelowSafetyStock);
      return matchSearch && matchStatus;
    });
  }

  onFacilityChange() {
    const fid = +this.form.get('facilityId')?.value;
    this.filteredZones = fid ? this.allZones.filter(z => z.facilityId === fid) : this.allZones;
    this.form.patchValue({ storageZoneId: '' });
  }

  openAdd() {
    this.editId = null;
    this.filteredZones = this.allZones;
    this.form.reset({ quantity: 1, safetyStock: 0 });
    this.showModal = true;
  }

  openEdit(p: PositionResponse) {
    this.editId = p.positionId;
    this.filteredZones = p.facilityId ? this.allZones.filter(z => z.facilityId === p.facilityId) : this.allZones;
    this.form.patchValue({
      itemId:        p.itemId,
      lotId:         p.lotId,
      expiryDate:    p.expiryDate.substring(0, 10),
      quantity:      p.quantity,
      facilityId:    p.facilityId,
      storageZoneId: p.storageZoneId,
      safetyStock:   p.safetyStock,
    });
    this.showModal = true;
  }

  closeModal() { this.showModal = false; this.errorMessage = ''; }

  save() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.isSaving = true;
    const val = this.form.value;
    const safetyStock = val.safetyStock ?? 0;
    const obs = this.editId
      ? this.svc.updatePosition(this.editId, { quantity: val.quantity, facilityId: +val.facilityId, storageZoneId: +val.storageZoneId, safetyStock, expiryDate: val.expiryDate })
      : this.svc.createPosition({ ...val, itemId: +val.itemId, facilityId: +val.facilityId, storageZoneId: +val.storageZoneId, safetyStock });
    obs.subscribe({
      next: () => { this.isSaving = false; this.closeModal(); this.showSuccess(this.editId ? 'Position updated.' : 'Stock lot added.'); this.loadAll(); },
      error: (e: HttpErrorResponse) => { this.isSaving = false; this.errorMessage = e.error?.message ?? 'Save failed.'; },
    });
  }

  confirmDelete(p: PositionResponse) { this.deleteTarget = p; this.showDeleteConfirm = true; }
  cancelDelete() { this.deleteTarget = null; this.showDeleteConfirm = false; }
  doDelete() {
    if (!this.deleteTarget) return;
    this.isDeleting = true;
    this.svc.deletePosition(this.deleteTarget.positionId).subscribe({
      next: () => { this.isDeleting = false; this.cancelDelete(); this.showSuccess('Position removed.'); this.loadAll(); },
      error: (e: HttpErrorResponse) => { this.isDeleting = false; this.errorMessage = e.error?.message ?? 'Delete failed.'; this.cancelDelete(); },
    });
  }

  rowClass(p: PositionResponse) {
    if (p.isExpired) return 'table-danger';
    if (p.isExpiringSoon || p.isBelowSafetyStock) return 'table-warning';
    return '';
  }

  getFacilityName(id: number): string {
    return this.facilities.find(f => f.facilityId === id)?.name ?? `Facility #${id}`;
  }

  getZoneName(id: number): string {
    return this.allZones.find(z => z.zoneId === id)?.name ?? `Zone #${id}`;
  }

  private showSuccess(msg: string) { this.successMessage = msg; this.errorMessage = ''; setTimeout(() => this.successMessage = '', 3500); }

  get expiredCount()     { return this.positions.filter(p => p.isExpired).length; }
  get expiringSoonCount(){ return this.positions.filter(p => p.isExpiringSoon && !p.isExpired).length; }
  get lowStockCount()    { return this.positions.filter(p => p.isBelowSafetyStock).length; }
}
