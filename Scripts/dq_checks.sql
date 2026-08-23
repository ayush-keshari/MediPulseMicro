-- ============================================================================
-- MediPulseMicro - Data Quality Check Script
-- Performs row-count and null constraints checks per table
-- Only runs checks for tables that exist in the current database
-- ============================================================================

SET NOCOUNT ON;

DECLARE @ErrorCount INT = 0;
DECLARE @TableName NVARCHAR(128);
DECLARE @ExpectedMin INT;
DECLARE @ActualCount INT;
DECLARE @NullCount INT;

PRINT 'Starting data quality checks...';

-- ============================================================================
-- 1. FACILITY SERVICE
-- ============================================================================

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Facility')
BEGIN
    PRINT 'Checking FacilityService tables...';

    -- Facility
    SELECT @TableName = 'Facility', @ExpectedMin = 6;
    SELECT @ActualCount = COUNT(*) FROM [Facility];
    IF @ActualCount < @ExpectedMin
    BEGIN
        PRINT 'ERROR: Facility row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR);
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: Facility row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR);
    END

    -- Check NOT NULL columns: Name, Type, Region
    SELECT @NullCount = SUM(CASE WHEN [Name] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Type] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Region] IS NULL THEN 1 ELSE 0 END)
    FROM [Facility];
    IF @NullCount > 0
    BEGIN
        PRINT 'ERROR: Facility has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (Name, Type, Region)';
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: Facility mandatory columns (Name, Type, Region) have no NULL values';
    END
END
ELSE
BEGIN
    PRINT 'INFO: Facility table not found. Skipping FacilityService checks.';
END

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'StorageZone')
BEGIN
    -- StorageZone
    SELECT @TableName = 'StorageZone', @ExpectedMin = 8;
    SELECT @ActualCount = COUNT(*) FROM [StorageZone];
    IF @ActualCount < @ExpectedMin
    BEGIN
        PRINT 'ERROR: StorageZone row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR);
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: StorageZone row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR);
    END

    -- Check NOT NULL columns: FacilityID, Name, TemperatureProfile, Capacity
    SELECT @NullCount = SUM(CASE WHEN [FacilityID] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Name] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [TemperatureProfile] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Capacity] IS NULL THEN 1 ELSE 0 END)
    FROM [StorageZone];
    IF @NullCount > 0
    BEGIN
        PRINT 'ERROR: StorageZone has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (FacilityID, Name, TemperatureProfile, Capacity)';
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: StorageZone mandatory columns (FacilityID, Name, TemperatureProfile, Capacity) have no NULL values';
    END
END
ELSE
BEGIN
    PRINT 'INFO: StorageZone table not found. Skipping StorageZone checks.';
END

-- ============================================================================
-- 2. INVENTORY SERVICE
-- ============================================================================

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Item')
BEGIN
    PRINT 'Checking InventoryService tables...';

    -- Item
    SELECT @TableName = 'Item', @ExpectedMin = 8;
    SELECT @ActualCount = COUNT(*) FROM [Item];
    IF @ActualCount < @ExpectedMin
    BEGIN
        PRINT 'ERROR: Item row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR);
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: Item row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR);
    END

    -- Check NOT NULL columns: ItemCode, Name, Category, Unit, SafetyStock
    SELECT @NullCount = SUM(CASE WHEN [ItemCode] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Name] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Category] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Unit] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [SafetyStock] IS NULL THEN 1 ELSE 0 END)
    FROM [Item];
    IF @NullCount > 0
    BEGIN
        PRINT 'ERROR: Item has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (ItemCode, Name, Category, Unit, SafetyStock)';
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: Item mandatory columns (ItemCode, Name, Category, Unit, SafetyStock) have no NULL values';
    END
END
ELSE
BEGIN
    PRINT 'INFO: Item table not found. Skipping Item checks.';
