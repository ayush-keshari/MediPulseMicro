import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { InventoryService } from '../../../services/inventory/inventory.service';
import { ExceptionEventDto, RecallActionDto } from '../../../services/inventory/inventory.models';
import { FacilityService } from '../../../services/facility/facility.service';
import { FacilityDto } from '../../../services/facility/facility.models';
import { ItemResponse } from '../../../services/inventory/inventory.models';

@Component({
  selector: 'app-exceptions',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './exceptions.component.html',
  styleUrl: './exceptions.component.css',
})
export class ExceptionsComponent implements OnInit {
  exceptions: ExceptionEventDto[] = [];
  filtered: ExceptionEventDto[] = [];
  isLoading = false;
  successMessage = '';
  errorMessage = '';
  search = '';
  statusFilter = '';
  typeFilter = '';

  facilities: FacilityDto[] = [];
  facilitiesForItem: FacilityDto[] = [];   // filtered when an item is selected
  itemFacilitiesLoading = false;
  items: ItemResponse[] = [];

  // Exception form
  showExcModal = false;
  isSavingExc = false;
  excForm: FormGroup;

  // Status update
  showStatusModal = false;
  statusTarget: ExceptionEventDto | null = null;
  statusForm: FormGroup;
  isSavingStatus = false;

  // Recall action panel
  selectedExc: ExceptionEventDto | null = null;
  showRecallModal = false;
  isSavingRecall = false;
  recallForm: FormGroup;
  editRecallId: number | null = null;

  // Detect
  isDetecting = false;
  detectFacilityId: any = '';

  excTypes    = ['Stockout', 'ExpiryAlert', 'Excursion', 'Recall'];
  severities  = ['Low', 'Medium', 'High'];
  statuses    = ['Open', 'InProgress', 'Resolved', 'Dismissed'];
  refTypes    = ['Item', 'Lot', 'TransferOrder', 'Other'];

  constructor(
    private svc: InventoryService,
    private facilitySvc: FacilityService,
    private fb: FormBuilder,
  ) {
    this.excForm = this.fb.group({
      type:          ['Stockout', Validators.required],
      referenceType: ['Item', Validators.required],
      referenceId:   [0, [Validators.required, Validators.min(0)]],
      itemId:        [''],
      facilityId:    [''],
      lotId:         [''],
      severity:      ['Medium', Validators.required],
    });

    this.statusForm = this.fb.group({
      status: ['', Validators.required],
    });

    this.recallForm = this.fb.group({
      ownerId:           ['', Validators.required],
      actionDescription: ['', [Validators.required, Validators.maxLength(500)]],
      dueDate:           ['', Validators.required],
    });
  }

  ngOnInit() {
    this.load();
    this.facilitySvc.getFacilities().subscribe({ next: (d) => this.facilities = d });
    this.svc.getItems().subscribe({ next: (d) => this.items = d });

    this.excForm.get('itemId')!.valueChanges.subscribe(itemId => {
      this.onItemChange(itemId);
    });
  }

  onItemChange(itemId: any) {
    this.excForm.get('facilityId')!.setValue('', { emitEvent: false });
    if (!itemId) {
      this.facilitiesForItem = [];
      return;
    }
    this.itemFacilitiesLoading = true;
    this.svc.getFacilitiesByItem(+itemId).subscribe({
      next: (ids) => {
        this.facilitiesForItem = this.facilities.filter(f => ids.includes(f.facilityId));
        this.itemFacilitiesLoading = false;
      },
      error: () => { this.facilitiesForItem = []; this.itemFacilitiesLoading = false; },
    });
  }

  get displayFacilities(): FacilityDto[] {
    const itemId = this.excForm.get('itemId')?.value;
    return itemId ? this.facilitiesForItem : this.facilities;
  }

  load() {
    this.isLoading = true;
    this.svc.getExceptions().subscribe({
      next: (d) => { this.exceptions = d; this.applyFilter(); this.isLoading = false; },
      error: () => { this.errorMessage = 'Failed to load exceptions.'; this.isLoading = false; },
    });
  }

  applyFilter() {
    const q = this.search.toLowerCase();
    this.filtered = this.exceptions.filter(e =>
      (!q || (e.itemName ?? '').toLowerCase().includes(q) || e.type.toLowerCase().includes(q)) &&
      (!this.statusFilter || e.status === this.statusFilter) &&
      (!this.typeFilter   || e.type === this.typeFilter)
    );
  }

  getItemName(id: any): string {
    return this.items.find(i => i.itemId === +id)?.name ?? '';
  }

  // ── Exception CRUD ──────────────────────────────────────────────────────
  openAddExc() { this.excForm.reset({ type: 'Stockout', referenceType: 'Item', severity: 'Medium', referenceId: 0 }); this.facilitiesForItem = []; this.showExcModal = true; }
  closeExcModal() { this.showExcModal = false; this.errorMessage = ''; }

