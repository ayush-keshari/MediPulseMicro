-- ============================================================================
-- MediPulseMicro - Data Quality Check for Inventory Service
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

-- Items table
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Items')
BEGIN
    -- Items
    SELECT @TableName = 'Items', @ExpectedMin = 8;
    SELECT @ActualCount = COUNT(*) FROM [Items];
    IF @ActualCount >= @ExpectedMin
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('Item.RowCount', 'InventoryService', 0, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'OK: Item row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('Item.RowCount', 'InventoryService', 1, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'ERROR: Item row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END

    -- Check NOT NULL columns: ItemCode, Name, Category, Unit, SafetyStock
    SELECT @NullCount = SUM(CASE WHEN [ItemCode] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Name] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Category] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Unit] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [SafetyStock] IS NULL THEN 1 ELSE 0 END)
    FROM [Items];
    IF @NullCount = 0
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('Item.NotNull_ItemCode_Name_Category_Unit_SafetyStock', 'InventoryService', 0, 'No NULL values in ItemCode, Name, Category, Unit, SafetyStock', 'NULL count = 0', GETDATE(), 'OK: Item mandatory columns (ItemCode, Name, Category, Unit, SafetyStock) have no NULL values');
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('Item.NotNull_ItemCode_Name_Category_Unit_SafetyStock', 'InventoryService', 1, 'No NULL values in ItemCode, Name, Category, Unit, SafetyStock', 'NULL count = ' + CAST(@NullCount AS NVARCHAR), GETDATE(), 'ERROR: Item has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (ItemCode, Name, Category, Unit, SafetyStock)');
    END
END
ELSE
BEGIN
    INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('Items.TableExists', 'InventoryService', 0, 'Table Items exists', 'Table not found', GETDATE(), 'INFO: Items table not found. Skipping Items checks.');
END

-- InventoryPositions table
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'InventoryPositions')
BEGIN
    -- InventoryPositions
    SELECT @TableName = 'InventoryPositions', @ExpectedMin = 8;
    SELECT @ActualCount = COUNT(*) FROM [InventoryPositions];
    IF @ActualCount >= @ExpectedMin
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('InventoryPositions.RowCount', 'InventoryService', 0, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'OK: InventoryPositions row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('InventoryPositions.RowCount', 'InventoryService', 1, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'ERROR: InventoryPositions row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR));
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
    IF @NullCount = 0
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('InventoryPositions.NotNull_ItemId_LotId_ExpiryDate_Quantity_FacilityId_StorageZoneId_SafetyStock', 'InventoryService', 0, 'No NULL values in ItemId, LotId, ExpiryDate, Quantity, FacilityId, StorageZoneId, SafetyStock', 'NULL count = 0', GETDATE(), 'OK: InventoryPositions mandatory columns (ItemId, LotId, ExpiryDate, Quantity, FacilityId, StorageZoneId, SafetyStock) have no NULL values');
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('InventoryPositions.NotNull_ItemId_LotId_ExpiryDate_Quantity_FacilityId_StorageZoneId_SafetyStock', 'InventoryService', 1, 'No NULL values in ItemId, LotId, ExpiryDate, Quantity, FacilityId, StorageZoneId, SafetyStock', 'NULL count = ' + CAST(@NullCount AS NVARCHAR), GETDATE(), 'ERROR: InventoryPositions has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (ItemId, LotId, ExpiryDate, Quantity, FacilityId, StorageZoneId, SafetyStock)');
    END
END
ELSE
BEGIN
    INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
    VALUES ('InventoryPositions.TableExists', 'InventoryService', 0, 'Table InventoryPositions exists', 'Table not found', GETDATE(), 'INFO: InventoryPositions table not found. Skipping InventoryPositions checks.');
END

