-- ============================================================================
-- AuditService Schema
-- Table: AuditLog (for MedipulseAudit database)
-- ============================================================================

-- AuditLog: Complete audit trail of system actions
CREATE TABLE [AuditLog] (
    [AuditLogId] INT PRIMARY KEY IDENTITY(1,1),
    [UserId] NVARCHAR(100) NOT NULL,
    [UserName] NVARCHAR(150),
    [UserRole] NVARCHAR(100),
    [HttpMethod] NVARCHAR(10) NOT NULL,
    [Endpoint] NVARCHAR(500) NOT NULL,
    [EntityType] NVARCHAR(100),
    [EntityId] NVARCHAR(100),
    [ServiceName] NVARCHAR(100),
    [StatusCode] INT,
    [Timestamp] DATETIME2 NOT NULL,
    [Details] NVARCHAR(2000)
);

CREATE INDEX IX_AuditLog_Timestamp ON [AuditLog]([Timestamp]);
CREATE INDEX IX_AuditLog_UserId ON [AuditLog]([UserId]);
CREATE INDEX IX_AuditLog_UserRole ON [AuditLog]([UserRole]);
CREATE INDEX IX_AuditLog_EntityType ON [AuditLog]([EntityType]);
CREATE INDEX IX_AuditLog_ServiceName ON [AuditLog]([ServiceName]);