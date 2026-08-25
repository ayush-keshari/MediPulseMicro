-- ============================================================================
-- MediPulseMicro - Auth Service Mock Data
-- ============================================================================

-- Insert User Mock Data
INSERT INTO [User] ([Name], [Role], [Email], [Phone], [Password]) VALUES
('Rajesh Kumar', 'Admin', 'rajesh.kumar@medipulse.com', '+91-9876543210', '$2a$11$...hashed_password_admin...'),
('Priya Singh', 'WarehouseManager', 'priya.singh@medipulse.com', '+91-9876543211', '$2a$11$...hashed_password...'),
('Amit Patel', 'Nurse', 'amit.patel@medipulse.com', '+91-9876543212', '$2a$11$...hashed_password...'),
('Sophia Johnson', 'ComplianceOfficer', 'sophia.johnson@medipulse.com', '+91-9876543213', '$2a$11$...hashed_password...'),
('Dr. Vikram Sharma', 'Doctor', 'vikram.sharma@medipulse.com', '+91-9876543214', '$2a$11$...hashed_password...'),
('Neha Gupta', 'WarehouseStaff', 'neha.gupta@medipulse.com', '+91-9876543215', '$2a$11$...hashed_password...'),
('Mohammed Hassan', 'LogisticsManager', 'hassan.m@medipulse.com', '+91-9876543216', '$2a$11$...hashed_password...');