-- ExceptionEvent table
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ExceptionEvent')
BEGIN
    -- ExceptionEvent
    SELECT @TableName = 'ExceptionEvent', @ExpectedMin = 4;
    SELECT @ActualCount = COUNT(*) FROM [ExceptionEvent];
    IF @ActualCount >= @ExpectedMin
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('ExceptionEvent.RowCount', 'InventoryService', 0, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'OK: ExceptionEvent row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('ExceptionEvent.RowCount', 'InventoryService', 1, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'ERROR: ExceptionEvent row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END

    -- Check NOT NULL columns: Type, ReferenceType, ReferenceId, Severity, Status, DetectedDate
    SELECT @NullCount = SUM(CASE WHEN [Type] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [ReferenceType] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [ReferenceId] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Severity] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Status] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [DetectedDate] IS NULL THEN 1 ELSE 0 END)
    FROM [ExceptionEvent];
    IF @NullCount = 0
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('ExceptionEvent.NotNull_Type_ReferenceType_ReferenceId_Severity_Status_DetectedDate', 'InventoryService', 0, 'No NULL values in Type, ReferenceType, ReferenceId, Severity, Status, DetectedDate', 'NULL count = 0', GETDATE(), 'OK: ExceptionEvent mandatory columns (Type, ReferenceType, ReferenceId, Severity, Status, DetectedDate) have no NULL values');
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('ExceptionEvent.NotNull_Type_ReferenceType_ReferenceId_Severity_Status_DetectedDate', 'InventoryService', 1, 'No NULL values in Type, ReferenceType, ReferenceId, Severity, Status, DetectedDate', 'NULL count = ' + CAST(@NullCount AS NVARCHAR), GETDATE(), 'ERROR: ExceptionEvent has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (Type, ReferenceType, ReferenceId, Severity, Status, DetectedDate)');
    END
END
ELSE
BEGIN
    INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
    VALUES ('ExceptionEvent.TableExists', 'InventoryService', 0, 'Table ExceptionEvent exists', 'Table not found', GETDATE(), 'INFO: ExceptionEvent table not found. Skipping ExceptionEvent checks.');
END

-- RecallAction table
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'RecallAction')
BEGIN
    -- RecallAction
    SELECT @TableName = 'RecallAction', @ExpectedMin = 4;
    SELECT @ActualCount = COUNT(*) FROM [RecallAction];
    IF @ActualCount >= @ExpectedMin
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('RecallAction.RowCount', 'InventoryService', 0, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'OK: RecallAction row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('RecallAction.RowCount', 'InventoryService', 1, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'ERROR: RecallAction row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END

    -- Check NOT NULL columns: ExceptionId, OwnerId, ActionDescription, DueDate, Status
    SELECT @NullCount = SUM(CASE WHEN [ExceptionId] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [OwnerId] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [ActionDescription] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [DueDate] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Status] IS NULL THEN 1 ELSE 0 END)
    FROM [RecallAction];
    IF @NullCount = 0
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('RecallAction.NotNull_ExceptionId_OwnerId_ActionDescription_DueDate_Status', 'InventoryService', 0, 'No NULL values in ExceptionId, OwnerId, ActionDescription, DueDate, Status', 'NULL count = 0', GETDATE(), 'OK: RecallAction mandatory columns (ExceptionId, OwnerId, ActionDescription, DueDate, Status) have no NULL values');
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('RecallAction.NotNull_ExceptionId_OwnerId_ActionDescription_DueDate_Status', 'InventoryService', 1, 'No NULL values in ExceptionId, OwnerId, ActionDescription, DueDate, Status', 'NULL count = ' + CAST(@NullCount AS NVARCHAR), GETDATE(), 'ERROR: RecallAction has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (ExceptionId, OwnerId, ActionDescription, DueDate, Status)');
    END
END
ELSE
BEGIN
    INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
    VALUES ('RecallAction.TableExists', 'InventoryService', 0, 'Table RecallAction exists', 'Table not found', GETDATE(), 'INFO: RecallAction table not found. Skipping RecallAction checks.');
END

