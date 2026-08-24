-- ============================================================================
-- MediPulseMicro - Data Quality Check for Notification Service
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

-- Notification
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Notification')
BEGIN
    -- Notification row count
    SELECT @TableName = 'Notification', @ExpectedMin = 6;
    SELECT @ActualCount = COUNT(*) FROM [Notification];
    IF @ActualCount >= @ExpectedMin
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('Notification.RowCount', 'NotificationService', 0, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'OK: Notification row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('Notification.RowCount', 'NotificationService', 1, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'ERROR: Notification row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END

    -- Check NOT NULL columns: UserId, Category, Title, Message, IsRead, CreatedAt
    SELECT @NullCount = SUM(CASE WHEN [UserId] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Category] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Title] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Message] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [IsRead] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [CreatedAt] IS NULL THEN 1 ELSE 0 END)
    FROM [Notification];
    IF @NullCount = 0
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('Notification.NotNull_UserId_Category_Title_Message_IsRead_CreatedAt', 'NotificationService', 0, 'No NULL values in UserId, Category, Title, Message, IsRead, CreatedAt', 'NULL count = 0', GETDATE(), 'OK: Notification mandatory columns (UserId, Category, Title, Message, IsRead, CreatedAt) have no NULL values');
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('Notification.NotNull_UserId_Category_Title_Message_IsRead_CreatedAt', 'NotificationService', 1, 'No NULL values in UserId, Category, Title, Message, IsRead, CreatedAt', 'NULL count = ' + CAST(@NullCount AS NVARCHAR), GETDATE(), 'ERROR: Notification has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (UserId, Category, Title, Message, IsRead, CreatedAt)');
    END
END
ELSE
BEGIN
    INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
    VALUES ('Notification.TableExists', 'NotificationService', 0, 'Table Notification exists', 'Table not found', GETDATE(), 'INFO: Notification table not found. Skipping Notification checks.');
END

-- Output results as JSON
SELECT * FROM @Results FOR JSON PATH, ROOT('Checks');