import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { InventoryService } from '../../../services/inventory/inventory.service';
import { ForecastDto, ReplenishmentPlanDto, ItemResponse } from '../../../services/inventory/inventory.models';
import { FacilityService } from '../../../services/facility/facility.service';
import { FacilityDto } from '../../../services/facility/facility.models';

@Component({
  selector: 'app-replenishment',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './replenishment.component.html',
  styleUrl: './replenishment.component.css',
})
export class ReplenishmentComponent implements OnInit {
  plans: ReplenishmentPlanDto[] = [];
  forecasts: ForecastDto[] = [];
  filteredPlans: ReplenishmentPlanDto[] = [];
  isLoading = false;
  successMessage = '';
  errorMessage = '';

  facilities: FacilityDto[] = [];
  items: ItemResponse[] = [];

  activeTab: 'plans' | 'forecasts' = 'plans';
  planStatusFilter = '';
  planPriorityFilter = '';
  generateFacilityId: any = '';
  isGenerating = false;

  // Status update inline
  updatingPlanId: number | null = null;

  statuses   = ['Pending', 'Ordered', 'Fulfilled', 'Cancelled'];
  priorities = ['High', 'Medium', 'Low'];

  constructor(private svc: InventoryService, private facilitySvc: FacilityService) {}

  ngOnInit() {
    this.loadAll();
    this.facilitySvc.getFacilities().subscribe({ next: (d) => this.facilities = d });
    this.svc.getItems().subscribe({ next: (d) => this.items = d });
  }

  getItemName(id: number): string {
    return this.items.find(i => i.itemId === id)?.name ?? `Item #${id}`;
  }

  getFacilityName(id: number): string {
    return this.facilities.find(f => f.facilityId === id)?.name ?? `Facility #${id}`;
  }

  loadAll() {
    this.isLoading = true;
    this.svc.getPlans().subscribe({
      next: (d) => { this.plans = d; this.applyPlanFilter(); this.isLoading = false; },
      error: () => { this.errorMessage = 'Failed to load plans.'; this.isLoading = false; },
    });
    this.svc.getForecasts().subscribe({ next: (d) => this.forecasts = d });
  }

  applyPlanFilter() {
    this.filteredPlans = this.plans.filter(p =>
      (!this.planStatusFilter   || p.status === this.planStatusFilter) &&
      (!this.planPriorityFilter || p.priority === this.planPriorityFilter)
    );
  }

  generate() {
    if (!this.generateFacilityId) { this.errorMessage = 'Select a facility first.'; return; }
    this.isGenerating = true;
    this.svc.generateReplenishment(+this.generateFacilityId).subscribe({
      next: (r) => {
        this.isGenerating = false;
        this.showSuccess(`Generated ${r.plansCreated} plan(s) and ${r.forecastsCreated} forecast(s) for facility ${r.facilityId}.`);
        this.loadAll();
      },
      error: (e: HttpErrorResponse) => { this.isGenerating = false; this.errorMessage = e.error?.message ?? 'Generation failed.'; },
    });
  }

  updateStatus(plan: ReplenishmentPlanDto, status: string) {
    this.updatingPlanId = plan.planId;
    this.svc.updatePlanStatus(plan.planId, { status }).subscribe({
      next: (updated) => {
        this.updatingPlanId = null;
        plan.status = updated.status;
        this.showSuccess(`Plan updated to ${status}.`);
      },
      error: () => { this.updatingPlanId = null; this.errorMessage = 'Status update failed.'; },
    });
  }

  deletePlan(id: number) {
    this.svc.deletePlan(id).subscribe({
      next: () => { this.showSuccess('Plan removed.'); this.loadAll(); },
    });
  }

  priorityClass(p: string) { return { High: 'bg-danger', Medium: 'bg-warning', Low: 'bg-info' }[p] ?? 'bg-secondary'; }
  statusClass(s: string)   { return { Pending: 'bg-warning', Ordered: 'bg-primary', Fulfilled: 'bg-success', Cancelled: 'bg-secondary' }[s] ?? 'bg-secondary'; }

  private showSuccess(msg: string) { this.successMessage = msg; this.errorMessage = ''; setTimeout(() => this.successMessage = '', 4000); }

  get pendingCount()  { return this.plans.filter(p => p.status === 'Pending').length; }
  get highPrioCount() { return this.plans.filter(p => p.priority === 'High').length; }
  get orderedCount()  { return this.plans.filter(p => p.status === 'Ordered').length; }
}
