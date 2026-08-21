import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { TelemetryService } from '../../../services/telemetry/telemetry.service';
import { SensorDeviceDto } from '../../../services/telemetry/telemetry.models';
import { FacilityService } from '../../../services/facility/facility.service';
import { StorageZoneDto } from '../../../services/facility/facility.models';

@Component({
  selector: 'app-sensor-devices',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './sensor-devices.component.html',
  styleUrl: './sensor-devices.component.css',
})
export class SensorDevicesComponent implements OnInit {
  sensors: SensorDeviceDto[] = [];
  filtered: SensorDeviceDto[] = [];
  isLoading = false;
  successMessage = ''; errorMessage = '';
  search = ''; filterStatus = '';

  showModal = false; isSaving = false; editId: number | null = null;
  form: FormGroup;
  showDeleteConfirm = false; deleteTarget: SensorDeviceDto | null = null; isDeleting = false;

  zones: StorageZoneDto[] = [];

  deviceTypes = ['Temp', 'Humidity', 'GPS'];
  statuses     = ['Active', 'Inactive', 'Faulty'];

  constructor(private svc: TelemetryService, private fb: FormBuilder, private facilitySvc: FacilityService) {
    this.form = this.fb.group({
      deviceName:      ['', Validators.required],
      deviceType:      ['Temp', Validators.required],
      assignedTo:      ['Zone'],
      assignedEntityId:[null, Validators.required],
      status:          ['Active', Validators.required],
    });
  }

  ngOnInit() {
    this.load();
    this.facilitySvc.getZones().subscribe({ next: (z) => this.zones = z });
  }

  load() {
    this.isLoading = true;
    this.svc.getSensors().subscribe({
      next: (d) => { this.sensors = d; this.applyFilter(); this.isLoading = false; },
      error: () => { this.errorMessage = 'Failed to load sensors.'; this.isLoading = false; },
    });
  }

  applyFilter() {
    const q = this.search.toLowerCase();
    this.filtered = this.sensors.filter(s =>
      (!q || s.deviceName.toLowerCase().includes(q) || s.deviceType.toLowerCase().includes(q) || s.assignedTo.toLowerCase().includes(q)) &&
      (!this.filterStatus || s.status === this.filterStatus)
    );
  }

  openAdd() { this.editId = null; this.form.reset({ deviceName: '', deviceType: 'Temp', assignedTo: 'Zone', assignedEntityId: null, status: 'Active' }); this.showModal = true; }
  openEdit(s: SensorDeviceDto) {
    this.editId = s.sensorId;
    this.form.setValue({ deviceName: s.deviceName, deviceType: s.deviceType, assignedTo: s.assignedTo, assignedEntityId: s.assignedEntityId ?? null, status: s.status });
    this.showModal = true;
  }
  closeModal() { this.showModal = false; }

  save() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.isSaving = true;
    const v = this.form.value;
    const payload = { ...v, assignedEntityId: v.assignedEntityId || undefined };
    const obs = this.editId ? this.svc.updateSensor(this.editId, payload) : this.svc.createSensor(payload);
    obs.subscribe({
      next: () => { this.isSaving = false; this.closeModal(); this.showSuccess(this.editId ? 'Sensor updated.' : 'Sensor created.'); this.load(); },
      error: (e: HttpErrorResponse) => { this.isSaving = false; this.errorMessage = e.error?.message ?? 'Save failed.'; },
    });
  }

  confirmDelete(s: SensorDeviceDto) { this.deleteTarget = s; this.showDeleteConfirm = true; }
  cancelDelete() { this.deleteTarget = null; this.showDeleteConfirm = false; }
  doDelete() {
    if (!this.deleteTarget) return;
    this.isDeleting = true;
    this.svc.deleteSensor(this.deleteTarget.sensorId).subscribe({
      next: () => { this.isDeleting = false; this.cancelDelete(); this.showSuccess('Sensor deleted.'); this.load(); },
      error: (e: HttpErrorResponse) => { this.isDeleting = false; this.errorMessage = e.error?.message ?? 'Delete failed.'; this.cancelDelete(); },
    });
  }

  statusBadge(s: string) { return { Active: 'bg-success', Inactive: 'bg-secondary', Faulty: 'bg-danger', Maintenance: 'bg-warning text-dark' }[s] ?? 'bg-secondary'; }
  typeBadge(t: string) { return { Temp: 'bg-primary', Humidity: 'bg-info text-dark', GPS: 'bg-warning text-dark' }[t] ?? 'bg-secondary'; }
  countByStatus(status: string) { return this.sensors.filter(s => s.status === status).length; }
  private showSuccess(msg: string) { this.successMessage = msg; this.errorMessage = ''; setTimeout(() => this.successMessage = '', 3500); }
}
