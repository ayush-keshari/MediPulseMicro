-- ============================================================================
-- NotificationService Mock Data
-- Table: Notification
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM [Notification] WHERE [NotificationId] = 1)
BEGIN
    INSERT INTO [Notification] ([UserId], [Category], [Title], [Message], [IsRead], [CreatedAt]) VALUES
    ('user-manager-01', 'Exception', 'Stockout Alert', 'Insulin stock at Apollo Hospital has reached critical level', 0, DATEADD(HOUR, -24, GETUTCDATE())),
    ('user-manager-01', 'Expiry', 'Expiry Warning', 'Vaccine batch LOT-VAC-2026-001 expiring in 3 days', 0, DATEADD(HOUR, -18, GETUTCDATE())),
    ('user-warehouse-01', 'Receipt', 'Receipt Confirmed', 'PO#1 received: 200 units of Insulin from Cipla Limited', 1, DATEADD(HOUR, -12, GETUTCDATE())),
    ('user-nurse-02', 'Replenishment', 'Replenishment Suggested', 'Order suggested for Amoxicillin at Apollo Hospital', 0, DATEADD(HOUR, -6, GETUTCDATE())),
    ('user-manager-02', 'Exception', 'Temperature Excursion', 'Temperature breach detected in Zone A2 at Apollo Hospital', 0, DATEADD(HOUR, -2, GETUTCDATE())),
    ('user-compliance-01', 'Exception', 'Recall Action Pending', 'Recall action REC-001 is overdue', 0, GETUTCDATE());
END