-- ============================================================================
-- MediPulseMicro - Complete SQL Schema and Mock Data
-- Database: MedipulseMain (for most services) and MedipulseAudit (for AuditService)
-- ============================================================================

-- ============================================================================
-- 1. FACILITY SERVICE TABLES
-- ============================================================================

-- Facility: Medical facilities (hospitals, clinics, distribution centers)
CREATE TABLE [Facility] (
	[FacilityID] INT PRIMARY KEY IDENTITY(1,1),
	[Name] NVARCHAR(100) NOT NULL,
	[Type] NVARCHAR(50),
	[Region] NVARCHAR(100)
);

-- StorageZone: Temperature-controlled zones within facilities
CREATE TABLE [StorageZone] (
	[ZoneID] INT PRIMARY KEY IDENTITY(1,1),
	[FacilityID] INT NOT NULL,
	[Name] NVARCHAR(100),
	[TemperatureProfile] NVARCHAR(50),
	[Capacity] DECIMAL(18, 2),
	CONSTRAINT FK_StorageZone_Facility FOREIGN KEY ([FacilityID]) REFERENCES [Facility]([FacilityID])
);

-- Insert Facility Mock Data
INSERT INTO [Facility] ([Name], [Type], [Region]) VALUES
('Apollo Hospital Delhi', 'Hospital', 'North'),
('Max Healthcare Mumbai', 'Hospital', 'West'),
('Regional Medical Center Chennai', 'Hospital', 'South'),
('Fortis Healthcare Bangalore', 'Hospital', 'South'),
('Central Distribution Hub Delhi', 'Distribution Center', 'North'),
('Eastern Medical Clinic Kolkata', 'Clinic', 'East');

-- Insert StorageZone Mock Data
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
-- 2. INVENTORY SERVICE TABLES
-- ============================================================================

-- Item: Medical inventory items (medicines, devices, consumables)
CREATE TABLE [Item] (
	[ItemId] INT PRIMARY KEY IDENTITY(1,1),
	[ItemCode] NVARCHAR(50) NOT NULL UNIQUE,
	[Name] NVARCHAR(150) NOT NULL,
	[Category] NVARCHAR(50) NOT NULL,
	[Unit] NVARCHAR(20) NOT NULL,
	[StorageRequirement] NVARCHAR(50),
	[SafetyStock] INT NOT NULL
);

-- InventoryPositions: Tracks lot-level inventory at facilities
CREATE TABLE [InventoryPositions] (
	[PositionId] INT PRIMARY KEY IDENTITY(1,1),
	[ItemId] INT NOT NULL,
	[LotId] NVARCHAR(50) NOT NULL,
	[ExpiryDate] DATETIME2 NOT NULL,
	[Quantity] INT NOT NULL,
	[FacilityId] INT NOT NULL,
	[StorageZoneId] INT NOT NULL,
	[SafetyStock] INT NOT NULL,
	CONSTRAINT FK_InventoryPositions_Item FOREIGN KEY ([ItemId]) REFERENCES [Item]([ItemId]) ON DELETE CASCADE,
	CONSTRAINT FK_InventoryPositions_Facility FOREIGN KEY ([FacilityId]) REFERENCES [Facility]([FacilityID]),
	CONSTRAINT FK_InventoryPositions_StorageZone FOREIGN KEY ([StorageZoneId]) REFERENCES [StorageZone]([ZoneID])
);

-- ExceptionEvent: Detected supply chain issues (stockouts, expiry, temperature breaches)
CREATE TABLE [ExceptionEvent] (
	[ExceptionId] INT PRIMARY KEY IDENTITY(1,1),
	[Type] NVARCHAR(50) NOT NULL,
	[ReferenceType] NVARCHAR(50) NOT NULL,
	[ReferenceId] INT NOT NULL,
	[ItemId] INT,
	[ItemName] NVARCHAR(150),
	[FacilityId] INT,
	[LotId] NVARCHAR(50),
	[Severity] NVARCHAR(20) NOT NULL DEFAULT 'Medium',
	[Status] NVARCHAR(20) NOT NULL DEFAULT 'Open',
	[DetectedDate] DATETIME2 NOT NULL,
	CONSTRAINT FK_ExceptionEvent_Item FOREIGN KEY ([ItemId]) REFERENCES [Item]([ItemId])
);

