-- ============================================================================
-- NotificationService Schema
-- Table: Notification
-- ============================================================================

-- Notification: User notifications for events
CREATE TABLE [Notification] (
    [NotificationId] INT PRIMARY KEY IDENTITY(1,1),
    [UserId] NVARCHAR(100) NOT NULL,
    [Category] NVARCHAR(50) NOT NULL,
    [Title] NVARCHAR(200) NOT NULL,
    [Message] NVARCHAR(1000) NOT NULL,
    [IsRead] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL
);

CREATE INDEX IX_Notification_UserIdIsRead ON [Notification]([UserId], [IsRead]);
CREATE INDEX IX_Notification_CreatedAt ON [Notification]([CreatedAt]);