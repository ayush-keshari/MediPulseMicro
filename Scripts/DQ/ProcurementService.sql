-- ============================================================================
-- MediPulseMicro - Data Quality Check for Procurement Service
-- ============================================================================
SET NOCOUNT ON;

DECLARE @Results TABLE (
    CheckName NVARCHAR(200),
    Domain NVARCHAR(50),
    Status INT, -- 0 PASS, 1 FAIL
    ExpectedCondition NVARCHAR(400),
    ActualResult NVARCHAR(400),
    Timestamp DATETIME2,
    Message NVARCHAR(400)
);

DECLARE @TableName NVARCHAR(128);
DECLARE @ExpectedMin INT;
DECLARE @ActualCount INT;
DECLARE @NullCount INT;

-- Supplier
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Supplier')
BEGIN
    -- Supplier row count
    SELECT @TableName = 'Supplier', @ExpectedMin = 7;
    SELECT @ActualCount = COUNT(*) FROM [Supplier];
    IF @ActualCount >= @ExpectedMin
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('Supplier.RowCount', 'ProcurementService', 0, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'OK: Supplier row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('Supplier.RowCount', 'ProcurementService', 1, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'ERROR: Supplier row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END

    -- Check NOT NULL columns: Name, SupplierType, Status
    SELECT @NullCount = SUM(CASE WHEN [Name] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [SupplierType] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [STATUS] IS NULL THEN 1 ELSE 0 END)
    FROM [Supplier];
    IF @NullCount = 0
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('Supplier.NotNull_Name_SupplierType_Status', 'ProcurementService', 0, 'No NULL values in Name, SupplierType, Status', 'NULL count = 0', GETDATE(), 'OK: Supplier mandatory columns (Name, SupplierType, Status) have no NULL values');
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('Supplier.NotNull_Name_SupplierType_Status', 'ProcurementService', 1, 'No NULL values in Name, SupplierType, Status', 'NULL count = ' + CAST(@NullCount AS NVARCHAR), GETDATE(), 'ERROR: Supplier has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (Name, SupplierType, Status)');
    END
END
ELSE
BEGIN
    INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
    VALUES ('Supplier.TableExists', 'ProcurementService', 0, 'Table Supplier exists', 'Table not found', GETDATE(), 'INFO: Supplier table not found. Skipping Supplier checks.');
END

-- PurchaseOrder
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PurchaseOrder')
BEGIN
    -- PurchaseOrder row count
    SELECT @TableName = 'PurchaseOrder', @ExpectedMin = 6;
    SELECT @ActualCount = COUNT(*) FROM [PurchaseOrder];
    IF @ActualCount >= @ExpectedMin
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('PurchaseOrder.RowCount', 'ProcurementService', 0, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'OK: PurchaseOrder row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('PurchaseOrder.RowCount', 'ProcurementService', 1, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'ERROR: PurchaseOrder row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END

    -- Check NOT NULL columns: SupplierID, OrderDate, Status
    SELECT @NullCount = SUM(CASE WHEN [SupplierID] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [OrderDate] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Status] IS NULL THEN 1 ELSE 0 END)
    FROM [PurchaseOrder];
    IF @NullCount = 0
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('PurchaseOrder.NotNull_SupplierID_OrderDate_Status', 'ProcurementService', 0, 'No NULL values in SupplierID, OrderDate, Status', 'NULL count = 0', GETDATE(), 'OK: PurchaseOrder mandatory columns (SupplierID, OrderDate, Status) have no NULL values');
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('PurchaseOrder.NotNull_SupplierID_OrderDate_Status', 'ProcurementService', 1, 'No NULL values in SupplierID, OrderDate, Status', 'NULL count = ' + CAST(@NullCount AS NVARCHAR), GETDATE(), 'ERROR: PurchaseOrder has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (SupplierID, OrderDate, Status)');
    END
END
ELSE
BEGIN
    INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
    VALUES ('PurchaseOrder.TableExists', 'ProcurementService', 0, 'Table PurchaseOrder exists', 'Table not found', GETDATE(), 'INFO: PurchaseOrder table not found. Skipping PurchaseOrder checks.');
END

-- Receipt
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Receipt')
BEGIN
    -- Receipt row count
    SELECT @TableName = 'Receipt', @ExpectedMin = 6;
    SELECT @ActualCount = COUNT(*) FROM [Receipt];
    IF @ActualCount >= @ExpectedMin
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('Receipt.RowCount', 'ProcurementService', 0, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'OK: Receipt row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('Receipt.RowCount', 'ProcurementService', 1, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'ERROR: Receipt row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END

    -- Check NOT NULL columns: POID, SupplierLot, ReceivedDate, ReceivedBy, QualityStatus, QuantityReceived
    SELECT @NullCount = SUM(CASE WHEN [POID] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [SupplierLot] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [ReceivedDate] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [ReceivedBy] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [QualityStatus] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [QuantityReceived] IS NULL THEN 1 ELSE 0 END)
    FROM [Receipt];
    IF @NullCount = 0
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('Receipt.NotNull_POID_SupplierLot_ReceivedDate_ReceivedBy_QualityStatus_QuantityReceived', 'ProcurementService', 0, 'No NULL values in POID, SupplierLot, ReceivedDate, ReceivedBy, QualityStatus, QuantityReceived', 'NULL count = 0', GETDATE(), 'OK: Receipt mandatory columns (POID, SupplierLot, ReceivedDate, ReceivedBy, QualityStatus, QuantityReceived) have no NULL values');
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('Receipt.NotNull_POID_SupplierLot_ReceivedDate_ReceivedBy_QualityStatus_QuantityReceived', 'ProcurementService', 1, 'No NULL values in POID, SupplierLot, ReceivedDate, ReceivedBy, QualityStatus, QuantityReceived', 'NULL count = ' + CAST(@NullCount AS NVARCHAR), GETDATE(), 'ERROR: Receipt has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (POID, SupplierLot, ReceivedDate, ReceivedBy, QualityStatus, QuantityReceived)');
    END
END
ELSE
BEGIN
    INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
    VALUES ('Receipt.TableExists', 'ProcurementService', 0, 'Table Receipt exists', 'Table not found', GETDATE(), 'INFO: Receipt table not found. Skipping Receipt checks.');
END

-- Output results as JSON
SELECT * FROM @Results FOR JSON PATH, ROOT('Checks');