CREATE INDEX IX_ExceptionEvent_Type_Status ON [ExceptionEvent]([Type], [Status]);

-- RecallAction: Corrective actions assigned for exceptions
CREATE TABLE [RecallAction] (
	[RecallActionId] INT PRIMARY KEY IDENTITY(1,1),
	[ExceptionId] INT NOT NULL,
	[OwnerId] NVARCHAR(100) NOT NULL,
	[ActionDescription] NVARCHAR(500) NOT NULL,
	[DueDate] DATETIME2 NOT NULL,
	[Status] NVARCHAR(20) NOT NULL DEFAULT 'Pending',
	CONSTRAINT FK_RecallAction_Exception FOREIGN KEY ([ExceptionId]) REFERENCES [ExceptionEvent]([ExceptionId]) ON DELETE CASCADE
);

-- Forecast: Demand forecasts for items
CREATE TABLE [Forecast] (
	[ForecastId] INT PRIMARY KEY IDENTITY(1,1),
	[ItemId] INT NOT NULL,
	[FacilityId] INT NOT NULL,
	[Period] NVARCHAR(10) NOT NULL,
	[ForecastQuantity] INT NOT NULL,
	[GeneratedDate] DATETIME2 NOT NULL,
	CONSTRAINT FK_Forecast_Item FOREIGN KEY ([ItemId]) REFERENCES [Item]([ItemId]),
	CONSTRAINT FK_Forecast_Facility FOREIGN KEY ([FacilityId]) REFERENCES [Facility]([FacilityID])
);

CREATE INDEX IX_Forecast_Facility_Item_Period ON [Forecast]([FacilityId], [ItemId], [Period]);

-- ReplenishmentPlan: Purchase suggestions
CREATE TABLE [ReplenishmentPlan] (
	[PlanId] INT PRIMARY KEY IDENTITY(1,1),
	[ItemId] INT NOT NULL,
	[FacilityId] INT NOT NULL,
	[SuggestedOrderQty] INT NOT NULL,
	[Priority] NVARCHAR(20) NOT NULL DEFAULT 'Medium',
	[Status] NVARCHAR(20) NOT NULL DEFAULT 'Pending',
	[GeneratedDate] DATETIME2 NOT NULL,
	CONSTRAINT FK_ReplenishmentPlan_Item FOREIGN KEY ([ItemId]) REFERENCES [Item]([ItemId]),
	CONSTRAINT FK_ReplenishmentPlan_Facility FOREIGN KEY ([FacilityId]) REFERENCES [Facility]([FacilityID])
);

CREATE INDEX IX_ReplenishmentPlan_Facility_Status ON [ReplenishmentPlan]([FacilityId], [Status]);

-- Insert Item Mock Data
INSERT INTO [Item] ([ItemCode], [Name], [Category], [Unit], [StorageRequirement], [SafetyStock]) VALUES
('MED-001', 'Insulin Vial 100IU', 'Pharma', 'Box', 'Refrigerated', 50),
('MED-002', 'Amoxicillin 500mg Tablet', 'Pharma', 'Box', 'Ambient', 100),
('MED-003', 'Saline Solution 0.9%', 'Pharma', 'Box', 'Ambient', 200),
('MED-004', 'Surgical Mask N95', 'Consumable', 'Box', 'Ambient', 500),
('MED-005', 'IV Cannula 20G', 'Device', 'Box', 'Ambient', 300),
('MED-006', 'Vaccine Pfizer Vial', 'Pharma', 'Vial', 'Freezer', 25),
('MED-007', 'Paracetamol 500mg', 'Pharma', 'Box', 'Ambient', 150),
('MED-008', 'Blood Pressure Monitor', 'Device', 'Piece', 'Ambient', 10);

-- Insert InventoryPosition Mock Data
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

