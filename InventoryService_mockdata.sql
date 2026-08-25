-- ============================================================================
-- MediPulseMicro - Inventory Service Mock Data
-- ============================================================================

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