  saveExc() {
    if (this.excForm.invalid) { this.excForm.markAllAsTouched(); return; }
    this.isSavingExc = true;
    const v = this.excForm.value;
    this.svc.createException({
      ...v,
      itemId:     v.itemId     ? +v.itemId     : undefined,
      itemName:   v.itemId     ? this.getItemName(v.itemId) : undefined,
      facilityId: v.facilityId ? +v.facilityId : undefined,
    }).subscribe({
      next: () => { this.isSavingExc = false; this.closeExcModal(); this.showSuccess('Exception created.'); this.load(); },
      error: (e: HttpErrorResponse) => { this.isSavingExc = false; this.errorMessage = e.error?.message ?? 'Save failed.'; },
    });
  }

  // ── Status update ───────────────────────────────────────────────────────
  openStatus(exc: ExceptionEventDto) {
    this.statusTarget = exc;
    this.statusForm.setValue({ status: exc.status });
    this.showStatusModal = true;
  }
  closeStatusModal() { this.showStatusModal = false; this.statusTarget = null; }

  saveStatus() {
    if (this.statusForm.invalid) return;
    this.isSavingStatus = true;
    this.svc.updateExceptionStatus(this.statusTarget!.exceptionId, this.statusForm.value).subscribe({
      next: () => { this.isSavingStatus = false; this.closeStatusModal(); this.showSuccess('Status updated.'); this.load(); },
      error: (e: HttpErrorResponse) => { this.isSavingStatus = false; this.errorMessage = e.error?.message ?? 'Update failed.'; },
    });
  }

  // ── Auto-detect ─────────────────────────────────────────────────────────
  detect() {
    if (!this.detectFacilityId) { this.errorMessage = 'Select a facility first.'; return; }
    this.isDetecting = true;
    this.svc.detectExceptions(+this.detectFacilityId).subscribe({
      next: (r) => { this.isDetecting = false; this.showSuccess(`Detected ${r.totalCreated} new exception(s) (${r.stockoutCount} stockouts, ${r.expiryCount} expiry alerts).`); this.load(); },
      error: () => { this.isDetecting = false; this.errorMessage = 'Detection failed.'; },
    });
  }

  // ── Recall Actions panel ────────────────────────────────────────────────
  openRecallPanel(exc: ExceptionEventDto) { this.selectedExc = exc; }
  closeRecallPanel() { this.selectedExc = null; }

  openAddRecall() {
    this.editRecallId = null;
    this.recallForm.reset();
    this.showRecallModal = true;
  }

  openEditRecall(a: RecallActionDto) {
    this.editRecallId = a.recallActionId;
    this.recallForm.setValue({
      ownerId:           a.ownerId,
      actionDescription: a.actionDescription,
      dueDate:           a.dueDate.substring(0, 10),
    });
    this.showRecallModal = true;
  }

  closeRecallModal() { this.showRecallModal = false; }

  saveRecall() {
    if (this.recallForm.invalid) { this.recallForm.markAllAsTouched(); return; }
    this.isSavingRecall = true;
    const v = this.recallForm.value;
    const obs = this.editRecallId
      ? this.svc.updateRecallAction(this.editRecallId, { actionDescription: v.actionDescription, dueDate: v.dueDate })
      : this.svc.createRecallAction({ ownerId: v.ownerId, actionDescription: v.actionDescription, dueDate: v.dueDate, exceptionId: this.selectedExc!.exceptionId });
    obs.subscribe({
      next: () => {
        this.isSavingRecall = false;
        this.closeRecallModal();
        this.showSuccess(this.editRecallId ? 'Action updated.' : 'Action added.');
        this.svc.getException(this.selectedExc!.exceptionId).subscribe(e => { this.selectedExc = e; this.load(); });
      },
      error: (e: HttpErrorResponse) => { this.isSavingRecall = false; this.errorMessage = e.error?.message ?? 'Save failed.'; },
    });
  }

  deleteRecall(id: number) {
    this.svc.deleteRecallAction(id).subscribe({
      next: () => { this.showSuccess('Action removed.'); this.svc.getException(this.selectedExc!.exceptionId).subscribe(e => { this.selectedExc = e; this.load(); }); },
    });
  }

  severityClass(s: string) { return { High: 'bg-danger', Medium: 'bg-warning', Low: 'bg-info' }[s] ?? 'bg-secondary'; }
  statusClass(s: string)   { return { Open: 'bg-danger', InProgress: 'bg-warning', Resolved: 'bg-success', Dismissed: 'bg-secondary' }[s] ?? 'bg-secondary'; }

  private showSuccess(msg: string) { this.successMessage = msg; this.errorMessage = ''; setTimeout(() => this.successMessage = '', 4000); }

  get openCount()       { return this.exceptions.filter(e => e.status === 'Open').length; }
  get inProgressCount() { return this.exceptions.filter(e => e.status === 'InProgress').length; }
  get highCount()       { return this.exceptions.filter(e => e.severity === 'High').length; }
}
