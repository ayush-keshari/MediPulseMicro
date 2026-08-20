-- ============================================================================
-- MediPulseMicro - Mock Data Inserts Only
-- Use this after EF Core migrations have created the tables
-- ============================================================================

-- ============================================================================
-- 1. FACILITY SERVICE - Mock Data
-- ============================================================================

INSERT INTO [Facility] ([Name], [Type], [Region]) VALUES
('Apollo Hospital Delhi', 'Hospital', 'North'),
('Max Healthcare Mumbai', 'Hospital', 'West'),
('Regional Medical Center Chennai', 'Hospital', 'South'),
('Fortis Healthcare Bangalore', 'Hospital', 'South'),
('Central Distribution Hub Delhi', 'Distribution Center', 'North'),
('Eastern Medical Clinic Kolkata', 'Clinic', 'East');

INSERT INTO [StorageZone] ([FacilityID], [Name], [TemperatureProfile], [Capacity]) VALUES
(1, 'Cold Storage A1', 'Refrigerated', 5000.00),
(1, 'Cold Storage A2', 'Freezer', 3000.00),
(1, 'Ambient Storage A3', 'Ambient', 10000.00),
(2, 'Cold Room B1', 'Refrigerated', 4500.00),
(2, 'Freezer B2', 'Freezer', 2500.00),
(3, 'Storage Zone C1', 'Ambient', 8000.00),
(4, 'Premium Cold Storage D1', 'Refrigerated', 6000.00),
(5, 'Hub Storage E1', 'Ambient', 50000.00),
(6, 'Clinic Storage F1', 'Ambient', 2000.00);

-- ============================================================================
-- 2. INVENTORY SERVICE - Mock Data
-- ============================================================================

INSERT INTO [Item] ([ItemCode], [Name], [Category], [Unit], [StorageRequirement], [SafetyStock]) VALUES
('MED-001', 'Insulin Vial 100IU', 'Pharma', 'Box', 'Refrigerated', 50),
('MED-002', 'Amoxicillin 500mg Tablet', 'Pharma', 'Box', 'Ambient', 100),
('MED-003', 'Saline Solution 0.9%', 'Pharma', 'Box', 'Ambient', 200),
('MED-004', 'Surgical Mask N95', 'Consumable', 'Box', 'Ambient', 500),
('MED-005', 'IV Cannula 20G', 'Device', 'Box', 'Ambient', 300),
('MED-006', 'Vaccine Pfizer Vial', 'Pharma', 'Vial', 'Freezer', 25),
('MED-007', 'Paracetamol 500mg', 'Pharma', 'Box', 'Ambient', 150),
('MED-008', 'Blood Pressure Monitor', 'Device', 'Piece', 'Ambient', 10);

INSERT INTO [InventoryPositions] ([ItemId], [LotId], [ExpiryDate], [Quantity], [FacilityId], [StorageZoneId], [SafetyStock]) VALUES
(1, 'LOT-INS-2026-001', '2027-06-30', 150, 1, 1, 50),
(1, 'LOT-INS-2026-002', '2027-12-31', 200, 1, 1, 50),
(2, 'LOT-AMX-2026-001', '2028-02-15', 500, 1, 3, 100),
(3, 'LOT-SAL-2026-001', '2027-08-20', 1000, 2, 4, 200),
(4, 'LOT-MSK-2026-001', '2027-12-31', 2000, 1, 3, 500),
(5, 'LOT-IVC-2026-001', '2027-10-10', 800, 2, 4, 300),
(6, 'LOT-VAC-2026-001', '2027-03-15', 100, 1, 2, 25),
(7, 'LOT-PAR-2026-001', '2028-01-25', 600, 3, 6, 150),
(8, 'LOT-BPM-2026-001', '2029-12-31', 30, 4, 7, 10);

INSERT INTO [ExceptionEvent] ([Type], [ReferenceType], [ReferenceId], [ItemId], [ItemName], [FacilityId], [LotId], [Severity], [Status], [DetectedDate]) VALUES
('Stockout', 'InventoryPosition', 4, 3, 'Saline Solution 0.9%', 2, 'LOT-SAL-2026-001', 'High', 'Open', GETUTCDATE()),
('ExpiryAlert', 'InventoryPosition', 7, 6, 'Vaccine Pfizer Vial', 1, 'LOT-VAC-2026-001', 'High', 'InProgress', DATEADD(DAY, -20, GETUTCDATE())),
('Excursion', 'Telemetry', 5, 1, 'Insulin Vial 100IU', 1, 'LOT-INS-2026-001', 'High', 'Open', DATEADD(DAY, -5, GETUTCDATE())),
('Recall', 'Supplier', 1, 2, 'Amoxicillin 500mg Tablet', 1, 'LOT-AMX-2026-001', 'Medium', 'Open', DATEADD(DAY, -2, GETUTCDATE()));

