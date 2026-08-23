-- ============================================================================
-- InventoryService Schema
-- Tables: Item, InventoryPositions, ExceptionEvent, RecallAction, Forecast, ReplenishmentPlan
-- ============================================================================

-- Item: Medical inventory items (medicines, devices, consumables)
CREATE TABLE [Item] (
    [ItemId] INT PRIMARY KEY IDENTITY(1,1),
    [ItemCode] NVARCHAR(50) NOT NULL UNIQUE,
    [Name] NVARCHAR(150) NOT NULL,
    [Category] NVARCHAR(50) NOT NULL,
    [Unit] NVARCHAR(20) NOT NULL,
    [StorageRequirement] NVARCHAR(50),
    [SafetyStock] INT NOT NULL
);

-- InventoryPositions: Tracks lot-level inventory at facilities
CREATE TABLE [InventoryPositions] (
    [PositionId] INT PRIMARY KEY IDENTITY(1,1),
    [ItemId] INT NOT NULL,
    [LotId] NVARCHAR(50) NOT NULL,
    [ExpiryDate] DATETIME2 NOT NULL,
    [Quantity] INT NOT NULL,
    [FacilityId] INT NOT NULL,
    [StorageZoneId] INT NOT NULL,
    [SafetyStock] INT NOT NULL,
    CONSTRAINT FK_InventoryPositions_Item FOREIGN KEY ([ItemId]) REFERENCES [Item]([ItemId]) ON DELETE CASCADE,
    CONSTRAINT FK_InventoryPositions_Facility FOREIGN KEY ([FacilityId]) REFERENCES [Facility]([FacilityID]),
    CONSTRAINT FK_InventoryPositions_StorageZone FOREIGN KEY ([StorageZoneId]) REFERENCES [StorageZone]([ZoneID])
);

-- ExceptionEvent: Detected supply chain issues (stockouts, expiry, temperature breaches)
CREATE TABLE [ExceptionEvent] (
    [ExceptionId] INT PRIMARY KEY IDENTITY(1,1),
    [Type] NVARCHAR(50) NOT NULL,
    [ReferenceType] NVARCHAR(50) NOT NULL,
    [ReferenceId] INT NOT NULL,
    [ItemId] INT,
    [ItemName] NVARCHAR(150),
    [FacilityId] INT,
    [LotId] NVARCHAR(50),
    [Severity] NVARCHAR(20) NOT NULL DEFAULT 'Medium',
    [Status] NVARCHAR(20) NOT NULL DEFAULT 'Open',
    [DetectedDate] DATETIME2 NOT NULL,
    CONSTRAINT FK_ExceptionEvent_Item FOREIGN KEY ([ItemId]) REFERENCES [Item]([ItemId])
);

CREATE INDEX IX_ExceptionEvent_Type_Status ON [ExceptionEvent]([Type], [Status]);

-- RecallAction: Corrective actions assigned for exceptions
CREATE TABLE [RecallAction] (
    [RecallActionId] INT PRIMARY KEY IDENTITY(1,1),
    [ExceptionId] INT NOT NULL,
    [OwnerId] NVARCHAR(100) NOT NULL,
    [ActionDescription] NVARCHAR(500) NOT NULL,
    [DueDate] DATETIME2 NOT NULL,
    [Status] NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    CONSTRAINT FK_RecallAction_Exception FOREIGN KEY ([ExceptionId]) REFERENCES [ExceptionEvent]([ExceptionId]) ON DELETE CASCADE
);

-- Forecast: Demand forecasts for items
CREATE TABLE [Forecast] (
    [ForecastId] INT PRIMARY KEY IDENTITY(1,1),
    [ItemId] INT NOT NULL,
    [FacilityId] INT NOT NULL,
    [Period] NVARCHAR(10) NOT NULL,
    [ForecastQuantity] INT NOT NULL,
    [GeneratedDate] DATETIME2 NOT NULL,
    CONSTRAINT FK_Forecast_Item FOREIGN KEY ([ItemId]) REFERENCES [Item]([ItemId]),
    CONSTRAINT FK_Forecast_Facility FOREIGN KEY ([FacilityId]) REFERENCES [Facility]([FacilityID])
);

CREATE INDEX IX_Forecast_Facility_Item_Period ON [Forecast]([FacilityId], [ItemId], [Period]);

-- ReplenishmentPlan: Purchase suggestions
CREATE TABLE [ReplenishmentPlan] (
    [PlanId] INT PRIMARY KEY IDENTITY(1,1),
    [ItemId] INT NOT NULL,
    [FacilityId] INT NOT NULL,
    [SuggestedOrderQty] INT NOT NULL,
    [Priority] NVARCHAR(20) NOT NULL DEFAULT 'Medium',
    [Status] NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    [GeneratedDate] DATETIME2 NOT NULL,
    CONSTRAINT FK_ReplenishmentPlan_Item FOREIGN KEY ([ItemId]) REFERENCES [Item]([ItemId]),
    CONSTRAINT FK_ReplenishmentPlan_Facility FOREIGN KEY ([FacilityId]) REFERENCES [Facility]([FacilityID])
);

CREATE INDEX IX_ReplenishmentPlan_Facility_Status ON [ReplenishmentPlan]([FacilityId], [Status]);