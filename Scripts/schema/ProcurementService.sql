-- ============================================================================
-- ProcurementService Schema
-- Tables: Supplier, PurchaseOrder, Receipt
-- ============================================================================

-- Supplier: Vendors and pharmaceutical suppliers
CREATE TABLE [Supplier] (
    [SupplierID] INT PRIMARY KEY IDENTITY(1,1),
    [Name] NVARCHAR(100) NOT NULL,
    [SupplierType] NVARCHAR(50),
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Active'
);

-- PurchaseOrder: Orders placed with suppliers
CREATE TABLE [PurchaseOrder] (
    [POID] INT PRIMARY KEY IDENTITY(1,1),
    [SupplierID] INT NOT NULL,
    [OrderDate] DATETIME2 NOT NULL,
    [ExpectedDeliveryDate] DATETIME2,
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Draft',
    [Notes] NVARCHAR(500),
    CONSTRAINT FK_PurchaseOrder_Supplier FOREIGN KEY ([SupplierID]) REFERENCES [Supplier]([SupplierID]) ON DELETE RESTRICT
);

-- Receipt: Goods receipt notes (GRN) for received orders
CREATE TABLE [Receipt] (
    [ReceiptID] INT PRIMARY KEY IDENTITY(1,1),
    [POID] INT NOT NULL,
    [SupplierLot] NVARCHAR(100),
    [ReceivedDate] DATETIME2 NOT NULL,
    [ReceivedBy] NVARCHAR(100) NOT NULL,
    [QualityStatus] NVARCHAR(50) NOT NULL DEFAULT 'Accepted',
    [QuantityReceived] INT NOT NULL,
    CONSTRAINT FK_Receipt_PurchaseOrder FOREIGN KEY ([POID]) REFERENCES [PurchaseOrder]([POID]) ON DELETE CASCADE
);