-- Insert ExceptionEvent Mock Data
INSERT INTO [ExceptionEvent] ([Type], [ReferenceType], [ReferenceId], [ItemId], [ItemName], [FacilityId], [LotId], [Severity], [Status], [DetectedDate]) VALUES
('Stockout', 'InventoryPosition', 4, 3, 'Saline Solution 0.9%', 2, 'LOT-SAL-2026-001', 'High', 'Open', GETUTCDATE()),
('ExpiryAlert', 'InventoryPosition', 7, 6, 'Vaccine Pfizer Vial', 1, 'LOT-VAC-2026-001', 'High', 'InProgress', DATEADD(DAY, -20, GETUTCDATE())),
('Excursion', 'Telemetry', 5, 1, 'Insulin Vial 100IU', 1, 'LOT-INS-2026-001', 'High', 'Open', DATEADD(DAY, -5, GETUTCDATE())),
('Recall', 'Supplier', 1, 2, 'Amoxicillin 500mg Tablet', 1, 'LOT-AMX-2026-001', 'Medium', 'Open', DATEADD(DAY, -2, GETUTCDATE()));

-- Insert RecallAction Mock Data
INSERT INTO [RecallAction] ([ExceptionId], [OwnerId], [ActionDescription], [DueDate], [Status]) VALUES
(1, 'user-101', 'Urgent replenishment needed for Apollo Delhi', DATEADD(DAY, 3, GETUTCDATE()), 'InProgress'),
(2, 'user-102', 'Initiate cold chain recall for expired vaccines', DATEADD(DAY, 7, GETUTCDATE()), 'Pending'),
(3, 'user-103', 'Investigate temperature excursion in zone A1', DATEADD(DAY, 2, GETUTCDATE()), 'Pending'),
(4, 'user-104', 'Quarantine and destroy recalled batch', DATEADD(DAY, 5, GETUTCDATE()), 'Pending');

-- Insert Forecast Mock Data
INSERT INTO [Forecast] ([ItemId], [FacilityId], [Period], [ForecastQuantity], [GeneratedDate]) VALUES
(1, 1, '2026-06', 300, GETUTCDATE()),
(2, 1, '2026-06', 600, GETUTCDATE()),
(3, 2, '2026-06', 500, GETUTCDATE()),
(4, 1, '2026-06', 1500, GETUTCDATE()),
(5, 2, '2026-06', 400, GETUTCDATE()),
(7, 3, '2026-06', 800, GETUTCDATE());

-- Insert ReplenishmentPlan Mock Data
INSERT INTO [ReplenishmentPlan] ([ItemId], [FacilityId], [SuggestedOrderQty], [Priority], [Status], [GeneratedDate]) VALUES
(1, 1, 200, 'High', 'Pending', GETUTCDATE()),
(3, 2, 500, 'High', 'Pending', GETUTCDATE()),
(4, 1, 1000, 'Medium', 'Pending', GETUTCDATE()),
(2, 1, 400, 'Medium', 'Pending', GETUTCDATE());

-- ============================================================================
-- 3. PROCUREMENT SERVICE TABLES
-- ============================================================================

-- Supplier: Vendors and pharmaceutical suppliers
CREATE TABLE [Supplier] (
	[SupplierID] INT PRIMARY KEY IDENTITY(1,1),
	[Name] NVARCHAR(100) NOT NULL,
	[SupplierType] NVARCHAR(50),
	[Status] NVARCHAR(50) NOT NULL DEFAULT 'Active'
);

-- PurchaseOrder: Orders placed with suppliers
CREATE TABLE [PurchaseOrder] (
	[POID] INT PRIMARY KEY IDENTITY(1,1),
	[SupplierID] INT NOT NULL,
	[OrderDate] DATETIME2 NOT NULL,
	[ExpectedDeliveryDate] DATETIME2,
	[Status] NVARCHAR(50) NOT NULL DEFAULT 'Draft',
	[Notes] NVARCHAR(500),
	CONSTRAINT FK_PurchaseOrder_Supplier FOREIGN KEY ([SupplierID]) REFERENCES [Supplier]([SupplierID]) ON DELETE RESTRICT
);