INSERT INTO [RecallAction] ([ExceptionId], [OwnerId], [ActionDescription], [DueDate], [Status]) VALUES
(1, 'user-101', 'Urgent replenishment needed for Apollo Delhi', DATEADD(DAY, 3, GETUTCDATE()), 'InProgress'),
(2, 'user-102', 'Initiate cold chain recall for expired vaccines', DATEADD(DAY, 7, GETUTCDATE()), 'Pending'),
(3, 'user-103', 'Investigate temperature excursion in zone A1', DATEADD(DAY, 2, GETUTCDATE()), 'Pending'),
(4, 'user-104', 'Quarantine and destroy recalled batch', DATEADD(DAY, 5, GETUTCDATE()), 'Pending');

INSERT INTO [Forecast] ([ItemId], [FacilityId], [Period], [ForecastQuantity], [GeneratedDate]) VALUES
(1, 1, '2026-06', 300, GETUTCDATE()),
(2, 1, '2026-06', 600, GETUTCDATE()),
(3, 2, '2026-06', 500, GETUTCDATE()),
(4, 1, '2026-06', 1500, GETUTCDATE()),
(5, 2, '2026-06', 400, GETUTCDATE()),
(7, 3, '2026-06', 800, GETUTCDATE());

INSERT INTO [ReplenishmentPlan] ([ItemId], [FacilityId], [SuggestedOrderQty], [Priority], [Status], [GeneratedDate]) VALUES
(1, 1, 200, 'High', 'Pending', GETUTCDATE()),
(3, 2, 500, 'High', 'Pending', GETUTCDATE()),
(4, 1, 1000, 'Medium', 'Pending', GETUTCDATE()),
(2, 1, 400, 'Medium', 'Pending', GETUTCDATE());

-- ============================================================================
-- 3. PROCUREMENT SERVICE - Mock Data
-- ============================================================================

INSERT INTO [Supplier] ([Name], [SupplierType], [Status]) VALUES
('Cipla Limited', 'Manufacturer', 'Active'),
('Lupin Pharmaceuticals', 'Manufacturer', 'Active'),
('Mankind Pharma', 'Manufacturer', 'Active'),
('GlaxoSmithKline India', 'Manufacturer', 'Active'),
('MedExpress Distributor', 'Distributor', 'Active'),
('PharmaCold Logistics', '3PL', 'Active'),
('Dr. Reddy Laboratories', 'Manufacturer', 'OnHold');

INSERT INTO [PurchaseOrder] ([SupplierID], [OrderDate], [ExpectedDeliveryDate], [Status], [Notes]) VALUES
(1, '2026-05-15', '2026-06-05', 'Approved', 'Insulin supply for Apollo Network'),
(2, '2026-05-18', '2026-06-08', 'Shipped', 'Amoxicillin restocking'),
(3, '2026-05-20', '2026-06-10', 'PartiallyReceived', 'Surgical supplies bundle'),
(4, '2026-05-25', '2026-06-15', 'Draft', 'Vaccine procurement'),
(1, '2026-06-01', '2026-06-20', 'Submitted', 'Emergency replenishment'),
(5, '2026-05-28', '2026-06-05', 'FullyReceived', 'General consumables');

INSERT INTO [Receipt] ([POID], [SupplierLot], [ReceivedDate], [ReceivedBy], [QualityStatus], [QuantityReceived]) VALUES
(1, 'LOT-INS-2026-001', '2026-06-01', 'user-warehouse-01', 'Accepted', 200),
(2, 'LOT-AMX-2026-001', '2026-06-08', 'user-warehouse-02', 'Accepted', 500),
(3, 'LOT-SUR-2026-001', '2026-06-09', 'user-warehouse-01', 'Accepted', 300),
(3, 'LOT-SUR-2026-002', '2026-06-12', 'user-warehouse-02', 'OnHold', 150),
(6, 'LOT-CON-2026-001', '2026-06-04', 'user-warehouse-03', 'Accepted', 1000),
(1, 'LOT-INS-2026-002', '2026-06-05', 'user-warehouse-01', 'Accepted', 150);

-- ============================================================================
-- 4. LOGISTICS SERVICE - Mock Data
-- ============================================================================

