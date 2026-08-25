-- ============================================================================
-- MediPulseMicro - Facility Service Schema
-- ============================================================================

-- Facility: Medical facilities (hospitals, clinics, distribution centers)
CREATE TABLE [Facility] (
	[FacilityID] INT PRIMARY KEY IDENTITY(1,1),
	[Name] NVARCHAR(100) NOT NULL,
	[Type] NVARCHAR(50),
	[Region] NVARCHAR(100)
);

-- StorageZone: Temperature-controlled zones within facilities
CREATE TABLE [StorageZone] (
	[ZoneID] INT PRIMARY KEY IDENTITY(1,1),
	[FacilityID] INT NOT NULL,
	[Name] NVARCHAR(100),
	[TemperatureProfile] NVARCHAR(50),
	[Capacity] DECIMAL(18, 2),
	CONSTRAINT FK_StorageZone_Facility FOREIGN KEY ([FacilityID]) REFERENCES [Facility]([FacilityID])
);