END

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'InventoryPositions')
BEGIN
    -- InventoryPositions
    SELECT @TableName = 'InventoryPositions', @ExpectedMin = 8;
    SELECT @ActualCount = COUNT(*) FROM [InventoryPositions];
    IF @ActualCount < @ExpectedMin
    BEGIN
        PRINT 'ERROR: InventoryPositions row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR);
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: InventoryPositions row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR);
    END

    -- Check NOT NULL columns: ItemId, LotId, ExpiryDate, Quantity, FacilityId, StorageZoneId, SafetyStock
    SELECT @NullCount = SUM(CASE WHEN [ItemId] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [LotId] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [ExpiryDate] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Quantity] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [FacilityId] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [StorageZoneId] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [SafetyStock] IS NULL THEN 1 ELSE 0 END)
    FROM [InventoryPositions];
    IF @NullCount > 0
    BEGIN
        PRINT 'ERROR: InventoryPositions has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (ItemId, LotId, ExpiryDate, Quantity, FacilityId, StorageZoneId, SafetyStock)';
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: InventoryPositions mandatory columns (ItemId, LotId, ExpiryDate, Quantity, FacilityId, StorageZoneId, SafetyStock) have no NULL values';
    END
END
ELSE
BEGIN
    PRINT 'INFO: InventoryPositions table not found. Skipping InventoryPositions checks.';
END

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ExceptionEvent')
BEGIN
    -- ExceptionEvent
    SELECT @TableName = 'ExceptionEvent', @ExpectedMin = 4;
    SELECT @ActualCount = COUNT(*) FROM [ExceptionEvent];
    IF @ActualCount < @ExpectedMin
    BEGIN
        PRINT 'ERROR: ExceptionEvent row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR);
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: ExceptionEvent row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR);
    END

    -- Check NOT NULL columns: Type, ReferenceType, ReferenceId, Severity, Status, DetectedDate
    SELECT @NullCount = SUM(CASE WHEN [Type] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [ReferenceType] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [ReferenceId] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Severity] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Status] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [DetectedDate] IS NULL THEN 1 ELSE 0 END)
    FROM [ExceptionEvent];
    IF @NullCount > 0
    BEGIN
        PRINT 'ERROR: ExceptionEvent has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (Type, ReferenceType, ReferenceId, Severity, Status, DetectedDate)';
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: ExceptionEvent mandatory columns (Type, ReferenceType, ReferenceId, Severity, Status, DetectedDate) have no NULL values';
    END
END
ELSE
BEGIN
    PRINT 'INFO: ExceptionEvent table not found. Skipping ExceptionEvent checks.';
END

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'RecallAction')
BEGIN
    -- RecallAction
    SELECT @TableName = 'RecallAction', @ExpectedMin = 4;
    SELECT @ActualCount = COUNT(*) FROM [RecallAction];
    IF @ActualCount < @ExpectedMin
    BEGIN
        PRINT 'ERROR: RecallAction row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR);
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: RecallAction row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR);
    END

    -- Check NOT NULL columns: ExceptionId, OwnerId, ActionDescription, DueDate, Status
    SELECT @NullCount = SUM(CASE WHEN [ExceptionId] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [OwnerId] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [ActionDescription] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [DueDate] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Status] IS NULL THEN 1 ELSE 0 END)
    FROM [RecallAction];
    IF @NullCount > 0
    BEGIN
        PRINT 'ERROR: RecallAction has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (ExceptionId, OwnerId, ActionDescription, DueDate, Status)';
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: RecallAction mandatory columns (ExceptionId, OwnerId, ActionDescription, DueDate, Status) have no NULL values';
    END
END
ELSE
BEGIN
    PRINT 'INFO: RecallAction table not found. Skipping RecallAction checks.';