INSERT INTO [TransferOrder] ([FromFacilityId], [FromFacilityName], [ToFacilityId], [ToFacilityName], [RequestedBy], [RequestedDate], [Status]) VALUES
(5, 'Central Distribution Hub Delhi', 1, 'Apollo Hospital Delhi', 'user-manager-01', DATEADD(DAY, -5, GETUTCDATE()), 'Completed'),
(1, 'Apollo Hospital Delhi', 2, 'Max Healthcare Mumbai', 'user-manager-02', DATEADD(DAY, -2, GETUTCDATE()), 'InTransit'),
(5, 'Central Distribution Hub Delhi', 3, 'Regional Medical Center Chennai', 'user-manager-03', DATEADD(HOUR, -12, GETUTCDATE()), 'Approved'),
(2, 'Max Healthcare Mumbai', 4, 'Fortis Healthcare Bangalore', 'user-manager-01', GETUTCDATE(), 'Draft');

INSERT INTO [TransferOrderItem] ([TransferOrderId], [ItemId], [ItemName], [Quantity], [ToStorageZoneId]) VALUES
(1, 1, 'Insulin Vial 100IU', 100, 4),
(1, 4, 'Surgical Mask N95', 500, 4),
(2, 2, 'Amoxicillin 500mg Tablet', 300, 5),
(2, 3, 'Saline Solution 0.9%', 200, 5),
(3, 7, 'Paracetamol 500mg', 400, 6),
(4, 5, 'IV Cannula 20G', 150, 7);

INSERT INTO [ConsumptionRecord] ([FacilityId], [WardId], [ItemId], [ItemName], [QuantityConsumed], [ConsumedDate], [ConsumedBy]) VALUES
(1, 101, 1, 'Insulin Vial 100IU', 15, DATEADD(DAY, -5, GETUTCDATE()), 'user-nurse-01'),
(1, 102, 2, 'Amoxicillin 500mg Tablet', 30, DATEADD(DAY, -4, GETUTCDATE()), 'user-nurse-02'),
(2, 201, 3, 'Saline Solution 0.9%', 50, DATEADD(DAY, -3, GETUTCDATE()), 'user-nurse-03'),
(2, 202, 4, 'Surgical Mask N95', 200, DATEADD(DAY, -2, GETUTCDATE()), 'user-staff-01'),
(3, 301, 5, 'IV Cannula 20G', 75, DATEADD(DAY, -1, GETUTCDATE()), 'user-nurse-04'),
(1, 103, 7, 'Paracetamol 500mg', 45, GETUTCDATE(), 'user-nurse-05');

-- ============================================================================
-- 5. TELEMETRY SERVICE - Mock Data
-- ============================================================================

INSERT INTO [SensorDevice] ([DeviceName], [DeviceType], [AssignedTo], [AssignedEntityId], [Status]) VALUES
('Zone A1 Temp Sensor', 'Temp', 'Zone', 1, 'Active'),
('Zone A1 Humidity Sensor', 'Humidity', 'Zone', 1, 'Active'),
('Zone A2 Freezer Monitor', 'Temp', 'Zone', 2, 'Active'),
('Zone B1 Refrigerated Unit', 'Temp', 'Zone', 4, 'Active'),
('Shipment GPS Tracker 001', 'GPS', 'Shipment', 1, 'Active'),
('Shipment GPS Tracker 002', 'GPS', 'Shipment', 2, 'Inactive'),
('Zone C1 Ambient Monitor', 'Temp', 'Zone', 6, 'Active'),
('Central Hub Temperature', 'Temp', 'Zone', 8, 'Active');

