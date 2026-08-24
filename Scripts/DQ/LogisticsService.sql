-- ============================================================================
-- MediPulseMicro - Data Quality Check for Logistics Service
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

-- TransferOrder
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'TransferOrder')
BEGIN
    -- TransferOrder row count
    SELECT @TableName = 'TransferOrder', @ExpectedMin = 4;
    SELECT @ActualCount = COUNT(*) FROM [TransferOrder];
    IF @ActualCount >= @ExpectedMin
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('TransferOrder.RowCount', 'LogisticsService', 0, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'OK: TransferOrder row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('TransferOrder.RowCount', 'LogisticsService', 1, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'ERROR: TransferOrder row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR));
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
    IF @NullCount = 0
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('TransferOrder.NotNull_FromFacilityId_FromFacilityName_ToFacilityId_ToFacilityName_RequestedBy_RequestedDate_Status', 'LogisticsService', 0, 'No NULL values in FromFacilityId, FromFacilityName, ToFacilityId, ToFacilityName, RequestedBy, RequestedDate, Status', 'NULL count = 0', GETDATE(), 'OK: TransferOrder mandatory columns (FromFacilityId, FromFacilityName, ToFacilityId, ToFacilityName, RequestedBy, RequestedDate, Status) have no NULL values');
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('TransferOrder.NotNull_FromFacilityId_FromFacilityName_ToFacilityId_ToFacilityName_RequestedBy_RequestedDate_Status', 'LogisticsService', 1, 'No NULL values in FromFacilityId, FromFacilityName, ToFacilityId, ToFacilityName, RequestedBy, RequestedDate, Status', 'NULL count = ' + CAST(@NullCount AS NVARCHAR), GETDATE(), 'ERROR: TransferOrder has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (FromFacilityId, FromFacilityName, ToFacilityId, ToFacilityName, RequestedBy, RequestedDate, Status)');
    END
END
ELSE
BEGIN
    INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
    VALUES ('TransferOrder.TableExists', 'LogisticsService', 0, 'Table TransferOrder exists', 'Table not found', GETDATE(), 'INFO: TransferOrder table not found. Skipping TransferOrder checks.');
END

-- TransferOrderItem
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'TransferOrderItem')
BEGIN
    -- TransferOrderItem row count
    SELECT @TableName = 'TransferOrderItem', @ExpectedMin = 6;
    SELECT @ActualCount = COUNT(*) FROM [TransferOrderItem];
    IF @ActualCount >= @ExpectedMin
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('TransferOrderItem.RowCount', 'LogisticsService', 0, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'OK: TransferOrderItem row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('TransferOrderItem.RowCount', 'LogisticsService', 1, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'ERROR: TransferOrderItem row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END

    -- Check NOT NULL columns: TransferOrderId, ItemId, ItemName, Quantity, ToStorageZoneId
    SELECT @NullCount = SUM(CASE WHEN [TransferOrderId] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [ItemId] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [ItemName] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Quantity] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [ToStorageZoneId] IS NULL THEN 1 ELSE 0 END)
    FROM [TransferOrderItem];
    IF @NullCount = 0
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('TransferOrderItem.NotNull_TransferOrderId_ItemId_ItemName_Quantity_ToStorageZoneId', 'LogisticsService', 0, 'No NULL values in TransferOrderId, ItemId, ItemName, Quantity, ToStorageZoneId', 'NULL count = 0', GETDATE(), 'OK: TransferOrderItem mandatory columns (TransferOrderId, ItemId, ItemName, Quantity, ToStorageZoneId) have no NULL values');
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('TransferOrderItem.NotNull_TransferOrderId_ItemId_ItemName_Quantity_ToStorageZoneId', 'LogisticsService', 1, 'No NULL values in TransferOrderId, ItemId, ItemName, Quantity, ToStorageZoneId', 'NULL count = ' + CAST(@NullCount AS NVARCHAR), GETDATE(), 'ERROR: TransferOrderItem has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (TransferOrderId, ItemId, ItemName, Quantity, ToStorageZoneId)');
    END
END
ELSE
BEGIN
    INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
    VALUES ('TransferOrderItem.TableExists', 'LogisticsService', 0, 'Table TransferOrderItem exists', 'Table not found', GETDATE(), 'INFO: TransferOrderItem table not found. Skipping TransferOrderItem checks.');
END

-- ConsumptionRecord
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ConsumptionRecord')
BEGIN
    -- ConsumptionRecord row count
    SELECT @TableName = 'ConsumptionRecord', @ExpectedMin = 6;
    SELECT @ActualCount = COUNT(*) FROM [ConsumptionRecord];
    IF @ActualCount >= @ExpectedMin
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('ConsumptionRecord.RowCount', 'LogisticsService', 0, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'OK: ConsumptionRecord row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('ConsumptionRecord.RowCount', 'LogisticsService', 1, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'ERROR: ConsumptionRecord row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR));
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
    IF @NullCount = 0
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('ConsumptionRecord.NotNull_FacilityId_WardId_ItemId_ItemName_QuantityConsumed_ConsumedDate_ConsumedBy', 'LogisticsService', 0, 'No NULL values in FacilityId, WardId, ItemId, ItemName, QuantityConsumed, ConsumedDate, ConsumedBy', 'NULL count = 0', GETDATE(), 'OK: ConsumptionRecord mandatory columns (FacilityId, WardId, ItemId, ItemName, QuantityConsumed, ConsumedDate, ConsumedBy) have no NULL values');
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('ConsumptionRecord.NotNull_FacilityId_WardId_ItemId_ItemName_QuantityConsumed_ConsumedDate_ConsumedBy', 'LogisticsService', 1, 'No NULL values in FacilityId, WardId, ItemId, ItemName, QuantityConsumed, ConsumedDate, ConsumedBy', 'NULL count = ' + CAST(@NullCount AS NVARCHAR), GETDATE(), 'ERROR: ConsumptionRecord has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (FacilityId, WardId, ItemId, ItemName, QuantityConsumed, ConsumedDate, ConsumedBy)');
    END
END
ELSE
BEGIN
    INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
    VALUES ('ConsumptionRecord.TableExists', 'LogisticsService', 0, 'Table ConsumptionRecord exists', 'Table not found', GETDATE(), 'INFO: ConsumptionRecord table not found. Skipping ConsumptionRecord checks.');
END

-- Output results as JSON
SELECT * FROM @Results FOR JSON PATH, ROOT('Checks');