END

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Forecast')
BEGIN
    -- Forecast
    SELECT @TableName = 'Forecast', @ExpectedMin = 6;
    SELECT @ActualCount = COUNT(*) FROM [Forecast];
    IF @ActualCount < @ExpectedMin
    BEGIN
        PRINT 'ERROR: Forecast row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR);
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: Forecast row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR);
    END

    -- Check NOT NULL columns: ItemId, FacilityId, Period, ForecastQuantity, GeneratedDate
    SELECT @NullCount = SUM(CASE WHEN [ItemId] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [FacilityId] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Period] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [ForecastQuantity] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [GeneratedDate] IS NULL THEN 1 ELSE 0 END)
    FROM [Forecast];
    IF @NullCount > 0
    BEGIN
        PRINT 'ERROR: Forecast has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (ItemId, FacilityId, Period, ForecastQuantity, GeneratedDate)';
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: Forecast mandatory columns (ItemId, FacilityId, Period, ForecastQuantity, GeneratedDate) have no NULL values';
    END
END
ELSE
BEGIN
    PRINT 'INFO: Forecast table not found. Skipping Forecast checks.';
END

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ReplenishmentPlan')
BEGIN
    -- ReplenishmentPlan
    SELECT @TableName = 'ReplenishmentPlan', @ExpectedMin = 4;
    SELECT @ActualCount = COUNT(*) FROM [ReplenishmentPlan];
    IF @ActualCount < @ExpectedMin
    BEGIN
        PRINT 'ERROR: ReplenishmentPlan row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR);
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: ReplenishmentPlan row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR);
    END

    -- Check NOT NULL columns: ItemId, FacilityId, SuggestedOrderQty, Priority, Status, GeneratedDate
    SELECT @NullCount = SUM(CASE WHEN [ItemId] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [FacilityId] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [SuggestedOrderQty] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Priority] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Status] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [GeneratedDate] IS NULL THEN 1 ELSE 0 END)
    FROM [ReplenishmentPlan];
    IF @NullCount > 0
    BEGIN
        PRINT 'ERROR: ReplenishmentPlan has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (ItemId, FacilityId, SuggestedOrderQty, Priority, Status, GeneratedDate)';
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: ReplenishmentPlan mandatory columns (ItemId, FacilityId, SuggestedOrderQty, Priority, Status, GeneratedDate) have no NULL values';
    END
END
ELSE
BEGIN
    PRINT 'INFO: ReplenishmentPlan table not found. Skipping ReplenishmentPlan checks.';
END

-- ============================================================================
-- 3. PROCUREMENT SERVICE
-- ============================================================================

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Supplier')
BEGIN
    PRINT 'Checking ProcurementService tables...';

    -- Supplier
    SELECT @TableName = 'Supplier', @ExpectedMin = 7;
    SELECT @ActualCount = COUNT(*) FROM [Supplier];
    IF @ActualCount < @ExpectedMin
    BEGIN
        PRINT 'ERROR: Supplier row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR);
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: Supplier row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR);
    END

    -- Check NOT NULL columns: Name, SupplierType, Status
    SELECT @NullCount = SUM(CASE WHEN [Name] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [SupplierType] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [STATUS] IS NULL THEN 1 ELSE 0 END)
    FROM [Supplier];
    IF @NullCount > 0
    BEGIN
        PRINT 'ERROR: Supplier has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (Name, SupplierType, Status)';
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: Supplier mandatory columns (Name, SupplierType, Status) have no NULL values';
    END
END
ELSE
BEGIN
    PRINT 'INFO: Supplier table not found. Skipping Supplier checks.';
END

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PurchaseOrder')
BEGIN
    -- PurchaseOrder
    SELECT @TableName = 'PurchaseOrder', @ExpectedMin = 6;
    SELECT @ActualCount = COUNT(*) FROM [PurchaseOrder];
    IF @ActualCount < @ExpectedMin
    BEGIN
        PRINT 'ERROR: PurchaseOrder row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR);
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: PurchaseOrder row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR);
    END

    -- Check NOT NULL columns: SupplierID, OrderDate, Status
    SELECT @NullCount = SUM(CASE WHEN [SupplierID] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [OrderDate] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Status] IS NULL THEN 1 ELSE 0 END)
    FROM [PurchaseOrder];
    IF @NullCount > 0
    BEGIN
        PRINT 'ERROR: PurchaseOrder has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (SupplierID, OrderDate, Status)';
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: PurchaseOrder mandatory columns (SupplierID, OrderDate, Status) have no NULL values';
    END