-- Receipt: Goods receipt notes (GRN) for received orders
CREATE TABLE [Receipt] (
	[ReceiptID] INT PRIMARY KEY IDENTITY(1,1),
	[POID] INT NOT NULL,
	[SupplierLot] NVARCHAR(100),
	[ReceivedDate] DATETIME2 NOT NULL,
	[ReceivedBy] NVARCHAR(100) NOT NULL,
	[QualityStatus] NVARCHAR(50) NOT NULL DEFAULT 'Accepted',
	[QuantityReceived] INT NOT NULL,
	CONSTRAINT FK_Receipt_PurchaseOrder FOREIGN KEY ([POID]) REFERENCES [PurchaseOrder]([POID]) ON DELETE CASCADE
);

-- Insert Supplier Mock Data
INSERT INTO [Supplier] ([Name], [SupplierType], [Status]) VALUES
('Cipla Limited', 'Manufacturer', 'Active'),
('Lupin Pharmaceuticals', 'Manufacturer', 'Active'),
('Mankind Pharma', 'Manufacturer', 'Active'),
('GlaxoSmithKline India', 'Manufacturer', 'Active'),
('MedExpress Distributor', 'Distributor', 'Active'),
('PharmaCold Logistics', '3PL', 'Active'),
('Dr. Reddy Laboratories', 'Manufacturer', 'OnHold');

-- Insert PurchaseOrder Mock Data
INSERT INTO [PurchaseOrder] ([SupplierID], [OrderDate], [ExpectedDeliveryDate], [Status], [Notes]) VALUES
(1, '2026-05-15', '2026-06-05', 'Approved', 'Insulin supply for Apollo Network'),
(2, '2026-05-18', '2026-06-08', 'Shipped', 'Amoxicillin restocking'),
(3, '2026-05-20', '2026-06-10', 'PartiallyReceived', 'Surgical supplies bundle'),
(4, '2026-05-25', '2026-06-15', 'Draft', 'Vaccine procurement'),
(1, '2026-06-01', '2026-06-20', 'Submitted', 'Emergency replenishment'),
(5, '2026-05-28', '2026-06-05', 'FullyReceived', 'General consumables');

-- Insert Receipt Mock Data
INSERT INTO [Receipt] ([POID], [SupplierLot], [ReceivedDate], [ReceivedBy], [QualityStatus], [QuantityReceived]) VALUES
(1, 'LOT-INS-2026-001', '2026-06-01', 'user-warehouse-01', 'Accepted', 200),
(2, 'LOT-AMX-2026-001', '2026-06-08', 'user-warehouse-02', 'Accepted', 500),
(3, 'LOT-SUR-2026-001', '2026-06-09', 'user-warehouse-01', 'Accepted', 300),
(3, 'LOT-SUR-2026-002', '2026-06-12', 'user-warehouse-02', 'OnHold', 150),
(6, 'LOT-CON-2026-001', '2026-06-04', 'user-warehouse-03', 'Accepted', 1000),
(1, 'LOT-INS-2026-002', '2026-06-05', 'user-warehouse-01', 'Accepted', 150);

-- ============================================================================
-- 4. LOGISTICS SERVICE TABLES
-- ============================================================================

-- TransferOrder: Inter-facility stock transfers
CREATE TABLE [TransferOrder] (
	[TransferOrderId] INT PRIMARY KEY IDENTITY(1,1),
	[FromFacilityId] INT NOT NULL,
	[FromFacilityName] NVARCHAR(100) NOT NULL,
	[ToFacilityId] INT NOT NULL,
	[ToFacilityName] NVARCHAR(100) NOT NULL,
	[RequestedBy] NVARCHAR(100) NOT NULL,
	[RequestedDate] DATETIME2 NOT NULL,
	[Status] NVARCHAR(50) NOT NULL DEFAULT 'Draft',
	CONSTRAINT FK_TransferOrder_FromFacility FOREIGN KEY ([FromFacilityId]) REFERENCES [Facility]([FacilityID]),
	CONSTRAINT FK_TransferOrder_ToFacility FOREIGN KEY ([ToFacilityId]) REFERENCES [Facility]([FacilityID])
);

