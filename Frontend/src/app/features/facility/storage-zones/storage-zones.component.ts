import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { FacilityService } from '../../../services/facility/facility.service';
import { StorageZoneDto, FacilityDto } from '../../../services/facility/facility.models';

@Component({
  selector: 'app-storage-zones',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './storage-zones.component.html',
  styleUrl: './storage-zones.component.css',
})
export class StorageZonesComponent implements OnInit {
  zones: StorageZoneDto[] = [];
  filtered: StorageZoneDto[] = [];
  facilities: FacilityDto[] = [];
  isLoading = false;
  successMessage = '';
  errorMessage = '';
  search = '';
  filterProfile = '';

  showModal = false;
  isSaving = false;
  editId: number | null = null;
  form: FormGroup;

  showDeleteConfirm = false;
  deleteTarget: StorageZoneDto | null = null;
  isDeleting = false;

  profiles = ['Ambient', 'Refrigerated', 'Freezer'];

  constructor(private svc: FacilityService, private fb: FormBuilder) {
    this.form = this.fb.group({
      facilityId:         [null, Validators.required],
      name:               ['', [Validators.required, Validators.maxLength(100)]],
      temperatureProfile: ['Ambient', Validators.required],
      capacity:           [null, [Validators.required, Validators.min(0.01)]],
    });
  }

  ngOnInit() { this.loadAll(); }

  loadAll() {
    this.isLoading = true;
    this.svc.getZones().subscribe({
      next: (data) => { this.zones = data; this.applyFilter(); this.isLoading = false; },
      error: () => { this.errorMessage = 'Failed to load zones.'; this.isLoading = false; },
    });
    this.svc.getFacilities().subscribe({ next: (f) => this.facilities = f });
  }

  applyFilter() {
    const q = this.search.toLowerCase();
    this.filtered = this.zones.filter(z =>
      (!q || (z.name ?? '').toLowerCase().includes(q) || z.facilityName.toLowerCase().includes(q)) &&
      (!this.filterProfile || z.temperatureProfile === this.filterProfile)
    );
  }

  openAdd() { this.editId = null; this.form.reset({ temperatureProfile: 'Ambient' }); this.showModal = true; }
  openEdit(z: StorageZoneDto) {
    this.editId = z.zoneId;
    this.form.setValue({ facilityId: z.facilityId ?? null, name: z.name ?? '', temperatureProfile: z.temperatureProfile ?? 'Ambient', capacity: z.capacity ?? null });
    this.showModal = true;
  }
  closeModal() { this.showModal = false; }

  save() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.isSaving = true;
    const val = this.form.value;
    const obs = this.editId
      ? this.svc.updateZone(this.editId, { name: val.name, temperatureProfile: val.temperatureProfile, capacity: val.capacity })
      : this.svc.createZone(val);
    obs.subscribe({
      next: () => { this.isSaving = false; this.closeModal(); this.showSuccess(this.editId ? 'Zone updated.' : 'Zone created.'); this.loadAll(); },
      error: (e: HttpErrorResponse) => { this.isSaving = false; this.errorMessage = e.error?.message ?? 'Save failed.'; },
    });
  }

  confirmDelete(z: StorageZoneDto) { this.deleteTarget = z; this.showDeleteConfirm = true; }
  cancelDelete() { this.deleteTarget = null; this.showDeleteConfirm = false; }
  doDelete() {
    if (!this.deleteTarget) return;
    this.isDeleting = true;
    this.svc.deleteZone(this.deleteTarget.zoneId).subscribe({
      next: () => { this.isDeleting = false; this.cancelDelete(); this.showSuccess('Zone deleted.'); this.loadAll(); },
      error: (e: HttpErrorResponse) => { this.isDeleting = false; this.errorMessage = e.error?.message ?? 'Delete failed.'; this.cancelDelete(); },
    });
  }

  profileBadge(p?: string) {
    return { Ambient: 'bg-success', Refrigerated: 'bg-info text-dark', Freezer: 'bg-primary' }[p ?? ''] ?? 'bg-secondary';
  }

  countByProfile(profile: string) { return this.zones.filter(z => z.temperatureProfile === profile).length; }

  private showSuccess(msg: string) { this.successMessage = msg; this.errorMessage = ''; setTimeout(() => this.successMessage = '', 3500); }
}
