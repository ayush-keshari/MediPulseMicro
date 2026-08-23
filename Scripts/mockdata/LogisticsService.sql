-- ============================================================================
-- LogisticsService Mock Data
-- Tables: TransferOrder, TransferOrderItem, ConsumptionRecord
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM [TransferOrder] WHERE [TransferOrderId] = 1)
BEGIN
    INSERT INTO [TransferOrder] ([FromFacilityId], [FromFacilityName], [ToFacilityId], [ToFacilityName], [RequestedBy], [RequestedDate], [Status]) VALUES
    (5, 'Central Distribution Hub Delhi', 1, 'Apollo Hospital Delhi', 'user-manager-01', DATEADD(DAY, -5, GETUTCDATE()), 'Completed'),
    (1, 'Apollo Hospital Delhi', 2, 'Max Healthcare Mumbai', 'user-manager-02', DATEADD(DAY, -2, GETUTCDATE()), 'InTransit'),
    (5, 'Central Distribution Hub Delhi', 3, 'Regional Medical Center Chennai', 'user-manager-03', DATEADD(HOUR, -12, GETUTCDATE()), 'Approved'),
    (2, 'Max Healthcare Mumbai', 4, 'Fortis Healthcare Bangalore', 'user-manager-01', GETUTCDATE(), 'Draft');
END

IF NOT EXISTS (SELECT 1 FROM [TransferOrderItem] WHERE [TransferOrderItemId] = 1)
BEGIN
    INSERT INTO [TransferOrderItem] ([TransferOrderId], [ItemId], [ItemName], [Quantity], [ToStorageZoneId]) VALUES
    (1, 1, 'Insulin Vial 100IU', 100, 4),
    (1, 4, 'Surgical Mask N95', 500, 4),
    (2, 2, 'Amoxicillin 500mg Tablet', 300, 5),
    (2, 3, 'Saline Solution 0.9%', 200, 5),
    (3, 7, 'Paracetamol 500mg', 400, 6),
    (4, 5, 'IV Cannula 20G', 150, 7);
END

IF NOT EXISTS (SELECT 1 FROM [ConsumptionRecord] WHERE [ConsumptionId] = 1)
BEGIN
    INSERT INTO [ConsumptionRecord] ([FacilityId], [WardId], [ItemId], [ItemName], [QuantityConsumed], [ConsumedDate], [ConsumedBy]) VALUES
    (1, 101, 1, 'Insulin Vial 100IU', 15, DATEADD(DAY, -5, GETUTCDATE()), 'user-nurse-01'),
    (1, 102, 2, 'Amoxicillin 500mg Tablet', 30, DATEADD(DAY, -4, GETUTCDATE()), 'user-nurse-02'),
    (2, 201, 3, 'Saline Solution 0.9%', 50, DATEADD(DAY, -3, GETUTCDATE()), 'user-nurse-03'),
    (2, 202, 4, 'Surgical Mask N95', 200, DATEADD(DAY, -2, GETUTCDATE()), 'user-staff-01'),
    (3, 301, 5, 'IV Cannula 20G', 75, DATEADD(DAY, -1, GETUTCDATE()), 'user-nurse-04'),
    (1, 103, 7, 'Paracetamol 500mg', 45, GETUTCDATE(), 'user-nurse-05');
END