-- TransferOrderItem: Line items in a transfer order
CREATE TABLE [TransferOrderItem] (
	[TransferOrderItemId] INT PRIMARY KEY IDENTITY(1,1),
	[TransferOrderId] INT NOT NULL,
	[ItemId] INT NOT NULL,
	[ItemName] NVARCHAR(150) NOT NULL,
	[Quantity] INT NOT NULL,
	[ToStorageZoneId] INT NOT NULL,
	CONSTRAINT FK_TransferOrderItem_TransferOrder FOREIGN KEY ([TransferOrderId]) REFERENCES [TransferOrder]([TransferOrderId]) ON DELETE CASCADE,
	CONSTRAINT FK_TransferOrderItem_StorageZone FOREIGN KEY ([ToStorageZoneId]) REFERENCES [StorageZone]([ZoneID])
);

-- ConsumptionRecord: Track stock consumption at facilities
CREATE TABLE [ConsumptionRecord] (
	[ConsumptionId] INT PRIMARY KEY IDENTITY(1,1),
	[FacilityId] INT NOT NULL,
	[WardId] INT,
	[ItemId] INT NOT NULL,
	[ItemName] NVARCHAR(150) NOT NULL,
	[QuantityConsumed] INT NOT NULL,
	[ConsumedDate] DATETIME2 NOT NULL,
	[ConsumedBy] NVARCHAR(100) NOT NULL,
	CONSTRAINT FK_ConsumptionRecord_Facility FOREIGN KEY ([FacilityId]) REFERENCES [Facility]([FacilityID])
);

-- Insert TransferOrder Mock Data
INSERT INTO [TransferOrder] ([FromFacilityId], [FromFacilityName], [ToFacilityId], [ToFacilityName], [RequestedBy], [RequestedDate], [Status]) VALUES
(5, 'Central Distribution Hub Delhi', 1, 'Apollo Hospital Delhi', 'user-manager-01', DATEADD(DAY, -5, GETUTCDATE()), 'Completed'),
(1, 'Apollo Hospital Delhi', 2, 'Max Healthcare Mumbai', 'user-manager-02', DATEADD(DAY, -2, GETUTCDATE()), 'InTransit'),
(5, 'Central Distribution Hub Delhi', 3, 'Regional Medical Center Chennai', 'user-manager-03', DATEADD(HOUR, -12, GETUTCDATE()), 'Approved'),
(2, 'Max Healthcare Mumbai', 4, 'Fortis Healthcare Bangalore', 'user-manager-01', GETUTCDATE(), 'Draft');

-- Insert TransferOrderItem Mock Data
INSERT INTO [TransferOrderItem] ([TransferOrderId], [ItemId], [ItemName], [Quantity], [ToStorageZoneId]) VALUES
(1, 1, 'Insulin Vial 100IU', 100, 4),
(1, 4, 'Surgical Mask N95', 500, 4),
(2, 2, 'Amoxicillin 500mg Tablet', 300, 5),
(2, 3, 'Saline Solution 0.9%', 200, 5),
(3, 7, 'Paracetamol 500mg', 400, 6),
(4, 5, 'IV Cannula 20G', 150, 7);

-- Insert ConsumptionRecord Mock Data
INSERT INTO [ConsumptionRecord] ([FacilityId], [WardId], [ItemId], [ItemName], [QuantityConsumed], [ConsumedDate], [ConsumedBy]) VALUES
(1, 101, 1, 'Insulin Vial 100IU', 15, DATEADD(DAY, -5, GETUTCDATE()), 'user-nurse-01'),
(1, 102, 2, 'Amoxicillin 500mg Tablet', 30, DATEADD(DAY, -4, GETUTCDATE()), 'user-nurse-02'),
(2, 201, 3, 'Saline Solution 0.9%', 50, DATEADD(DAY, -3, GETUTCDATE()), 'user-nurse-03'),
(2, 202, 4, 'Surgical Mask N95', 200, DATEADD(DAY, -2, GETUTCDATE()), 'user-staff-01'),
(3, 301, 5, 'IV Cannula 20G', 75, DATEADD(DAY, -1, GETUTCDATE()), 'user-nurse-04'),
(1, 103, 7, 'Paracetamol 500mg', 45, GETUTCDATE(), 'user-nurse-05');

-- ============================================================================
-- 5. TELEMETRY SERVICE TABLES
-- ============================================================================

