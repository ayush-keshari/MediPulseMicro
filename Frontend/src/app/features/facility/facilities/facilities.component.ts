import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { FacilityService } from '../../../services/facility/facility.service';
import { FacilityDto } from '../../../services/facility/facility.models';

@Component({
  selector: 'app-facilities',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './facilities.component.html',
  styleUrl: './facilities.component.css',
})
export class FacilitiesComponent implements OnInit {
  facilities: FacilityDto[] = [];
  filtered: FacilityDto[] = [];
  isLoading = false;
  successMessage = '';
  errorMessage = '';
  search = '';

  showModal = false;
  isSaving = false;
  editId: number | null = null;
  form: FormGroup;

  showDeleteConfirm = false;
  deleteTarget: FacilityDto | null = null;
  isDeleting = false;

  facilityTypes = ['Hospital', 'Clinic', 'Warehouse'];

  constructor(private svc: FacilityService, private fb: FormBuilder) {
    this.form = this.fb.group({
      name:   ['', [Validators.required, Validators.maxLength(200)]],
      type:   ['Hospital', Validators.required],
      region: ['', [Validators.required, Validators.maxLength(100)]],
    });
  }

  ngOnInit() { this.load(); }

  load() {
    this.isLoading = true;
    this.svc.getFacilities().subscribe({
      next: (data) => { this.facilities = data; this.applyFilter(); this.isLoading = false; },
      error: () => { this.errorMessage = 'Failed to load facilities.'; this.isLoading = false; },
    });
  }

  applyFilter() {
    const q = this.search.toLowerCase();
    this.filtered = this.facilities.filter(f =>
      !q || f.name.toLowerCase().includes(q) || (f.region ?? '').toLowerCase().includes(q)
    );
  }

  openAdd() { this.editId = null; this.form.reset({ type: 'Hospital' }); this.showModal = true; }
  openEdit(f: FacilityDto) {
    this.editId = f.facilityId;
    this.form.setValue({ name: f.name, type: f.type ?? 'Hospital', region: f.region ?? '' });
    this.showModal = true;
  }
  closeModal() { this.showModal = false; }

  save() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.isSaving = true;
    const val = this.form.value;
    const obs = this.editId
      ? this.svc.updateFacility(this.editId, val)
      : this.svc.createFacility(val);
    obs.subscribe({
      next: () => { this.isSaving = false; this.closeModal(); this.showSuccess(this.editId ? 'Facility updated.' : 'Facility created.'); this.load(); },
      error: (e: HttpErrorResponse) => { this.isSaving = false; this.errorMessage = e.error?.message ?? 'Save failed.'; },
    });
  }

  confirmDelete(f: FacilityDto) { this.deleteTarget = f; this.showDeleteConfirm = true; }
  cancelDelete() { this.deleteTarget = null; this.showDeleteConfirm = false; }
  doDelete() {
    if (!this.deleteTarget) return;
    this.isDeleting = true;
    this.svc.deleteFacility(this.deleteTarget.facilityId).subscribe({
      next: () => { this.isDeleting = false; this.cancelDelete(); this.showSuccess('Facility deleted.'); this.load(); },
      error: (e: HttpErrorResponse) => { this.isDeleting = false; this.errorMessage = e.error?.message ?? 'Delete failed.'; this.cancelDelete(); },
    });
  }

  private showSuccess(msg: string) { this.successMessage = msg; this.errorMessage = ''; setTimeout(() => this.successMessage = '', 3500); }

  countByType(type: string) { return this.facilities.filter(f => f.type === type).length; }

  typeBadge(type?: string) {
    return { Hospital: 'bg-danger', Clinic: 'bg-primary', Warehouse: 'bg-secondary' }[type ?? ''] ?? 'bg-secondary';
  }
}
