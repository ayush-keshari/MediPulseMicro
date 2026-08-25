-- ============================================================================
-- MediPulseMicro - Audit Service Mock Data
-- ============================================================================

-- Insert AuditLog Mock Data
INSERT INTO [AuditLog] ([UserId], [UserName], [UserRole], [HttpMethod], [Endpoint], [EntityType], [EntityId], [ServiceName], [StatusCode], [Timestamp], [Details]) VALUES
('user-manager-01', 'Rajesh Kumar', 'Admin', 'POST', '/api/suppliers', 'Supplier', '1', 'ProcurementService', 201, DATEADD(DAY, -7, GETUTCDATE()), 'Created new supplier: Cipla Limited'),
('user-warehouse-01', 'Priya Singh', 'WarehouseManager', 'GET', '/api/inventory/items', 'Item', NULL, 'InventoryService', 200, DATEADD(DAY, -5, GETUTCDATE()), 'Retrieved all items'),
('user-nurse-02', 'Amit Patel', 'Nurse', 'POST', '/api/consumption', 'ConsumptionRecord', '1', 'LogisticsService', 201, DATEADD(DAY, -3, GETUTCDATE()), 'Logged consumption: Insulin 15 units'),
('user-compliance-01', 'Sophia Johnson', 'ComplianceOfficer', 'GET', '/api/audit', NULL, NULL, 'AuditService', 200, DATEADD(DAY, -2, GETUTCDATE()), 'Audit report generated'),
('user-manager-02', 'Rajesh Kumar', 'Admin', 'PUT', '/api/purchase-orders/3', 'PurchaseOrder', '3', 'ProcurementService', 200, DATEADD(DAY, -1, GETUTCDATE()), 'Updated PO status to Approved'),
('user-warehouse-02', 'Neha Gupta', 'WarehouseStaff', 'POST', '/api/receipts', 'Receipt', '1', 'ProcurementService', 201, GETUTCDATE(), 'GRN created for PO#1'),
('user-logistics-01', 'Mohammed Hassan', 'LogisticsManager', 'POST', '/api/transfer-orders', 'TransferOrder', '1', 'LogisticsService', 201, DATEADD(HOUR, -12, GETUTCDATE()), 'Transfer order created from Hub to Apollo');