-- SensorDevice: IoT sensors for monitoring cold chain conditions
CREATE TABLE [SensorDevice] (
	[SensorID] INT PRIMARY KEY IDENTITY(1,1),
	[DeviceName] NVARCHAR(100) NOT NULL,
	[DeviceType] NVARCHAR(50) NOT NULL,
	[AssignedTo] NVARCHAR(50) NOT NULL,
	[AssignedEntityId] INT,
	[Status] NVARCHAR(50) NOT NULL DEFAULT 'Active'
);

-- TelemetryRecord: Temperature and humidity readings from sensors
CREATE TABLE [TelemetryRecord] (
	[TelemetryID] INT PRIMARY KEY IDENTITY(1,1),
	[SensorID] INT NOT NULL,
	[Timestamp] DATETIME2 NOT NULL,
	[Temperature] DECIMAL(5,2),
	[Humidity] DECIMAL(5,2),
	[Location] NVARCHAR(200),
	[IsExcursion] BIT NOT NULL DEFAULT 0,
	CONSTRAINT FK_TelemetryRecord_SensorDevice FOREIGN KEY ([SensorID]) REFERENCES [SensorDevice]([SensorID]) ON DELETE CASCADE
);

-- Insert SensorDevice Mock Data
INSERT INTO [SensorDevice] ([DeviceName], [DeviceType], [AssignedTo], [AssignedEntityId], [Status]) VALUES
('Zone A1 Temp Sensor', 'Temp', 'Zone', 1, 'Active'),
('Zone A1 Humidity Sensor', 'Humidity', 'Zone', 1, 'Active'),
('Zone A2 Freezer Monitor', 'Temp', 'Zone', 2, 'Active'),
('Zone B1 Refrigerated Unit', 'Temp', 'Zone', 4, 'Active'),
('Shipment GPS Tracker 001', 'GPS', 'Shipment', 1, 'Active'),
('Shipment GPS Tracker 002', 'GPS', 'Shipment', 2, 'Inactive'),
('Zone C1 Ambient Monitor', 'Temp', 'Zone', 6, 'Active'),
('Central Hub Temperature', 'Temp', 'Zone', 8, 'Active');

-- Insert TelemetryRecord Mock Data
INSERT INTO [TelemetryRecord] ([SensorID], [Timestamp], [Temperature], [Humidity], [Location], [IsExcursion]) VALUES
(1, DATEADD(HOUR, -24, GETUTCDATE()), 4.5, 55.2, 'Apollo Hospital Zone A1', 0),
(1, DATEADD(HOUR, -12, GETUTCDATE()), 4.3, 54.8, 'Apollo Hospital Zone A1', 0),
(1, DATEADD(HOUR, -1, GETUTCDATE()), 4.2, 55.1, 'Apollo Hospital Zone A1', 0),
(2, DATEADD(HOUR, -24, GETUTCDATE()), 4.2, 65.0, 'Apollo Hospital Zone A1', 0),
(3, DATEADD(HOUR, -24, GETUTCDATE()), -18.5, 45.3, 'Apollo Hospital Zone A2', 0),
(3, DATEADD(HOUR, -12, GETUTCDATE()), -18.7, 44.9, 'Apollo Hospital Zone A2', 0),
(3, DATEADD(HOUR, -2, GETUTCDATE()), 8.2, 70.5, 'Apollo Hospital Zone A2', 1),  -- EXCURSION!
(4, DATEADD(HOUR, -24, GETUTCDATE()), 5.1, 60.2, 'Max Healthcare Zone B1', 0),
(5, DATEADD(HOUR, -24, GETUTCDATE()), 25.3, 45.0, '13.0827,80.2707', 0),
(7, DATEADD(HOUR, -24, GETUTCDATE()), 22.5, 50.0, 'Regional Medical Center Zone C1', 0),
(7, DATEADD(HOUR, -12, GETUTCDATE()), 23.1, 51.2, 'Regional Medical Center Zone C1', 0),
(8, DATEADD(HOUR, -24, GETUTCDATE()), 21.8, 48.5, 'Distribution Hub Delhi', 0);

