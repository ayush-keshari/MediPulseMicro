-- ============================================================================
-- MediPulseMicro - Data Quality Check for Telemetry Service
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

-- SensorDevice
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'SensorDevice')
BEGIN
    -- SensorDevice row count
    SELECT @TableName = 'SensorDevice', @ExpectedMin = 8;
    SELECT @ActualCount = COUNT(*) FROM [SensorDevice];
    IF @ActualCount >= @ExpectedMin
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('SensorDevice.RowCount', 'TelemetryService', 0, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'OK: SensorDevice row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('SensorDevice.RowCount', 'TelemetryService', 1, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'ERROR: SensorDevice row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END

    -- Check NOT NULL columns: DeviceName, DeviceType, AssignedTo, Status
    SELECT @NullCount = SUM(CASE WHEN [DeviceName] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [DeviceType] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [AssignedTo] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Status] IS NULL THEN 1 ELSE 0 END)
    FROM [SensorDevice];
    IF @NullCount = 0
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('SensorDevice.NotNull_DeviceName_DeviceType_AssignedTo_Status', 'TelemetryService', 0, 'No NULL values in DeviceName, DeviceType, AssignedTo, Status', 'NULL count = 0', GETDATE(), 'OK: SensorDevice mandatory columns (DeviceName, DeviceType, AssignedTo, Status) have no NULL values');
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('SensorDevice.NotNull_DeviceName_DeviceType_AssignedTo_Status', 'TelemetryService', 1, 'No NULL values in DeviceName, DeviceType, AssignedTo, Status', 'NULL count = ' + CAST(@NullCount AS NVARCHAR), GETDATE(), 'ERROR: SensorDevice has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (DeviceName, DeviceType, AssignedTo, Status)');
    END
END
ELSE
BEGIN
    INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
    VALUES ('SensorDevice.TableExists', 'TelemetryService', 0, 'Table SensorDevice exists', 'Table not found', GETDATE(), 'INFO: SensorDevice table not found. Skipping SensorDevice checks.');
END

-- TelemetryRecord
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'TelemetryRecord')
BEGIN
    -- TelemetryRecord row count
    SELECT @TableName = 'TelemetryRecord', @ExpectedMin = 12;
    SELECT @ActualCount = COUNT(*) FROM [TelemetryRecord];
    IF @ActualCount >= @ExpectedMin
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('TelemetryRecord.RowCount', 'TelemetryService', 0, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'OK: TelemetryRecord row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('TelemetryRecord.RowCount', 'TelemetryService', 1, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'ERROR: TelemetryRecord row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END

    -- Check NOT NULL columns: SensorID, Timestamp, Temperature, Humidity, Location
    SELECT @NullCount = SUM(CASE WHEN [SensorID] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Timestamp] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Temperature] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Humidity] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Location] IS NULL THEN 1 ELSE 0 END)
    FROM [TelemetryRecord];
    IF @NullCount = 0
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('TelemetryRecord.NotNull_SensorID_Timestamp_Temperature_Humidity_Location', 'TelemetryService', 0, 'No NULL values in SensorID, Timestamp, Temperature, Humidity, Location', 'NULL count = 0', GETDATE(), 'OK: TelemetryRecord mandatory columns (SensorID, Timestamp, Temperature, Humidity, Location) have no NULL values');
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('TelemetryRecord.NotNull_SensorID_Timestamp_Temperature_Humidity_Location', 'TelemetryService', 1, 'No NULL values in SensorID, Timestamp, Temperature, Humidity, Location', 'NULL count = ' + CAST(@NullCount AS NVARCHAR), GETDATE(), 'ERROR: TelemetryRecord has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (SensorID, Timestamp, Temperature, Humidity, Location)');
    END
END
ELSE
BEGIN
    INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
    VALUES ('TelemetryRecord.TableExists', 'TelemetryService', 0, 'Table TelemetryRecord exists', 'Table not found', GETDATE(), 'INFO: TelemetryRecord table not found. Skipping TelemetryRecord checks.');
END

-- Output results as JSON
SELECT * FROM @Results FOR JSON PATH, ROOT('Checks');