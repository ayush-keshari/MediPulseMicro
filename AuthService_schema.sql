-- ============================================================================
-- MediPulseMicro - Auth Service Schema
-- ============================================================================

-- User: System users with roles
CREATE TABLE [User] (
	[UserID] INT PRIMARY KEY IDENTITY(1,1),
	[Name] NVARCHAR(100) NOT NULL,
	[Role] NVARCHAR(50) NOT NULL,
	[Email] NVARCHAR(255) NOT NULL UNIQUE,
	[Phone] NVARCHAR(20),
	[Password] NVARCHAR(255) NOT NULL DEFAULT ''
);