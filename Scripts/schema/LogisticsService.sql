-- ============================================================================
-- LogisticsService Schema
-- Tables: TransferOrder, TransferOrderItem, ConsumptionRecord
-- ============================================================================

-- TransferOrder: Inter-facility stock transfers
CREATE TABLE [TransferOrder] (
    [TransferOrderId] INT PRIMARY KEY IDENTITY(1,1),
    [FromFacilityId] INT NOT NULL,
    [FromFacilityName] NVARCHAR(100) NOT NULL,
    [ToFacilityId] INT NOT NULL,
    [ToFacilityName] NVARCHAR(100) NOT NULL,
    [RequestedBy] NVARCHAR(100) NOT NULL,
    [RequestedDate] DATETIME2 NOT NULL,
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Draft',
    CONSTRAINT FK_TransferOrder_FromFacility FOREIGN KEY ([FromFacilityId]) REFERENCES [Facility]([FacilityID]),
    CONSTRAINT FK_TransferOrder_ToFacility FOREIGN KEY ([ToFacilityId]) REFERENCES [Facility]([FacilityID])
);

-- TransferOrderItem: Line items in a transfer order
CREATE TABLE [TransferOrderItem] (
    [TransferOrderItemId] INT PRIMARY KEY IDENTITY(1,1),
    [TransferOrderId] INT NOT NULL,
    [ItemId] INT NOT NULL,
    [ItemName] NVARCHAR(150) NOT NULL,
    [Quantity] INT NOT NULL,
    [ToStorageZoneId] INT NOT NULL,
    CONSTRAINT FK_TransferOrderItem_TransferOrder FOREIGN KEY ([TransferOrderId]) REFERENCES [TransferOrder]([TransferOrderId]) ON DELETE CASCADE,
    CONSTRAINT FK_TransferOrderItem_StorageZone FOREIGN KEY ([ToStorageZoneId]) REFERENCES [StorageZone]([ZoneID])
);

-- ConsumptionRecord: Track stock consumption at facilities
CREATE TABLE [ConsumptionRecord] (
    [ConsumptionId] INT PRIMARY KEY IDENTITY(1,1),
    [FacilityId] INT NOT NULL,
    [WardId] INT,
    [ItemId] INT NOT NULL,
    [ItemName] NVARCHAR(150) NOT NULL,
    [QuantityConsumed] INT NOT NULL,
    [ConsumedDate] DATETIME2 NOT NULL,
    [ConsumedBy] NVARCHAR(100) NOT NULL,
    CONSTRAINT FK_ConsumptionRecord_Facility FOREIGN KEY ([FacilityId]) REFERENCES [Facility]([FacilityID])
);