INSERT INTO [TelemetryRecord] ([SensorID], [Timestamp], [Temperature], [Humidity], [Location], [IsExcursion]) VALUES
(1, DATEADD(HOUR, -24, GETUTCDATE()), 4.5, 55.2, 'Apollo Hospital Zone A1', 0),
(1, DATEADD(HOUR, -12, GETUTCDATE()), 4.3, 54.8, 'Apollo Hospital Zone A1', 0),
(1, DATEADD(HOUR, -1, GETUTCDATE()), 4.2, 55.1, 'Apollo Hospital Zone A1', 0),
(2, DATEADD(HOUR, -24, GETUTCDATE()), 4.2, 65.0, 'Apollo Hospital Zone A1', 0),
(3, DATEADD(HOUR, -24, GETUTCDATE()), -18.5, 45.3, 'Apollo Hospital Zone A2', 0),
(3, DATEADD(HOUR, -12, GETUTCDATE()), -18.7, 44.9, 'Apollo Hospital Zone A2', 0),
(3, DATEADD(HOUR, -2, GETUTCDATE()), 8.2, 70.5, 'Apollo Hospital Zone A2', 1),
(4, DATEADD(HOUR, -24, GETUTCDATE()), 5.1, 60.2, 'Max Healthcare Zone B1', 0),
(5, DATEADD(HOUR, -24, GETUTCDATE()), 25.3, 45.0, '13.0827,80.2707', 0),
(7, DATEADD(HOUR, -24, GETUTCDATE()), 22.5, 50.0, 'Regional Medical Center Zone C1', 0),
(7, DATEADD(HOUR, -12, GETUTCDATE()), 23.1, 51.2, 'Regional Medical Center Zone C1', 0),
(8, DATEADD(HOUR, -24, GETUTCDATE()), 21.8, 48.5, 'Distribution Hub Delhi', 0);

-- ============================================================================
-- 6. NOTIFICATION SERVICE - Mock Data
-- ============================================================================

INSERT INTO [Notification] ([UserId], [Category], [Title], [Message], [IsRead], [CreatedAt]) VALUES
('user-manager-01', 'Exception', 'Stockout Alert', 'Insulin stock at Apollo Hospital has reached critical level', 0, DATEADD(HOUR, -24, GETUTCDATE())),
('user-manager-01', 'Expiry', 'Expiry Warning', 'Vaccine batch LOT-VAC-2026-001 expiring in 3 days', 0, DATEADD(HOUR, -18, GETUTCDATE())),
('user-warehouse-01', 'Receipt', 'Receipt Confirmed', 'PO#1 received: 200 units of Insulin from Cipla Limited', 1, DATEADD(HOUR, -12, GETUTCDATE())),
('user-nurse-02', 'Replenishment', 'Replenishment Suggested', 'Order suggested for Amoxicillin at Apollo Hospital', 0, DATEADD(HOUR, -6, GETUTCDATE())),
('user-manager-02', 'Exception', 'Temperature Excursion', 'Temperature breach detected in Zone A2 at Apollo Hospital', 0, DATEADD(HOUR, -2, GETUTCDATE())),
('user-compliance-01', 'Exception', 'Recall Action Pending', 'Recall action REC-001 is overdue', 0, GETUTCDATE());

-- ============================================================================
-- 7. AUTH SERVICE - Mock Data
-- ============================================================================

INSERT INTO [User] ([Name], [Role], [Email], [Phone], [Password]) VALUES
('Rajesh Kumar', 'Admin', 'rajesh.kumar@medipulse.com', '+91-9876543210', '$2a$11$...hashed_password_admin...'),
('Priya Singh', 'WarehouseManager', 'priya.singh@medipulse.com', '+91-9876543211', '$2a$11$...hashed_password...'),
('Amit Patel', 'Nurse', 'amit.patel@medipulse.com', '+91-9876543212', '$2a$11$...hashed_password...'),
('Sophia Johnson', 'ComplianceOfficer', 'sophia.johnson@medipulse.com', '+91-9876543213', '$2a$11$...hashed_password...'),
('Dr. Vikram Sharma', 'Doctor', 'vikram.sharma@medipulse.com', '+91-9876543214', '$2a$11$...hashed_password...'),
('Neha Gupta', 'WarehouseStaff', 'neha.gupta@medipulse.com', '+91-9876543215', '$2a$11$...hashed_password...'),
('Mohammed Hassan', 'LogisticsManager', 'hassan.m@medipulse.com', '+91-9876543216', '$2a$11$...hashed_password...');

-- ============================================================================
-- 8. AUDIT SERVICE - Mock Data (Separate Database: MedipulseAudit)
-- ============================================================================

