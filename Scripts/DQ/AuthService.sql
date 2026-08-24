-- ============================================================================
-- MediPulseMicro - Data Quality Check for Auth Service
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

-- User
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'User')
BEGIN
    -- User row count
    SELECT @TableName = 'User', @ExpectedMin = 7;
    SELECT @ActualCount = COUNT(*) FROM [User];
    IF @ActualCount >= @ExpectedMin
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('User.RowCount', 'AuthService', 0, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'OK: User row count ' + CAST(@ActualCount AS NVARCHAR) + ' meets minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('User.RowCount', 'AuthService', 1, 'Row count >= ' + CAST(@ExpectedMin AS NVARCHAR), 'Row count = ' + CAST(@ActualCount AS NVARCHAR), GETDATE(), 'ERROR: User row count ' + CAST(@ActualCount AS NVARCHAR) + ' is less than expected minimum ' + CAST(@ExpectedMin AS NVARCHAR));
    END

    -- Check NOT NULL columns: Name, Role, Email, Password (Phone can be null)
    SELECT @NullCount = SUM(CASE WHEN [Name] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Role] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Email] IS NULL THEN 1 ELSE 0 END)
                        + SUM(CASE WHEN [Password] IS NULL THEN 1 ELSE 0 END)
    FROM [User];
    IF @NullCount = 0
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('User.NotNull_Name_Role_Email_Password', 'AuthService', 0, 'No NULL values in Name, Role, Email, Password', 'NULL count = 0', GETDATE(), 'OK: User mandatory columns (Name, Role, Email, Password) have no NULL values');
    END
    ELSE
    BEGIN
        INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
        VALUES ('User.NotNull_Name_Role_Email_Password', 'AuthService', 1, 'No NULL values in Name, Role, Email, Password', 'NULL count = ' + CAST(@NullCount AS NVARCHAR), GETDATE(), 'ERROR: User has ' + CAST(@NullCount AS NVARCHAR) + ' NULL values in mandatory columns (Name, Role, Email, Password)');
    END
END
ELSE
BEGIN
    INSERT INTO @Results (CheckName, Domain, Status, ExpectedCondition, ActualResult, Timestamp, Message)
    VALUES ('User.TableExists', 'AuthService', 0, 'Table User exists', 'Table not found', GETDATE(), 'INFO: User table not found. Skipping User checks.');
END

-- Output results as JSON
SELECT * FROM @Results FOR JSON PATH, ROOT('Checks');