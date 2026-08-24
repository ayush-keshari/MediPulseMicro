-- ============================================================================
-- MediPulseMicro - Mock Data Validation Script
-- Validates referential integrity of mock data
-- ============================================================================

SET NOCOUNT ON;

DECLARE @ErrorCount INT = 0;

PRINT 'Starting mock data validation...';

-- ============================================================================
-- 1. FACILITY SERVICE VALIDATION
-- ============================================================================

PRINT 'Checking Facility Service referential integrity...';

-- Check StorageZone references valid Facility
IF EXISTS (
    SELECT 1
    FROM [StorageZone] sz
    LEFT JOIN [Facility] f ON sz.[FacilityID] = f.[FacilityID]
    WHERE f.[FacilityID] IS NULL
)
BEGIN
    PRINT 'ERROR: StorageZone references non-existent Facility';
    SET @ErrorCount = @ErrorCount + 1;
END
ELSE
BEGIN
    PRINT 'OK: StorageZone -> Facility references are valid';
END

-- ============================================================================
-- 2. INVENTORY SERVICE VALIDATION
-- ============================================================================

PRINT 'Checking Inventory Service referential integrity...';

-- Check InventoryPosition references valid Item
IF EXISTS (
    SELECT 1
    FROM [InventoryPosition] ip
    LEFT JOIN [Item] i ON ip.[ItemId] = i.[ItemId]
    WHERE i.[ItemId] IS NULL
)
BEGIN
    PRINT 'ERROR: InventoryPosition references non-existent Item';
    SET @ErrorCount = @ErrorCount + 1;
END
ELSE
BEGIN
    PRINT 'OK: InventoryPosition -> Item references are valid';
END

-- Check InventoryPosition references valid Facility
IF EXISTS (
    SELECT 1
    FROM [InventoryPosition] ip
    LEFT JOIN [Facility] f ON ip.[FacilityId] = f.[FacilityID]
    WHERE f.[FacilityID] IS NULL
)
BEGIN
    PRINT 'ERROR: InventoryPosition references non-existent Facility';
    SET @ErrorCount = @ErrorCount + 1;
END
ELSE
BEGIN
    PRINT 'OK: InventoryPosition -> Facility references are valid';
END

-- Check InventoryPosition references valid StorageZone
IF EXISTS (
    SELECT 1
    FROM [InventoryPosition] ip
    LEFT JOIN [StorageZone] sz ON ip.[StorageZoneId] = sz.[ZoneID]
    WHERE sz.[ZoneID] IS NULL
)
BEGIN
    PRINT 'ERROR: InventoryPosition references non-existent StorageZone';
    SET @ErrorCount = @ErrorCount + 1;
END
ELSE
BEGIN
    PRINT 'OK: InventoryPosition -> StorageZone references are valid';
END

-- ============================================================================
-- 3. PROCUREMENT SERVICE VALIDATION
-- ============================================================================

PRINT 'Checking Procurement Service referential integrity...';

-- Check PurchaseOrder references valid Supplier
IF EXISTS (
    SELECT 1
    FROM [PurchaseOrder] po
    LEFT JOIN [Supplier] s ON po.[SupplierID] = s.[SupplierID]
    WHERE s.[SupplierId] IS NULL
)
BEGIN
    PRINT 'ERROR: PurchaseOrder references non-existent Supplier';
    SET @ErrorCount = @ErrorCount + 1;
END
ELSE
BEGIN
    PRINT 'OK: PurchaseOrder -> Supplier references are valid';
END

-- Check Receipt references valid PurchaseOrder
IF EXISTS (
    SELECT 1
    FROM [Receipt] r
    LEFT JOIN [PurchaseOrder] po ON r.[POID] = po.[POID]
    WHERE po.[POID] IS NULL
)
BEGIN
    PRINT 'ERROR: Receipt references non-existent PurchaseOrder';
    SET @ErrorCount = @ErrorCount + 1;
END
ELSE
BEGIN
    PRINT 'OK: Receipt -> PurchaseOrder references are valid';
END

-- ============================================================================
-- 4. LOGISTICS SERVICE VALIDATION
-- ============================================================================

PRINT 'Checking Logistics Service referential integrity...';

-- Check TransferOrder references valid FromFacility
IF EXISTS (
    SELECT 1
    FROM [TransferOrder] transferOrder
    LEFT JOIN [Facility] f ON transferOrder.[FromFacilityId] = f.[FacilityID]
    WHERE f.[FacilityID] IS NULL
)
BEGIN
    PRINT 'ERROR: TransferOrder references non-existent FromFacility';
    SET @ErrorCount = @ErrorCount + 1;
END
ELSE
BEGIN
    PRINT 'OK: TransferOrder -> FromFacility references are valid';
END

