import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { TelemetryService } from '../../../services/telemetry/telemetry.service';
import { TelemetryRecordDto, SensorDeviceDto } from '../../../services/telemetry/telemetry.models';
import { FacilityService } from '../../../services/facility/facility.service';
import { StorageZoneDto } from '../../../services/facility/facility.models';

@Component({
  selector: 'app-telemetry-records',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './telemetry-records.component.html',
  styleUrl: './telemetry-records.component.css',
})
export class TelemetryRecordsComponent implements OnInit {
  records: TelemetryRecordDto[] = [];
  filtered: TelemetryRecordDto[] = [];
  sensors: SensorDeviceDto[] = [];
  zones: StorageZoneDto[] = [];
  isLoading = false;
  successMessage = ''; errorMessage = '';
  search = ''; filterExcursion = '';

  showModal = false; isSaving = false; editId: number | null = null;
  form: FormGroup;
  showDeleteConfirm = false; deleteTarget: TelemetryRecordDto | null = null; isDeleting = false;

  constructor(private svc: TelemetryService, private fb: FormBuilder, private facilitySvc: FacilityService) {
    this.form = this.fb.group({
      sensorId:    [null, Validators.required],
      timestamp:   [this.nowLocal(), Validators.required],
      temperature: [null],
      humidity:    [null],
      location:    [''],
    });

    // Auto-fill location when sensor changes
    this.form.get('sensorId')!.valueChanges.subscribe((id: number | null) => {
      if (!id) return;
      const sensor = this.sensors.find(s => s.sensorId === +id);
      if (!sensor?.assignedEntityId) return;
      const zone = this.zones.find(z => z.zoneId === sensor.assignedEntityId);
      if (zone?.name) this.form.patchValue({ location: zone.name }, { emitEvent: false });
    });
  }

  ngOnInit() { this.loadAll(); }

  loadAll() {
    this.isLoading = true;
    this.svc.getRecords().subscribe({
      next: (d) => { this.records = d; this.applyFilter(); this.isLoading = false; },
      error: () => { this.errorMessage = 'Failed to load telemetry records.'; this.isLoading = false; },
    });
    this.svc.getSensors().subscribe({ next: (s) => this.sensors = s });
    this.facilitySvc.getZones().subscribe({ next: (z) => this.zones = z });
  }

  applyFilter() {
    const q = this.search.toLowerCase();
    this.filtered = this.records.filter(r =>
      (!q || String(r.sensorId).includes(q) || r.deviceType.toLowerCase().includes(q) || (r.location ?? '').toLowerCase().includes(q)) &&
      (!this.filterExcursion || (this.filterExcursion === 'yes' ? r.isExcursion : !r.isExcursion))
    );
  }

  openAdd() { this.editId = null; this.form.reset({ timestamp: this.nowLocal() }); this.showModal = true; }
  openEdit(r: TelemetryRecordDto) {
    this.editId = r.telemetryId;
    this.form.setValue({
      sensorId:    r.sensorId,
      timestamp:   this.toLocalInput(r.timestamp),
      temperature: r.temperature ?? null,
      humidity:    r.humidity ?? null,
      location:    r.location ?? '',
    });
    this.showModal = true;
  }
  closeModal() { this.showModal = false; }

  save() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.isSaving = true;
    const v = this.form.value;
    const obs = this.editId
      ? this.svc.updateRecord(this.editId, {
          timestamp:   v.timestamp,
          temperature: v.temperature ?? undefined,
          humidity:    v.humidity    ?? undefined,
          location:    v.location   || undefined,
        })
      : this.svc.createRecord({
          sensorId:    v.sensorId,
          timestamp:   v.timestamp,
          temperature: v.temperature ?? undefined,
          humidity:    v.humidity    ?? undefined,
          location:    v.location   || undefined,
        });
    obs.subscribe({
      next: () => { this.isSaving = false; this.closeModal(); this.showSuccess(this.editId ? 'Record updated.' : 'Record created.'); this.loadAll(); },
      error: (e: HttpErrorResponse) => { this.isSaving = false; this.errorMessage = e.error?.message ?? 'Save failed.'; },
    });
  }

  confirmDelete(r: TelemetryRecordDto) { this.deleteTarget = r; this.showDeleteConfirm = true; }
  cancelDelete() { this.deleteTarget = null; this.showDeleteConfirm = false; }
  doDelete() {
    if (!this.deleteTarget) return;
    this.isDeleting = true;
    this.svc.deleteRecord(this.deleteTarget.telemetryId).subscribe({
      next: () => { this.isDeleting = false; this.cancelDelete(); this.showSuccess('Record deleted.'); this.loadAll(); },
      error: (e: HttpErrorResponse) => { this.isDeleting = false; this.errorMessage = e.error?.message ?? 'Delete failed.'; this.cancelDelete(); },
    });
  }

  excursionBadge(isExcursion: boolean) { return isExcursion ? 'bg-danger' : 'bg-success'; }
  typeBadge(t: string) { return { Temp: 'bg-primary', Humidity: 'bg-info text-dark', GPS: 'bg-warning text-dark' }[t] ?? 'bg-secondary'; }

  private nowLocal() {
    const d = new Date();
    d.setMinutes(d.getMinutes() - d.getTimezoneOffset());
    return d.toISOString().slice(0, 16);
  }
  private toLocalInput(iso: string) {
    const d = new Date(iso);
    d.setMinutes(d.getMinutes() - d.getTimezoneOffset());
    return d.toISOString().slice(0, 16);
  }
  private showSuccess(msg: string) { this.successMessage = msg; this.errorMessage = ''; setTimeout(() => this.successMessage = '', 3500); }
}