END
ELSE
BEGIN
    PRINT 'INFO: PurchaseOrder table not found. Skipping PurchaseOrder checks.';
END

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Receipt')
BEGIN
    -- Receipt
    SELECT @TableName = 'Receipt', @ExpectedMin = 6;
    SELECT @ActualCount = COUNT(*) FROM [Receipt];
    IF @ActualCount < @ExpectedMin
    BEGIN
        PRINT 'ERROR: Receipt row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR);
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: Receipt row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR);
    END

    -- Check NOT NULL columns: POID, SupplierLot, ReceivedDate, ReceivedBy, QualityStatus, QuantityReceived
    SELECT @NullCount = SUM(CASE WHEN [POID] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [SupplierLot] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [ReceivedDate] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [ReceivedBy] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [QualityStatus] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [QuantityReceived] IS NULL THEN 1 ELSE 0 END)
    FROM [Receipt];
    IF @NullCount > 0
    BEGIN
        PRINT 'ERROR: Receipt has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (POID, SupplierLot, ReceivedDate, ReceivedBy, QualityStatus, QuantityReceived)';
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: Receipt mandatory columns (POID, SupplierLot, ReceivedDate, ReceivedBy, QualityStatus, QuantityReceived) have no NULL values';
    END
END
ELSE
BEGIN
    PRINT 'INFO: Receipt table not found. Skipping Receipt checks.';
END

-- ============================================================================
-- 4. LOGISTICS SERVICE
-- ============================================================================

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'TransferOrder')
BEGIN
    PRINT 'Checking LogisticsService tables...';

    -- TransferOrder
    SELECT @TableName = 'TransferOrder', @ExpectedMin = 4;
    SELECT @ActualCount = COUNT(*) FROM [TransferOrder];
    IF @ActualCount < @ExpectedMin
    BEGIN
        PRINT 'ERROR: TransferOrder row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR);
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: TransferOrder row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR);
    END

    -- Check NOT NULL columns: FromFacilityId, FromFacilityName, ToFacilityId, ToFacilityName, RequestedBy, RequestedDate, Status
    SELECT @NullCount = SUM(CASE WHEN [FromFacilityId] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [FromFacilityName] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [ToFacilityId] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [ToFacilityName] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [RequestedBy] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [RequestedDate] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Status] IS NULL THEN 1 ELSE 0 END)
    FROM [TransferOrder];
    IF @NullCount > 0
    BEGIN
        PRINT 'ERROR: TransferOrder has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (FromFacilityId, FromFacilityName, ToFacilityId, ToFacilityName, RequestedBy, RequestedDate, Status)';
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: TransferOrder mandatory columns (FromFacilityId, FromFacilityName, ToFacilityId, ToFacilityName, RequestedBy, RequestedDate, Status) have no NULL values';
    END
END
ELSE
BEGIN
    PRINT 'INFO: TransferOrder table not found. Skipping TransferOrder checks.';
END

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'TransferOrderItem')
BEGIN
    -- TransferOrderItem
    SELECT @TableName = 'TransferOrderItem', @ExpectedMin = 6;
    SELECT @ActualCount = COUNT(*) FROM [TransferOrderItem];
    IF @ActualCount < @ExpectedMin
    BEGIN
        PRINT 'ERROR: TransferOrderItem row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR);
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: TransferOrderItem row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR);
    END

    -- Check NOT NULL columns: TransferOrderId, ItemId, ItemName, Quantity, ToStorageZoneId
    SELECT @NullCount = SUM(CASE WHEN [TransferOrderId] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [ItemId] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [ItemName] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Quantity] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [ToStorageZoneId] IS NULL THEN 1 ELSE 0 END)
    FROM [TransferOrderItem];
    IF @NullCount > 0
    BEGIN
        PRINT 'ERROR: TransferOrderItem has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (TransferOrderId, ItemId, ItemName, Quantity, ToStorageZoneId)';
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: TransferOrderItem mandatory columns (TransferOrderId, ItemId, ItemName, Quantity, ToStorageZoneId) have no NULL values';
    END