-- Forecast table
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Forecast')
BEGIN
    -- Forecast
    SELECT @TableName = 'Forecast', @ExpectedMin = 6;
    SELECT @ActualCount = COUNT(*) FROM [Forecast];
    IF @ActualCount >= @ExpectedMin
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('Forecast.RowCount', 'InventoryService', 0, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'OK: Forecast row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('Forecast.RowCount', 'InventoryService', 1, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'ERROR: Forecast row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END

    -- Check NOT NULL columns: ItemId, FacilityId, Period, ForecastQuantity, GeneratedDate
    SELECT @NullCount = SUM(CASE WHEN [ItemId] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [FacilityId] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Period] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [ForecastQuantity] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [GeneratedDate] IS NULL THEN 1 ELSE 0 END)
    FROM [Forecast];
    IF @NullCount = 0
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('Forecast.NotNull_ItemId_FacilityId_Period_ForeachQuantity_GeneratedDate', 'InventoryService', 0, 'No NULL values in ItemId, FacilityId, Period, ForeachQuantity, GeneratedDate', 'NULL count = 0', GETDATE(), 'OK: Forecast mandatory columns (ItemId, FacilityId, Period, ForeachQuantity, GeneratedDate) have no NULL values');
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('Forecast.NotNull_ItemId_FacilityId_Period_ForeachQuantity_GeneratedDate', 'InventoryService', 1, 'No NULL values in ItemId, FacilityId, Period, ForeachQuantity, GeneratedDate', 'NULL count = ' + CAST(@NullCount AS NVARCHAR), GETDATE(), 'ERROR: Forecast has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (ItemId, FacilityId, Period, ForeachQuantity, GeneratedDate)');
    END
END
ELSE
BEGIN
    INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
    VALUES ('Forecast.TableExists', 'InventoryService', 0, 'Table Forecast exists', 'Table not found', GETDATE(), 'INFO: Forecast table not found. Skipping Forecast checks.');
END

-- ReplenishmentPlan table
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ReplenishmentPlan')
BEGIN
    -- ReplenishmentPlan
    SELECT @TableName = 'ReplenishmentPlan', @ExpectedMin = 4;
    SELECT @ActualCount = COUNT(*) FROM [ReplenishmentPlan];
    IF @ActualCount >= @ExpectedMin
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('ReplenishmentPlan.RowCount', 'InventoryService', 0, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'OK: ReplenishmentPlan row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('ReplenishmentPlan.RowCount', 'InventoryService', 1, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'ERROR: ReplenishmentPlan row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END

    -- Check NOT NULL columns: ItemId, FacilityId, SuggestedOrderQty, Priority, Status, GeneratedDate
    SELECT @NullCount = SUM(CASE WHEN [ItemId] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [FacilityId] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [SuggestedOrderQty] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Priority] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Status] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [GeneratedDate] IS NULL THEN 1 ELSE 0 END)
    FROM [ReplenishmentPlan];
    IF @NullCount = 0
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('ReplenishmentPlan.NotNull_ItemId_FacilityId_SuggestedOrderQty_Priority_Status_GeneratedDate', 'InventoryService', 0, 'No NULL values in ItemId, FacilityId, SuggestedOrderQty, Priority, Status, GeneratedDate', 'NULL count = 0', GETDATE(), 'OK: ReplenishmentPlan mandatory columns (ItemId, FacilityId, SuggestedOrderQty, Priority, Status, GeneratedDate) have no NULL values');
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('ReplenishmentPlan.NotNull_ItemId_FacilityId_SuggestedOrderQty_Priority_Status_GeneratedDate', 'InventoryService', 1, 'No NULL values in ItemId, FacilityId, SuggestedOrderQty, Priority, Status, GeneratedDate', 'NULL count = ' + CAST(@NullCount AS NVARCHAR), GETDATE(), 'ERROR: ReplenishmentPlan has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (ItemId, FacilityId, SuggestedOrderQty, Priority, Status, GeneratedDate)');
    END
END
ELSE
BEGIN
    INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
    VALUES ('ReplenishmentPlan.TableExists', 'InventoryService', 0, 'Table ReplenishmentPlan exists', 'Table not found', GETDATE(), 'INFO: ReplenishmentPlan table not found. Skipping ReplenishmentPlan checks.');
END

-- Output results as JSON
SELECT * FROM @Results FOR JSON PATH, ROOT('Checks');
