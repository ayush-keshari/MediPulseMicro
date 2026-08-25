-- ============================================================================
-- MediPulseMicro - Data Quality Check for Facility Service
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

-- Facility table
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Facility')
BEGIN
    -- Facility row count
    SELECT @TableName = 'Facility', @ExpectedMin = 6;
    SELECT @ActualCount = COUNT(*) FROM [Facility];
    IF @ActualCount >= @ExpectedMin
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('Facility.RowCount', 'FacilityService', 0, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'OK: Facility row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('Facility.RowCount', 'FacilityService', 1, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'ERROR: Facility row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END

    -- Facility NOT NULL columns: Name, Type, Region
    SELECT @NullCount = SUM(CASE WHEN [Name] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Type] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Region] IS NULL THEN 1 ELSE 0 END)
    FROM [Facility];
    IF @NullCount = 0
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('Facility.NotNull_Name_Type_Region', 'FacilityService', 0, 'No NULL values in Name, Type, Region', 'NULL count = 0', GETDATE(), 'OK: Facility mandatory columns (Name, Type, Region) have no NULL values');
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('Facility.NotNull_Name_Type_Region', 'FacilityService', 1, 'No NULL values in Name, Type, Region', 'NULL count = ' + CAST(@NullCount AS NVARCHAR), GETDATE(), 'ERROR: Facility has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (Name, Type, Region)');
    END
END
ELSE
BEGIN
    INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
    VALUES ('Facility.TableExists', 'FacilityService', 0, 'Table Facility exists', 'Table not found', GETDATE(), 'INFO: Facility table not found. Skipping FacilityService checks.');
END

-- StorageZone table
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'StorageZone')
BEGIN
    -- StorageZone row count
    SELECT @TableName = 'StorageZone', @ExpectedMin = 8;
    SELECT @ActualCount = COUNT(*) FROM [StorageZone];
    IF @ActualCount >= @ExpectedMin
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('StorageZone.RowCount', 'FacilityService', 0, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'OK: StorageZone row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('StorageZone.RowCount', 'FacilityService', 1, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'ERROR: StorageZone row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END

    -- StorageZone NOT NULL columns: FacilityID, Name, TemperatureProfile, Capacity
    SELECT @NullCount = SUM(CASE WHEN [FacilityID] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Name] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [TemperatureProfile] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Capacity] IS NULL THEN 1 ELSE 0 END)
    FROM [StorageZone];
    IF @NullCount = 0
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('StorageZone.NotNull_FacilityID_Name_TemperatureProfile_Capacity', 'FacilityService', 0, 'No NULL values in FacilityID, Name, TemperatureProfile, Capacity', 'NULL count = 0', GETDATE(), 'OK: StorageZone mandatory columns (FacilityID, Name, TemperatureProfile, Capacity) have no NULL values');
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('StorageZone.NotNull_FacilityID_Name_TemperatureProfile_Capacity', 'FacilityService', 1, 'No NULL values in FacilityID, Name, TemperatureProfile, Capacity', 'NULL count = ' + CAST(@NullCount AS NVARCHAR), GETDATE(), 'ERROR: StorageZone has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (FacilityID, Name, TemperatureProfile, Capacity)');
    END
END
ELSE
BEGIN
    INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
    VALUES ('StorageZone.TableExists', 'FacilityService', 0, 'Table StorageZone exists', 'Table not found', GETDATE(), 'INFO: StorageZone table not found. Skipping StorageZone checks.');
END

-- Output results as JSON
SELECT * FROM @Results FOR JSON PATH, ROOT('Checks');