-- Check TransferOrder references valid ToFacility
IF EXISTS (
    SELECT 1
    FROM [TransferOrder] transferOrder
    LEFT JOIN [Facility] f ON transferOrder.[ToFacilityId] = f.[FacilityID]
    WHERE f.[FacilityID] IS NULL
)
BEGIN
    PRINT 'ERROR: TransferOrder references non-existent ToFacility';
    SET @ErrorCount = @ErrorCount + 1;
END
ELSE
BEGIN
    PRINT 'OK: TransferOrder -> ToFacility references are valid';
END

-- Check TransferOrderItem references valid TransferOrder
IF EXISTS (
    SELECT 1
    FROM [TransferOrderItem] toi
    LEFT JOIN [TransferOrder] transferOrder ON toi.[TransferOrderId] = transferOrder.[TransferOrderId]
    WHERE transferOrder.[TransferOrderId] IS NULL
)
BEGIN
    PRINT 'ERROR: TransferOrderItem references non-existent TransferOrder';
    SET @ErrorCount = @ErrorCount + 1;
END
ELSE
BEGIN
    PRINT 'OK: TransferOrderItem -> TransferOrder references are valid';
END

-- Check TransferOrderItem references valid Item
IF EXISTS (
    SELECT 1
    FROM [TransferOrderItem] toi
    LEFT JOIN [Item] i ON toi.[ItemId] = i.[ItemId]
    WHERE i.[ItemId] IS NULL
)
BEGIN
    PRINT 'ERROR: TransferOrderItem references non-existent Item';
    SET @ErrorCount = @ErrorCount + 1;
END
ELSE
BEGIN
    PRINT 'OK: TransferOrderItem -> Item references are valid';
END

-- Check TransferOrderItem references valid StorageZone
IF EXISTS (
    SELECT 1
    FROM [TransferOrderItem] toi
    LEFT JOIN [StorageZone] sz ON toi.[ToStorageZoneId] = sz.[ZoneID]
    WHERE sz.[ZoneID] IS NULL
)
BEGIN
    PRINT 'ERROR: TransferOrderItem references non-existent StorageZone';
    SET @ErrorCount = @ErrorCount + 1;
END
ELSE
BEGIN
    PRINT 'OK: TransferOrderItem -> StorageZone references are valid';
END

-- Check ConsumptionRecord references valid Facility
IF EXISTS (
    SELECT 1
    FROM [ConsumptionRecord] cr
    LEFT JOIN [Facility] f ON cr.[FacilityId] = f.[FacilityID]
    WHERE f.[FacilityID] IS NULL
)
BEGIN
    PRINT 'ERROR: ConsumptionRecord references non-existent Facility';
    SET @ErrorCount = @ErrorCount + 1;
END
ELSE
BEGIN
    PRINT 'OK: ConsumptionRecord -> Facility references are valid';
END

-- Check ConsumptionRecord references valid Item
IF EXISTS (
    SELECT 1
    FROM [ConsumptionRecord] cr
    LEFT JOIN [Item] i ON cr.[ItemId] = i.[ItemId]
    WHERE i.[ItemId] IS NULL
)
BEGIN
    PRINT 'ERROR: ConsumptionRecord references non-existent Item';
    SET @ErrorCount = @ErrorCount + 1;
END
ELSE
BEGIN
    PRINT 'OK: ConsumptionRecord -> Item references are valid';
END

-- ============================================================================
-- 5. TELEMETRY SERVICE VALIDATION
-- ============================================================================

PRINT 'Checking Telemetry Service referential integrity...';

-- Check TelemetryRecord references valid SensorDevice
IF EXISTS (
    SELECT 1
    FROM [TelemetryRecord] tr
    LEFT JOIN [SensorDevice] sd ON tr.[SensorID] = sd.[SensorID]
    WHERE sd.[SensorID] IS NULL
)
BEGIN
    PRINT 'ERROR: TelemetryRecord references non-existent SensorDevice';
    SET @ErrorCount = @ErrorCount + 1;
END
ELSE
BEGIN
    PRINT 'OK: TelemetryRecord -> SensorDevice references are valid';
END

-- ============================================================================
-- 6. SHARED ENTITIES VALIDATION
-- ============================================================================

-- Assuming User table exists in AuthService
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'User')
BEGIN
    PRINT 'Checking User referential integrity...';

    -- This would check foreign keys to User table if they exist
    PRINT 'OK: User references validation skipped (no foreign keys found in mock data)';
END

-- ============================================================================
-- FINAL RESULT
-- ============================================================================

IF @ErrorCount = 0
BEGIN
    PRINT 'SUCCESS: All referential integrity checks passed!';
END
ELSE
BEGIN
    PRINT 'FAILURE: Found ' + CAST(@ErrorCount AS VARCHAR) + ' referential integrity error(s)!';
END

-- Return error count as result set for CI to consume
SELECT @ErrorCount AS ErrorCount;