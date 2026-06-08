import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuditService } from '../../../services/audit/audit.service';
import { AuditLogDto } from '../../../services/audit/audit.models';

@Component({
  selector: 'app-audit-log',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './audit-log.component.html',
  styleUrl: './audit-log.component.css',
})
export class AuditLogComponent implements OnInit {
  logs: AuditLogDto[] = [];
  isLoading = false;
  errorMessage = '';

  // Filters
  filterUserId     = '';
  filterRole       = '';
  filterMethod     = '';
  filterService    = '';
  filterEntityType = '';
  filterFrom       = '';
  filterTo         = '';

  // Pagination
  page     = 1;
  pageSize = 50;
  total    = 0;
  pages    = 0;

  selectedLog: AuditLogDto | null = null;

  methods  = [ 'POST', 'PUT', 'PATCH', 'DELETE'];
  services = ['AuthService', 'FacilityService', 'InventoryService', 'ProcurementService', 'TelemetryService', 'LogisticsService', 'NotificationService', 'AuditService'];

  constructor(private svc: AuditService) {}

  ngOnInit() { this.load(); }

  load() {
    this.isLoading = true;
    this.svc.getLogs({
      userId:      this.filterUserId     || undefined,
      userRole:    this.filterRole       || undefined,
      httpMethod:  this.filterMethod     || undefined,
      serviceName: this.filterService    || undefined,
      entityType:  this.filterEntityType || undefined,
      from:        this.filterFrom       || undefined,
      to:          this.filterTo         || undefined,
      page:        this.page,
      pageSize:    this.pageSize,
    }).subscribe({
      next: (r) => { this.logs = r.items; this.total = r.total; this.pages = r.pages; this.isLoading = false; },
      error: () => { this.errorMessage = 'Failed to load audit logs.'; this.isLoading = false; },
    });
  }

  applyFilters() { this.page = 1; this.load(); }
  resetFilters() {
    this.filterUserId = ''; this.filterRole = ''; this.filterMethod = '';
    this.filterService = ''; this.filterEntityType = ''; this.filterFrom = ''; this.filterTo = '';
    this.page = 1; this.load();
  }

  goPage(p: number) { if (p >= 1 && p <= this.pages) { this.page = p; this.load(); } }

  methodClass(m: string) {
    return { GET: 'bg-success', POST: 'bg-primary', PUT: 'bg-warning', PATCH: 'bg-info', DELETE: 'bg-danger' }[m] ?? 'bg-secondary';
  }

  statusClass(code: number) {
    if (code < 300) return 'bg-success';
    if (code < 400) return 'bg-info';
    if (code < 500) return 'bg-warning';
    return 'bg-danger';
  }

  get pageNums(): number[] {
    const nums: number[] = [];
    const start = Math.max(1, this.page - 2);
    const end   = Math.min(this.pages, this.page + 2);
    for (let i = start; i <= end; i++) nums.push(i);
    return nums;
  }
}
