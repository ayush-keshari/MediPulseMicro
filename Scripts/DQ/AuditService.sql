-- ============================================================================
-- MediPulseMicro - Data Quality Check for Audit Service
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

-- AuditLog (in MedipulseAudit database, but we check in current database)
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLog')
BEGIN
    -- AuditLog row count
    SELECT @TableName = 'AuditLog', @ExpectedMin = 7;
    SELECT @ActualCount = COUNT(*) FROM [AuditLog];
    IF @ActualCount >= @ExpectedMin
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('AuditLog.RowCount', 'AuditService', 0, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'OK: AuditLog row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('AuditLog.RowCount', 'AuditService', 1, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'ERROR: AuditLog row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END

    -- Check NOT NULL columns: UserId, HttpMethod, Endpoint, Timestamp, Details
    SELECT @NullCount = SUM(CASE WHEN [UserId] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [HttpMethod] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Endpoint] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Timestamp] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Details] IS NULL THEN 1 ELSE 0 END)
    FROM [AuditLog];
    IF @NullCount = 0
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('AuditLog.NotNull_UserId_HttpMethod_Endpoint_Timestamp_Details', 'AuditService', 0, 'No NULL values in UserId, HttpMethod, Endpoint, Timestamp, Details', 'NULL count = 0', GETDATE(), 'OK: AuditLog mandatory columns (UserId, HttpMethod, Endpoint, Timestamp, Details) have no NULL values');
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('AuditLog.NotNull_UserId_HttpMethod_Endpoint_Timestamp_Details', 'AuditService', 1, 'No NULL values in UserId, HttpMethod, Endpoint, Timestamp, Details', 'NULL count = ' + CAST(@NullCount AS NVARCHAR), GETDATE(), 'ERROR: AuditLog has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (UserId, HttpMethod, Endpoint, Timestamp, Details)');
    END
END
ELSE
BEGIN
    INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
    VALUES ('AuditLog.TableExists', 'AuditService', 0, 'Table AuditLog exists', 'Table not found', GETDATE(), 'INFO: AuditLog table not found in current database. Skipping AuditService checks.');
END

-- Output results as JSON
SELECT * FROM @Results FOR JSON PATH, ROOT('Checks');