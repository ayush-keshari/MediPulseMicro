-- ============================================================================
-- MediPulseMicro - Procurement Service Mock Data
-- ============================================================================

-- Insert Supplier Mock Data
INSERT INTO [Supplier] ([Name], [SupplierType], [Status]) VALUES
('Cipla Limited', 'Manufacturer', 'Active'),
('Lupin Pharmaceuticals', 'Manufacturer', 'Active'),
('Mankind Pharma', 'Manufacturer', 'Active'),
('GlaxoSmithKline India', 'Manufacturer', 'Active'),
('MedExpress Distributor', 'Distributor', 'Active'),
('PharmaCold Logistics', '3PL', 'Active'),
('Dr. Reddy Laboratories', 'Manufacturer', 'OnHold');

-- Insert PurchaseOrder Mock Data
INSERT INTO [PurchaseOrder] ([SupplierID], [OrderDate], [ExpectedDeliveryDate], [Status], [Notes]) VALUES
(1, '2026-05-15', '2026-06-05', 'Approved', 'Insulin supply for Apollo Network'),
(2, '2026-05-18', '2026-06-08', 'Shipped', 'Amoxicillin restocking'),
(3, '2026-05-20', '2026-06-10', 'PartiallyReceived', 'Surgical supplies bundle'),
(4, '2026-05-25', '2026-06-15', 'Draft', 'Vaccine procurement'),
(1, '2026-06-01', '2026-06-20', 'Submitted', 'Emergency replenishment'),
(5, '2026-05-28', '2026-06-05', 'FullyReceived', 'General consumables');

-- Insert Receipt Mock Data
INSERT INTO [Receipt] ([POID], [SupplierLot], [ReceivedDate], [ReceivedBy], [QualityStatus], [QuantityReceived]) VALUES
(1, 'LOT-INS-2026-001', '2026-06-01', 'user-warehouse-01', 'Accepted', 200),
(2, 'LOT-AMX-2026-001', '2026-06-08', 'user-warehouse-02', 'Accepted', 500),
(3, 'LOT-SUR-2026-001', '2026-06-09', 'user-warehouse-01', 'Accepted', 300),
(3, 'LOT-SUR-2026-002', '2026-06-12', 'user-warehouse-02', 'OnHold', 150),
(6, 'LOT-CON-2026-001', '2026-06-04', 'user-warehouse-03', 'Accepted', 1000),
(1, 'LOT-INS-2026-002', '2026-06-05', 'user-warehouse-01', 'Accepted', 150);