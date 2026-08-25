-- ============================================================================
-- MediPulseMicro - Telemetry Service Mock Data
-- ============================================================================

-- Insert SensorDevice Mock Data
INSERT INTO [SensorDevice] ([DeviceName], [DeviceType], [AssignedTo], [AssignedEntityId], [Status]) VALUES
('Zone A1 Temp Sensor', 'Temp', 'Zone', 1, 'Active'),
('Zone A1 Humidity Sensor', 'Humidity', 'Zone', 1, 'Active'),
('Zone A2 Freezer Monitor', 'Temp', 'Zone', 2, 'Active'),
('Zone B1 Refrigerated Unit', 'Temp', 'Zone', 4, 'Active'),
('Shipment GPS Tracker 001', 'GPS', 'Shipment', 1, 'Active'),
('Shipment GPS Tracker 002', 'GPS', 'Shipment', 2, 'Inactive'),
('Zone C1 Ambient Monitor', 'Temp', 'Zone', 6, 'Active'),
('Central Hub Temperature', 'Temp', 'Zone', 8, 'Active');

-- Insert TelemetryRecord Mock Data
INSERT INTO [TelemetryRecord] ([SensorID], [Timestamp], [Temperature], [Humidity], [Location], [IsExcursion]) VALUES
(1, DATEADD(HOUR, -24, GETUTCDATE()), 4.5, 55.2, 'Apollo Hospital Zone A1', 0),
(1, DATEADD(HOUR, -12, GETUTCDATE()), 4.3, 54.8, 'Apollo Hospital Zone A1', 0),
(1, DATEADD(HOUR, -1, GETUTCDATE()), 4.2, 55.1, 'Apollo Hospital Zone A1', 0),
(2, DATEADD(HOUR, -24, GETUTCDATE()), 4.2, 65.0, 'Apollo Hospital Zone A1', 0),
(3, DATEADD(HOUR, -24, GETUTCDATE()), -18.5, 45.3, 'Apollo Hospital Zone A2', 0),
(3, DATEADD(HOUR, -12, GETUTCDATE()), -18.7, 44.9, 'Apollo Hospital Zone A2', 0),
(3, DATEADD(HOUR, -2, GETUTCDATE()), 8.2, 70.5, 'Apollo Hospital Zone A2', 1),  -- EXCURSION!
(4, DATEADD(HOUR, -24, GETUTCDATE()), 5.1, 60.2, 'Max Healthcare Zone B1', 0),
(5, DATEADD(HOUR, -24, GETUTCDATE()), 25.3, 45.0, '13.0827,80.2707', 0),
(7, DATEADD(HOUR, -24, GETUTCDATE()), 22.5, 50.0, 'Regional Medical Center Zone C1', 0),
(7, DATEADD(HOUR, -12, GETUTCDATE()), 23.1, 51.2, 'Regional Medical Center Zone C1', 0),
(8, DATEADD(HOUR, -24, GETUTCDATE()), 21.8, 48.5, 'Distribution Hub Delhi', 0);