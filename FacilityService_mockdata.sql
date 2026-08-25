-- ============================================================================
-- MediPulseMicro - Facility Service Mock Data
-- ============================================================================

-- Insert Facility Mock Data
INSERT INTO [Facility] ([Name], [Type], [Region]) VALUES
('Apollo Hospital Delhi', 'Hospital', 'North'),
('Max Healthcare Mumbai', 'Hospital', 'West'),
('Regional Medical Center Chennai', 'Hospital', 'South'),
('Fortis Healthcare Bangalore', 'Hospital', 'South'),
('Central Distribution Hub Delhi', 'Distribution Center', 'North'),
('Eastern Medical Clinic Kolkata', 'Clinic', 'East');

-- Insert StorageZone Mock Data
INSERT INTO [StorageZone] ([FacilityID], [Name], [TemperatureProfile], [Capacity]) VALUES
(1, 'Cold Storage A1', 'Refrigerated', 5000.00),
(1, 'Cold Storage A2', 'Freezer', 3000.00),
(1, 'Ambient Storage A3', 'Ambient', 10000.00),
(2, 'Cold Room B1', 'Refrigerated', 4500.00),
(2, 'Freezer B2', 'Freezer', 2500.00),
(3, 'Storage Zone C1', 'Ambient', 8000.00),
(4, 'Premium Cold Storage D1', 'Refrigerated', 6000.00),
(5, 'Hub Storage E1', 'Ambient', 50000.00),
(6, 'Clinic Storage F1', 'Ambient', 2000.00);