INSERT INTO [AuditLog] ([UserId], [UserName], [UserRole], [HttpMethod], [Endpoint], [EntityType], [EntityId], [ServiceName], [StatusCode], [Timestamp], [Details]) VALUES
('user-manager-01', 'Rajesh Kumar', 'Admin', 'POST', '/api/suppliers', 'Supplier', '1', 'ProcurementService', 201, DATEADD(DAY, -7, GETUTCDATE()), 'Created new supplier: Cipla Limited'),
('user-warehouse-01', 'Priya Singh', 'WarehouseManager', 'GET', '/api/inventory/items', 'Item', NULL, 'InventoryService', 200, DATEADD(DAY, -5, GETUTCDATE()), 'Retrieved all items'),
('user-nurse-02', 'Amit Patel', 'Nurse', 'POST', '/api/consumption', 'ConsumptionRecord', '1', 'LogisticsService', 201, DATEADD(DAY, -3, GETUTCDATE()), 'Logged consumption: Insulin 15 units'),
('user-compliance-01', 'Sophia Johnson', 'ComplianceOfficer', 'GET', '/api/audit', NULL, NULL, 'AuditService', 200, DATEADD(DAY, -2, GETUTCDATE()), 'Audit report generated'),
('user-manager-02', 'Rajesh Kumar', 'Admin', 'PUT', '/api/purchase-orders/3', 'PurchaseOrder', '3', 'ProcurementService', 200, DATEADD(DAY, -1, GETUTCDATE()), 'Updated PO status to Approved'),
('user-warehouse-02', 'Neha Gupta', 'WarehouseStaff', 'POST', '/api/receipts', 'Receipt', '1', 'ProcurementService', 201, GETUTCDATE(), 'GRN created for PO#1'),
('user-logistics-01', 'Mohammed Hassan', 'LogisticsManager', 'POST', '/api/transfer-orders', 'TransferOrder', '1', 'LogisticsService', 201, DATEADD(HOUR, -12, GETUTCDATE()), 'Transfer order created from Hub to Apollo');

-- ============================================================================
-- VERIFICATION QUERIES (Optional - uncomment to run)
-- ============================================================================

/*
-- Count records in each table
SELECT 'Facility' as TableName, COUNT(*) as RecordCount FROM [Facility]
UNION ALL
SELECT 'StorageZone', COUNT(*) FROM [StorageZone]
UNION ALL
SELECT 'Item', COUNT(*) FROM [Item]
UNION ALL
SELECT 'InventoryPositions', COUNT(*) FROM [InventoryPositions]
UNION ALL
SELECT 'ExceptionEvent', COUNT(*) FROM [ExceptionEvent]
UNION ALL
SELECT 'RecallAction', COUNT(*) FROM [RecallAction]
UNION ALL
SELECT 'Forecast', COUNT(*) FROM [Forecast]
UNION ALL
SELECT 'ReplenishmentPlan', COUNT(*) FROM [ReplenishmentPlan]
UNION ALL
SELECT 'Supplier', COUNT(*) FROM [Supplier]
UNION ALL
SELECT 'PurchaseOrder', COUNT(*) FROM [PurchaseOrder]
UNION ALL
SELECT 'Receipt', COUNT(*) FROM [Receipt]
UNION ALL
SELECT 'TransferOrder', COUNT(*) FROM [TransferOrder]
UNION ALL
SELECT 'TransferOrderItem', COUNT(*) FROM [TransferOrderItem]
UNION ALL
SELECT 'ConsumptionRecord', COUNT(*) FROM [ConsumptionRecord]
UNION ALL
SELECT 'SensorDevice', COUNT(*) FROM [SensorDevice]
UNION ALL
SELECT 'TelemetryRecord', COUNT(*) FROM [TelemetryRecord]
UNION ALL
SELECT 'Notification', COUNT(*) FROM [Notification]
UNION ALL
SELECT 'User', COUNT(*) FROM [User]
UNION ALL
SELECT 'AuditLog', COUNT(*) FROM [AuditLog]
ORDER BY TableName;

-- Find critical exceptions
SELECT * FROM [ExceptionEvent] WHERE [Severity] = 'High' AND [Status] = 'Open';

-- Check inventory levels vs safety stock
SELECT 
	i.[ItemCode],
	i.[Name],
	ip.[Quantity],
	i.[SafetyStock],
	CASE WHEN ip.[Quantity] < i.[SafetyStock] THEN 'BELOW SAFETY STOCK' ELSE 'OK' END as [StockStatus]
FROM [InventoryPositions] ip
JOIN [Item] i ON ip.[ItemId] = i.[ItemId]
ORDER BY ip.[Quantity] ASC;

-- Track temperature excursions
SELECT 
	sd.[DeviceName],
	tr.[Timestamp],
	tr.[Temperature],
	tr.[Humidity],
	tr.[IsExcursion]
FROM [TelemetryRecord] tr
JOIN [SensorDevice] sd ON tr.[SensorID] = sd.[SensorID]
WHERE tr.[IsExcursion] = 1
ORDER BY tr.[Timestamp] DESC;
*/

-- ============================================================================
-- END OF MOCK DATA INSERTS
-- ============================================================================