-- ============================================================================
-- 6. NOTIFICATION SERVICE TABLES
-- ============================================================================

-- Notification: User notifications for events
CREATE TABLE [Notification] (
	[NotificationId] INT PRIMARY KEY IDENTITY(1,1),
	[UserId] NVARCHAR(100) NOT NULL,
	[Category] NVARCHAR(50) NOT NULL,
	[Title] NVARCHAR(200) NOT NULL,
	[Message] NVARCHAR(1000) NOT NULL,
	[IsRead] BIT NOT NULL DEFAULT 0,
	[CreatedAt] DATETIME2 NOT NULL
);

CREATE INDEX IX_Notification_UserIdIsRead ON [Notification]([UserId], [IsRead]);
CREATE INDEX IX_Notification_CreatedAt ON [Notification]([CreatedAt]);

-- Insert Notification Mock Data
INSERT INTO [Notification] ([UserId], [Category], [Title], [Message], [IsRead], [CreatedAt]) VALUES
('user-manager-01', 'Exception', 'Stockout Alert', 'Insulin stock at Apollo Hospital has reached critical level', 0, DATEADD(HOUR, -24, GETUTCDATE())),
('user-manager-01', 'Expiry', 'Expiry Warning', 'Vaccine batch LOT-VAC-2026-001 expiring in 3 days', 0, DATEADD(HOUR, -18, GETUTCDATE())),
('user-warehouse-01', 'Receipt', 'Receipt Confirmed', 'PO#1 received: 200 units of Insulin from Cipla Limited', 1, DATEADD(HOUR, -12, GETUTCDATE())),
('user-nurse-02', 'Replenishment', 'Replenishment Suggested', 'Order suggested for Amoxicillin at Apollo Hospital', 0, DATEADD(HOUR, -6, GETUTCDATE())),
('user-manager-02', 'Exception', 'Temperature Excursion', 'Temperature breach detected in Zone A2 at Apollo Hospital', 0, DATEADD(HOUR, -2, GETUTCDATE())),
('user-compliance-01', 'Exception', 'Recall Action Pending', 'Recall action REC-001 is overdue', 0, GETUTCDATE());

-- ============================================================================
-- 7. AUTH SERVICE TABLES
-- ============================================================================

-- User: System users with roles
CREATE TABLE [User] (
	[UserID] INT PRIMARY KEY IDENTITY(1,1),
	[Name] NVARCHAR(100) NOT NULL,
	[Role] NVARCHAR(50) NOT NULL,
	[Email] NVARCHAR(255) NOT NULL UNIQUE,
	[Phone] NVARCHAR(20),
	[Password] NVARCHAR(255) NOT NULL DEFAULT ''
);

-- Insert User Mock Data
INSERT INTO [User] ([Name], [Role], [Email], [Phone], [Password]) VALUES
('Rajesh Kumar', 'Admin', 'rajesh.kumar@medipulse.com', '+91-9876543210', '$2a$11$...hashed_password_admin...'),
('Priya Singh', 'WarehouseManager', 'priya.singh@medipulse.com', '+91-9876543211', '$2a$11$...hashed_password...'),
('Amit Patel', 'Nurse', 'amit.patel@medipulse.com', '+91-9876543212', '$2a$11$...hashed_password...'),
('Sophia Johnson', 'ComplianceOfficer', 'sophia.johnson@medipulse.com', '+91-9876543213', '$2a$11$...hashed_password...'),
('Dr. Vikram Sharma', 'Doctor', 'vikram.sharma@medipulse.com', '+91-9876543214', '$2a$11$...hashed_password...'),
('Neha Gupta', 'WarehouseStaff', 'neha.gupta@medipulse.com', '+91-9876543215', '$2a$11$...hashed_password...'),
('Mohammed Hassan', 'LogisticsManager', 'hassan.m@medipulse.com', '+91-9876543216', '$2a$11$...hashed_password...');

-- ============================================================================
-- 8. AUDIT SERVICE TABLES (Separate Database: MedipulseAudit)
-- ============================================================================
-- Note: This should be in a separate database called MedipulseAudit
-- Run this separately in that database if needed

