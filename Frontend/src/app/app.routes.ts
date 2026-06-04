
import { Routes } from '@angular/router';
import { authGuard } from './shared/filters/auth.guard';
import { roleGuard } from './shared/filters/role.guard';

export const routes: Routes = [
  { path: 'login',    loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent) },
  { path: 'register', loadComponent: () => import('./features/auth/register/register.component').then(m => m.RegisterComponent) },
  { path: '',         redirectTo: 'login', pathMatch: 'full' },

  {
    path: 'pending-approval',
    canActivate: [authGuard],
    loadComponent: () => import('./features/auth/pending-approval/pending-approval.component').then(m => m.PendingApprovalComponent),
  },

  // Admin only
  {
    path: 'admin',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin'] },
    children: [
      { path: 'dashboard',  loadComponent: () => import('./features/admin/admin-dashboard/admin-dashboard.component').then(m => m.AdminDashboardComponent) },
      { path: 'users',      loadComponent: () => import('./features/admin/user-management/user-management.component').then(m => m.UserManagementComponent) },
      { path: 'health',     loadComponent: () => import('./features/admin/system-health/system-health.component').then(m => m.SystemHealthComponent) },
      { path: '',           redirectTo: 'dashboard', pathMatch: 'full' },
    ],
  },

  // Shared role dashboard — all authenticated non-Unassigned users
  {
    path: 'dashboard',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin', 'SupplyManager', 'PharmacyManager', 'DeviceManager', 'ProcurementOfficer', 'ColdChainOperator', 'ComplianceOfficer', 'Nurse'] },
    loadComponent: () => import('./features/dashboard/role-dashboard/role-dashboard.component').then(m => m.RoleDashboardComponent),
  },

  // Facility — all roles with facility access (all except Unassigned)
  {
    path: 'facility',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin', 'SupplyManager', 'PharmacyManager', 'DeviceManager', 'ProcurementOfficer', 'ColdChainOperator', 'ComplianceOfficer', 'Nurse'] },
    children: [
      { path: 'facilities',    loadComponent: () => import('./features/facility/facilities/facilities.component').then(m => m.FacilitiesComponent) },
      { path: 'storage-zones', loadComponent: () => import('./features/facility/storage-zones/storage-zones.component').then(m => m.StorageZonesComponent) },
      { path: '',              redirectTo: 'facilities', pathMatch: 'full' },
    ],
  },

  // Procurement — Admin, ProcurementOfficer, SupplyManager (suppliers view), ComplianceOfficer (suppliers view)
  {
    path: 'procurement',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin', 'SupplyManager', 'ProcurementOfficer', 'ComplianceOfficer'] },
    children: [
      // Suppliers: Admin, SupplyManager, ProcurementOfficer, ComplianceOfficer
      {
        path: 'suppliers',
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'SupplyManager', 'ProcurementOfficer', 'ComplianceOfficer'] },
        loadComponent: () => import('./features/procurement/suppliers/suppliers.component').then(m => m.SuppliersComponent),
      },
      // Purchase orders: Admin, ProcurementOfficer
      {
        path: 'purchase-orders',
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'ProcurementOfficer'] },
        loadComponent: () => import('./features/procurement/purchase-orders/purchase-orders.component').then(m => m.PurchaseOrdersComponent),
      },
      // Receipts (receive stock): Admin, ProcurementOfficer
      {
        path: 'receipts',
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'ProcurementOfficer'] },
        loadComponent: () => import('./features/procurement/receipts/receipts.component').then(m => m.ReceiptsComponent),
      },
      { path: '', redirectTo: 'suppliers', pathMatch: 'full' },
    ],
  },

  // Telemetry — Admin, DeviceManager, ColdChainOperator
  {
    path: 'telemetry',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin', 'DeviceManager', 'ColdChainOperator'] },
    children: [
      { path: 'sensors', loadComponent: () => import('./features/telemetry/sensor-devices/sensor-devices.component').then(m => m.SensorDevicesComponent) },
      { path: 'records', loadComponent: () => import('./features/telemetry/telemetry-records/telemetry-records.component').then(m => m.TelemetryRecordsComponent) },
      { path: '',        redirectTo: 'sensors', pathMatch: 'full' },
    ],
  },

  // Inventory — Admin, SupplyManager, PharmacyManager, Nurse (+ ProcurementOfficer for stock-positions)
  {
    path: 'inventory',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin', 'SupplyManager', 'PharmacyManager', 'ProcurementOfficer', 'Nurse'] },
    children: [
      // Items (full inventory view): Admin, SupplyManager, PharmacyManager, Nurse
      {
        path: 'items',
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'SupplyManager', 'PharmacyManager', 'Nurse'] },
        loadComponent: () => import('./features/inventory/items/items.component').then(m => m.ItemsComponent),
      },
      // Stock positions: Admin, SupplyManager, PharmacyManager, ProcurementOfficer, Nurse
      {
        path: 'stock-positions',
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'SupplyManager', 'PharmacyManager', 'ProcurementOfficer', 'Nurse'] },
        loadComponent: () => import('./features/inventory/stock-positions/stock-positions.component').then(m => m.StockPositionsComponent),
      },
      // Exceptions & replenishment: Admin, SupplyManager
      {
        path: 'exceptions',
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'SupplyManager'] },
        loadComponent: () => import('./features/inventory/exceptions/exceptions.component').then(m => m.ExceptionsComponent),
      },
      {
        path: 'replenishment',
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'SupplyManager'] },
        loadComponent: () => import('./features/inventory/replenishment/replenishment.component').then(m => m.ReplenishmentComponent),
      },
      { path: '', redirectTo: 'items', pathMatch: 'full' },
    ],
  },

  // Distribution — Admin, SupplyManager, PharmacyManager, ProcurementOfficer, Nurse
  {
    path: 'distribution',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin', 'SupplyManager', 'PharmacyManager', 'ProcurementOfficer', 'Nurse'] },
    children: [
      // Transfer orders: Admin, SupplyManager, ProcurementOfficer
      {
        path: 'transfer-orders',
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'SupplyManager', 'ProcurementOfficer'] },
        loadComponent: () => import('./features/logistics/transfer-orders/transfer-orders.component').then(m => m.TransferOrdersComponent),
      },
      // Consumption tracking: Admin, PharmacyManager, Nurse
      {
        path: 'consumption',
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'PharmacyManager', 'Nurse'] },
        loadComponent: () => import('./features/logistics/consumption/consumption.component').then(m => m.ConsumptionComponent),
      },
      { path: '', redirectTo: 'transfer-orders', pathMatch: 'full' },
    ],
  },

  // Notifications — all authenticated non-Unassigned users
  {
    path: 'notifications',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin', 'SupplyManager', 'PharmacyManager', 'DeviceManager', 'ProcurementOfficer', 'ColdChainOperator', 'ComplianceOfficer', 'Nurse'] },
    loadComponent: () => import('./features/notifications/notifications-page/notifications-page.component').then(m => m.NotificationsPageComponent),
  },

  // Audit log — Admin, ComplianceOfficer
  {
    path: 'audit',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin', 'ComplianceOfficer'] },
    loadComponent: () => import('./features/audit/audit-log/audit-log.component').then(m => m.AuditLogComponent),
  },

  { path: '**', redirectTo: 'login' },
];