END
ELSE
BEGIN
    PRINT 'INFO: TransferOrderItem table not found. Skipping TransferOrderItem checks.';
END

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ConsumptionRecord')
BEGIN
    -- ConsumptionRecord
    SELECT @TableName = 'ConsumptionRecord', @ExpectedMin = 6;
    SELECT @ActualCount = COUNT(*) FROM [ConsumptionRecord];
    IF @ActualCount < @ExpectedMin
    BEGIN
        PRINT 'ERROR: ConsumptionRecord row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR);
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: ConsumptionRecord row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR);
    END

    -- Check NOT NULL columns: FacilityId, WardId, ItemId, ItemName, QuantityConsumed, ConsumedDate, ConsumedBy
    SELECT @NullCount = SUM(CASE WHEN [FacilityId] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [WardId] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [ItemId] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [ItemName] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [QuantityConsumed] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [ConsumedDate] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [ConsumedBy] IS NULL THEN 1 ELSE 0 END)
    FROM [ConsumptionRecord];
    IF @NullCount > 0
    BEGIN
        PRINT 'ERROR: ConsumptionRecord has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (FacilityId, WardId, ItemId, ItemName, QuantityConsumed, ConsumedDate, ConsumedBy)';
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: ConsumptionRecord mandatory columns (FacilityId, WardId, ItemId, ItemName, QuantityConsumed, ConsumedDate, ConsumedBy) have no NULL values';
    END
END
ELSE
BEGIN
    PRINT 'INFO: ConsumptionRecord table not found. Skipping ConsumptionRecord checks.';
END

-- ============================================================================
-- 5. TELEMETRY SERVICE
-- ============================================================================

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'SensorDevice')
BEGIN
    PRINT 'Checking TelemetryService tables...';

    -- SensorDevice
    SELECT @TableName = 'SensorDevice', @ExpectedMin = 8;
    SELECT @ActualCount = COUNT(*) FROM [SensorDevice];
    IF @ActualCount < @ExpectedMin
    BEGIN
        PRINT 'ERROR: SensorDevice row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR);
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: SensorDevice row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR);
    END

    -- Check NOT NULL columns: DeviceName, DeviceType, AssignedTo, Status
    SELECT @NullCount = SUM(CASE WHEN [DeviceName] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [DeviceType] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [AssignedTo] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Status] IS NULL THEN 1 ELSE 0 END)
    FROM [SensorDevice];
    IF @NullCount > 0
    BEGIN
        PRINT 'ERROR: SensorDevice has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (DeviceName, DeviceType, AssignedTo, Status)';
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: SensorDevice mandatory columns (DeviceName, DeviceType, AssignedTo, Status) have no NULL values';
    END
END
ELSE
BEGIN
    PRINT 'INFO: SensorDevice table not found. Skipping SensorDevice checks.';
END

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'TelemetryRecord')
BEGIN
    -- TelemetryRecord
    SELECT @TableName = 'TelemetryRecord', @ExpectedMin = 12;
    SELECT @ActualCount = COUNT(*) FROM [TelemetryRecord];
    IF @ActualCount < @ExpectedMin
    BEGIN
        PRINT 'ERROR: TelemetryRecord row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR);
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: TelemetryRecord row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR);
    END

    -- Check NOT NULL columns: SensorID, Timestamp, Temperature, Humidity, Location
    SELECT @NullCount = SUM(CASE WHEN [SensorID] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Timestamp] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Temperature] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Humidity] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Location] IS NULL THEN 1 ELSE 0 END)
    FROM [TelemetryRecord];
    IF @NullCount > 0
    BEGIN
        PRINT 'ERROR: TelemetryRecord has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (SensorID, Timestamp, Temperature, Humidity, Location)';
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: TelemetryRecord mandatory columns (SensorID, Timestamp, Temperature, Humidity, Location) have no NULL values';
    END
END
ELSE
BEGIN
    PRINT 'INFO: TelemetryRecord table not found. Skipping TelemetryRecord checks.';
END

