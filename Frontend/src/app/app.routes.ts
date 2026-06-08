
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

  // Facility — FacilitiesController: all 8 roles | StorageZonesController: Admin, Supply, ColdChain, Compliance, Nurse
  {
    path: 'facility',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin', 'SupplyManager', 'PharmacyManager', 'DeviceManager', 'ProcurementOfficer', 'ColdChainOperator', 'ComplianceOfficer', 'Nurse'] },
    children: [
      {
        path: 'facilities',
        loadComponent: () => import('./features/facility/facilities/facilities.component').then(m => m.FacilitiesComponent),
      },
      {
        path: 'storage-zones',
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'SupplyManager', 'ColdChainOperator', 'ComplianceOfficer', 'Nurse'] },
        loadComponent: () => import('./features/facility/storage-zones/storage-zones.component').then(m => m.StorageZonesComponent),
      },
      { path: '', redirectTo: 'facilities', pathMatch: 'full' },
    ],
  },

  // Procurement — SuppliersController & PurchaseOrdersController: Admin, Supply, Procurement, Compliance
  //              ReceiptsController: Admin, Supply, Pharmacy, Procurement, Compliance
  {
    path: 'procurement',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin', 'SupplyManager', 'PharmacyManager', 'ProcurementOfficer', 'ComplianceOfficer'] },
    children: [
      {
        path: 'suppliers',
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'SupplyManager', 'ProcurementOfficer', 'ComplianceOfficer'] },
        loadComponent: () => import('./features/procurement/suppliers/suppliers.component').then(m => m.SuppliersComponent),
      },
      {
        path: 'purchase-orders',
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'SupplyManager', 'ProcurementOfficer', 'ComplianceOfficer'] },
        loadComponent: () => import('./features/procurement/purchase-orders/purchase-orders.component').then(m => m.PurchaseOrdersComponent),
      },
      {
        path: 'receipts',
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'SupplyManager', 'PharmacyManager', 'ProcurementOfficer', 'ComplianceOfficer'] },
        loadComponent: () => import('./features/procurement/receipts/receipts.component').then(m => m.ReceiptsComponent),
      },
      { path: '', redirectTo: 'suppliers', pathMatch: 'full' },
    ],
  },

  // Telemetry — SensorDevicesController: Admin, Supply, ColdChain
  //             TelemetryRecordsController: Admin, Supply, ColdChain, Compliance
  {
    path: 'telemetry',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin', 'SupplyManager', 'ColdChainOperator', 'ComplianceOfficer'] },
    children: [
      {
        path: 'sensors',
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'SupplyManager', 'ColdChainOperator'] },
        loadComponent: () => import('./features/telemetry/sensor-devices/sensor-devices.component').then(m => m.SensorDevicesComponent),
      },
      {
        path: 'records',
        loadComponent: () => import('./features/telemetry/telemetry-records/telemetry-records.component').then(m => m.TelemetryRecordsComponent),
      },
      { path: '', redirectTo: 'sensors', pathMatch: 'full' },
    ],
  },

  // Inventory — Items/StockPositions: JwtAuth (all roles)
  //             ExceptionsController: Admin, Supply, Pharmacy, Device, Compliance
  //             ReplenishmentController: Admin, Supply, Pharmacy, Procurement
  {
    path: 'inventory',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin', 'SupplyManager', 'PharmacyManager', 'DeviceManager', 'ProcurementOfficer', 'ColdChainOperator', 'ComplianceOfficer', 'Nurse'] },
    children: [
      {
        path: 'items',
        loadComponent: () => import('./features/inventory/items/items.component').then(m => m.ItemsComponent),
      },
      {
        path: 'stock-positions',
        loadComponent: () => import('./features/inventory/stock-positions/stock-positions.component').then(m => m.StockPositionsComponent),
      },
      {
        path: 'exceptions',
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'SupplyManager', 'PharmacyManager', 'DeviceManager', 'ComplianceOfficer'] },
        loadComponent: () => import('./features/inventory/exceptions/exceptions.component').then(m => m.ExceptionsComponent),
      },
      {
        path: 'replenishment',
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'SupplyManager', 'PharmacyManager', 'ProcurementOfficer'] },
        loadComponent: () => import('./features/inventory/replenishment/replenishment.component').then(m => m.ReplenishmentComponent),
      },
      { path: '', redirectTo: 'items', pathMatch: 'full' },
    ],
  },

  // Distribution — TransferOrdersController: Admin, Supply, Procurement, Device
  //                ConsumptionController: Admin, Supply, Pharmacy, Nurse
  {
    path: 'distribution',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin', 'SupplyManager', 'PharmacyManager', 'DeviceManager', 'ProcurementOfficer', 'Nurse'] },
    children: [
      {
        path: 'transfer-orders',
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'SupplyManager', 'ProcurementOfficer', 'DeviceManager'] },
        loadComponent: () => import('./features/logistics/transfer-orders/transfer-orders.component').then(m => m.TransferOrdersComponent),
      },
      {
        path: 'consumption',
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'SupplyManager', 'PharmacyManager', 'Nurse'] },
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