-- AuditLog: Complete audit trail of system actions
CREATE TABLE [AuditLog] (
	[AuditLogId] INT PRIMARY KEY IDENTITY(1,1),
	[UserId] NVARCHAR(100) NOT NULL,
	[UserName] NVARCHAR(150),
	[UserRole] NVARCHAR(100),
	[HttpMethod] NVARCHAR(10) NOT NULL,
	[Endpoint] NVARCHAR(500) NOT NULL,
	[EntityType] NVARCHAR(100),
	[EntityId] NVARCHAR(100),
	[ServiceName] NVARCHAR(100),
	[StatusCode] INT,
	[Timestamp] DATETIME2 NOT NULL,
	[Details] NVARCHAR(2000)
);

CREATE INDEX IX_AuditLog_Timestamp ON [AuditLog]([Timestamp]);
CREATE INDEX IX_AuditLog_UserId ON [AuditLog]([UserId]);
CREATE INDEX IX_AuditLog_UserRole ON [AuditLog]([UserRole]);
CREATE INDEX IX_AuditLog_EntityType ON [AuditLog]([EntityType]);
CREATE INDEX IX_AuditLog_ServiceName ON [AuditLog]([ServiceName]);

-- Insert AuditLog Mock Data
INSERT INTO [AuditLog] ([UserId], [UserName], [UserRole], [HttpMethod], [Endpoint], [EntityType], [EntityId], [ServiceName], [StatusCode], [Timestamp], [Details]) VALUES
('user-manager-01', 'Rajesh Kumar', 'Admin', 'POST', '/api/suppliers', 'Supplier', '1', 'ProcurementService', 201, DATEADD(DAY, -7, GETUTCDATE()), 'Created new supplier: Cipla Limited'),
('user-warehouse-01', 'Priya Singh', 'WarehouseManager', 'GET', '/api/inventory/items', 'Item', NULL, 'InventoryService', 200, DATEADD(DAY, -5, GETUTCDATE()), 'Retrieved all items'),
('user-nurse-02', 'Amit Patel', 'Nurse', 'POST', '/api/consumption', 'ConsumptionRecord', '1', 'LogisticsService', 201, DATEADD(DAY, -3, GETUTCDATE()), 'Logged consumption: Insulin 15 units'),
('user-compliance-01', 'Sophia Johnson', 'ComplianceOfficer', 'GET', '/api/audit', NULL, NULL, 'AuditService', 200, DATEADD(DAY, -2, GETUTCDATE()), 'Audit report generated'),
('user-manager-02', 'Rajesh Kumar', 'Admin', 'PUT', '/api/purchase-orders/3', 'PurchaseOrder', '3', 'ProcurementService', 200, DATEADD(DAY, -1, GETUTCDATE()), 'Updated PO status to Approved'),
('user-warehouse-02', 'Neha Gupta', 'WarehouseStaff', 'POST', '/api/receipts', 'Receipt', '1', 'ProcurementService', 201, GETUTCDATE(), 'GRN created for PO#1'),
('user-logistics-01', 'Mohammed Hassan', 'LogisticsManager', 'POST', '/api/transfer-orders', 'TransferOrder', '1', 'LogisticsService', 201, DATEADD(HOUR, -12, GETUTCDATE()), 'Transfer order created from Hub to Apollo');

-- ============================================================================
-- USEFUL QUERIES FOR VERIFICATION
-- ============================================================================

-- Count records in each table
/*
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
*/

-- Find critical exceptions
/*
SELECT * FROM [ExceptionEvent] WHERE [Severity] = 'High' AND [Status] = 'Open';
*/

-- Check inventory levels vs safety stock
/*
SELECT 
	i.[ItemCode],
	i.[Name],
	ip.[Quantity],
	i.[SafetyStock],
	CASE WHEN ip.[Quantity] < i.[SafetyStock] THEN 'BELOW SAFETY STOCK' ELSE 'OK' END as [StockStatus]
FROM [InventoryPositions] ip
JOIN [Item] i ON ip.[ItemId] = i.[ItemId]
ORDER BY ip.[Quantity] ASC;
*/

-- Track temperature excursions
/*
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
-- END OF SCHEMA AND MOCK DATA
-- ============================================================================