-- ============================================================================
-- 6. NOTIFICATION SERVICE
-- ============================================================================

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Notification')
BEGIN
    PRINT 'Checking NotificationService tables...';

    -- Notification
    SELECT @TableName = 'Notification', @ExpectedMin = 6;
    SELECT @ActualCount = COUNT(*) FROM [Notification];
    IF @ActualCount < @ExpectedMin
    BEGIN
        PRINT 'ERROR: Notification row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR);
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: Notification row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR);
    END

    -- Check NOT NULL columns: UserId, Category, Title, Message, IsRead, CreatedAt
    SELECT @NullCount = SUM(CASE WHEN [UserId] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Category] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Title] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Message] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [IsRead] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [CreatedAt] IS NULL THEN 1 ELSE 0 END)
    FROM [Notification];
    IF @NullCount > 0
    BEGIN
        PRINT 'ERROR: Notification has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (UserId, Category, Title, Message, IsRead, CreatedAt)';
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: Notification mandatory columns (UserId, Category, Title, Message, IsRead, CreatedAt) have no NULL values';
    END
END
ELSE
BEGIN
    PRINT 'INFO: Notification table not found. Skipping Notification checks.';
END

-- ============================================================================
-- 7. AUTH SERVICE
-- ============================================================================

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'User')
BEGIN
    PRINT 'Checking AuthService tables...';

    -- User
    SELECT @TableName = 'User', @ExpectedMin = 7;
    SELECT @ActualCount = COUNT(*) FROM [User];
    IF @ActualCount < @ExpectedMin
    BEGIN
        PRINT 'ERROR: User row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR);
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: User row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR);
    END

    -- Check NOT NULL columns: Name, Role, Email, Password (Phone can be null)
    SELECT @NullCount = SUM(CASE WHEN [Name] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Role] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Email] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Password] IS NULL THEN 1 ELSE 0 END)
    FROM [User];
    IF @NullCount > 0
    BEGIN
        PRINT 'ERROR: User has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (Name, Role, Email, Password)';
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: User mandatory columns (Name, Role, Email, Password) have no NULL values';
    END
END
ELSE
BEGIN
    PRINT 'INFO: User table not found. Skipping User checks.';
END

-- ============================================================================
-- 8. AUDIT SERVICE (MedipulseAudit database)
-- ============================================================================

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLog')
BEGIN
    PRINT 'Checking AuditService tables...';

    -- AuditLog
    SELECT @TableName = 'AuditLog', @ExpectedMin = 7;
    SELECT @ActualCount = COUNT(*) FROM [AuditLog];
    IF @ActualCount < @ExpectedMin
    BEGIN
        PRINT 'ERROR: AuditLog row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR);
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: AuditLog row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR);
    END

    -- Check NOT NULL columns: UserId, HttpMethod, Endpoint, Timestamp, Details
    SELECT @NullCount = SUM(CASE WHEN [UserId] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [HttpMethod] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Endpoint] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Timestamp] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Details] IS NULL THEN 1 ELSE 0 END)
    FROM [AuditLog];
    IF @NullCount > 0
    BEGIN
        PRINT 'ERROR: AuditLog has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (UserId, HttpMethod, Endpoint, Timestamp, Details)';
        SET @ErrorCount = @ErrorCount + 1;
    END
    ELSE
    BEGIN
        PRINT 'OK: AuditLog mandatory columns (UserId, HttpMethod, Endpoint, Timestamp, Details) have no NULL values';
    END
END
ELSE
BEGIN
    PRINT 'INFO: AuditLog table not found in current database. Skipping AuditService checks.';
END

-- ============================================================================
-- FINAL RESULT
-- ============================================================================

IF @ErrorCount = 0
BEGIN
    PRINT 'SUCCESS: All data quality checks passed!';
END
ELSE
BEGIN
    PRINT 'FAILURE: Found ' + CAST(@ErrorCount AS NVARCHAR) + ' data quality error(s)!';
END

-- Return error count as result set for CI to consume
SELECT @ErrorCount AS ErrorCount;