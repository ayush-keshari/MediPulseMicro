import { Component, OnInit, OnDestroy, HostListener, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth/auth.service';
import { CurrentUser } from '../../services/auth/auth.models';
import { getRoleDisplayName } from '../../shared/extensions/app.extensions';
import { NotificationService } from '../../services/notification/notification.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css',
})
export class HeaderComponent implements OnInit, OnDestroy {
  currentUser: CurrentUser | null = null;
  activeDropdown: string | null = null;
  showMobileMenu = false;
  unreadCount = 0;
  private pollInterval: any;

  constructor(
    private authService: AuthService,
    private router: Router,
    private notificationSvc: NotificationService,
    private ngZone: NgZone,
  ) {}

  ngOnInit(): void {
    this.authService.currentUser$.subscribe((user) => {
      this.currentUser = user;
      if (user) {
        this.loadUnreadCount();
        // Run interval outside Angular zone — prevents change detection on every tick.
        // ngZone.run() is called only inside the HTTP callback when data actually arrives.
        this.ngZone.runOutsideAngular(() => {
          this.pollInterval = setInterval(() => this.loadUnreadCount(), 30_000);
        });
      } else {
        clearInterval(this.pollInterval);
        this.unreadCount = 0;
      }
    });
  }

  ngOnDestroy(): void { clearInterval(this.pollInterval); }

  private loadUnreadCount() {
    this.notificationSvc.getUnreadCount().subscribe({
      next: (r) => this.unreadCount = r.count,
      error: () => {},
    });
  }

  // ── Role flags ──────────────────────────────────────────────────────────
  get role(): string { return this.currentUser?.role ?? ''; }
  get isAdmin()       { return this.role === 'Admin'; }
  get isSupply()      { return this.role === 'SupplyManager'; }
  get isPharmacy()    { return this.role === 'PharmacyManager'; }
  get isProcurement() { return this.role === 'ProcurementOfficer'; }
  get isColdChain()   { return this.role === 'ColdChainOperator'; }
  get isBiomedical()  { return this.role === 'DeviceManager'; }
  get isNursing()     { return this.role === 'Nurse'; }
  get isCompliance()  { return this.role === 'ComplianceOfficer'; }

  // ── Permission getters — mirror backend controller [RoleAuthorize] attributes ──
  // FacilitiesController: all 8 roles
  get canSeeFacilities()  { return this.isAdmin || this.isSupply || this.isPharmacy || this.isProcurement || this.isColdChain || this.isBiomedical || this.isCompliance || this.isNursing; }
  // StorageZonesController: Admin, Supply, ColdChain, Compliance, Nurse
  get canSeeZones()       { return this.isAdmin || this.isSupply || this.isColdChain || this.isCompliance || this.isNursing; }
  // SuppliersController: Admin, Supply, Procurement, Compliance
  get canSeeSuppliers()   { return this.isAdmin || this.isSupply || this.isProcurement || this.isCompliance; }
  // ItemsController + InventoryController: JwtAuth (all authenticated roles)
  get canSeeItems()          { return !!this.currentUser; }
  get canSeeStockPositions() { return !!this.currentUser; }
  get canSeeMasterData()     { return this.canSeeFacilities || this.canSeeSuppliers || this.canSeeZones || this.canSeeItems; }

  // PurchaseOrdersController: Admin, Supply, Procurement, Compliance
  get canSeePOs()         { return this.isAdmin || this.isSupply || this.isProcurement || this.isCompliance; }
  // ReceiptsController: Admin, Supply, Pharmacy, Procurement, Compliance
  get canSeeReceipts()    { return this.isAdmin || this.isSupply || this.isPharmacy || this.isProcurement || this.isCompliance; }
  get canSeeProcurement() { return this.canSeePOs || this.canSeeReceipts; }

  // ExceptionsController: Admin, Supply, Pharmacy, Device, Compliance
  get canSeeExceptions()    { return this.isAdmin || this.isSupply || this.isPharmacy || this.isBiomedical || this.isCompliance; }
  // ReplenishmentController: Admin, Supply, Pharmacy, Procurement
  get canSeeReplenishment() { return this.isAdmin || this.isSupply || this.isPharmacy || this.isProcurement; }
  get canSeeInventory()     { return this.canSeeItems || this.canSeeStockPositions || this.canSeeExceptions || this.canSeeReplenishment; }

  // SensorDevicesController: Admin, Supply, ColdChain
  get canSeeSensors()       { return this.isAdmin || this.isSupply || this.isColdChain; }
  // TelemetryRecordsController: Admin, Supply, ColdChain, Compliance
  get canSeeTelemetryData() { return this.isAdmin || this.isSupply || this.isColdChain || this.isCompliance; }
  get canSeeColdChain()     { return this.canSeeSensors || this.canSeeTelemetryData; }

  // TransferOrdersController: Admin, Supply, Procurement, Device
  get canSeeTransfers()   { return this.isAdmin || this.isSupply || this.isProcurement || this.isBiomedical; }
  // ConsumptionController: Admin, Supply, Pharmacy, Nurse
  get canSeeConsumption() { return this.isAdmin || this.isSupply || this.isPharmacy || this.isNursing; }
  get canSeeDistrib()     { return this.canSeeTransfers || this.canSeeConsumption; }

  // AuditController: Admin, ComplianceOfficer
  get canSeeAudit()       { return this.isAdmin || this.isCompliance; }

  get roleDisplayName(): string { return this.currentUser ? getRoleDisplayName(this.currentUser.role) : ''; }
  get dashboardRoute(): string  { return this.isAdmin ? '/admin/dashboard' : '/dashboard'; }

  // ── Dropdown control ────────────────────────────────────────────────────
  toggleDropdown(name: string, event: MouseEvent): void {
    event.stopPropagation();
    this.activeDropdown = this.activeDropdown === name ? null : name;
  }
  isOpen(name: string): boolean { return this.activeDropdown === name; }

  toggleMobileMenu(): void { this.showMobileMenu = !this.showMobileMenu; }

  @HostListener('document:click')
  onDocumentClick(): void { this.activeDropdown = null; }

  // ── Navigation ──────────────────────────────────────────────────────────
  navigate(route: string): void {
    this.activeDropdown = null;
    this.showMobileMenu = false;
    this.router.navigate([route]);
  }

  logout(): void { this.authService.logout(); }
}
