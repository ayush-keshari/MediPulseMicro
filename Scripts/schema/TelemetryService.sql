-- ============================================================================
-- TelemetryService Schema
-- Tables: SensorDevice, TelemetryRecord
-- ============================================================================

-- SensorDevice: IoT sensors for monitoring cold chain conditions
CREATE TABLE [SensorDevice] (
    [SensorID] INT PRIMARY KEY IDENTITY(1,1),
    [DeviceName] NVARCHAR(100) NOT NULL,
    [DeviceType] NVARCHAR(50) NOT NULL,
    [AssignedTo] NVARCHAR(50) NOT NULL,
    [AssignedEntityId] INT,
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Active'
);

-- TelemetryRecord: Temperature and humidity readings from sensors
CREATE TABLE [TelemetryRecord] (
    [TelemetryID] INT PRIMARY KEY IDENTITY(1,1),
    [SensorID] INT NOT NULL,
    [Timestamp] DATETIME2 NOT NULL,
    [Temperature] DECIMAL(5,2),
    [Humidity] DECIMAL(5,2),
    [Location] NVARCHAR(200),
    [IsExcursion] BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_TelemetryRecord_SensorDevice FOREIGN KEY ([SensorID]) REFERENCES [SensorDevice]([SensorID]) ON DELETE CASCADE
);