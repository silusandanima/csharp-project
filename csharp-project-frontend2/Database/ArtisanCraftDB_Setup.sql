/* ============================================================
   CraftConnectPOS Database Setup Script
   Safe to run more than once:
   - Creates ArtisanCraftDB if it does not exist
   - Creates missing tables
   - Adds ReorderLevel to Materials if it is missing
   - Does NOT delete existing data
   - Does NOT create a Budget table
   ============================================================ */

USE master;
GO

IF DB_ID(N'ArtisanCraftDB') IS NULL
BEGIN
    CREATE DATABASE ArtisanCraftDB;
END
GO

USE ArtisanCraftDB;
GO

/* ============================================================
   1. USERS
   ============================================================ */

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        UserID INT PRIMARY KEY IDENTITY(1,1),
        Username VARCHAR(50) NOT NULL UNIQUE,
        Password VARCHAR(255) NOT NULL
    );
END
GO

/* ============================================================
   2. SUPPLIERS
   ============================================================ */

IF OBJECT_ID(N'dbo.Suppliers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Suppliers (
        SupplierID INT PRIMARY KEY IDENTITY(1,1),
        SupplierName VARCHAR(100) NOT NULL,
        Phone VARCHAR(20),
        Address VARCHAR(200)
    );
END
GO

/* ============================================================
   3. MATERIALS
   ============================================================ */

IF OBJECT_ID(N'dbo.Materials', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Materials (
        MaterialID INT PRIMARY KEY IDENTITY(1,1),
        MaterialName VARCHAR(100) NOT NULL,
        Quantity DECIMAL(10,2),
        Unit VARCHAR(20),

        -- Used by Inventory and Dashboard low-stock checks.
        ReorderLevel INT NOT NULL
            CONSTRAINT DF_Materials_ReorderLevel DEFAULT (0),

        SupplierID INT,

        CONSTRAINT FK_Materials_Suppliers
            FOREIGN KEY (SupplierID)
            REFERENCES dbo.Suppliers(SupplierID)
    );
END
GO

-- For databases created with the earlier script:
-- add ReorderLevel without deleting any existing material data.
IF COL_LENGTH(N'dbo.Materials', N'ReorderLevel') IS NULL
BEGIN
    ALTER TABLE dbo.Materials
    ADD ReorderLevel INT NOT NULL
        CONSTRAINT DF_Materials_ReorderLevel DEFAULT (0);
END
GO

/* ============================================================
   4. PRODUCTS
   ============================================================ */

IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products (
        ProductID INT PRIMARY KEY IDENTITY(1,1),
        ProductName VARCHAR(100) NOT NULL,
        Description VARCHAR(255),
        Price DECIMAL(10,2),
        StockQuantity INT
    );
END
GO

/* ============================================================
   5. CUSTOMERS
   ============================================================ */

IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers (
        CustomerID INT PRIMARY KEY IDENTITY(1,1),
        CustomerName VARCHAR(100) NOT NULL,
        Phone VARCHAR(20),
        Email VARCHAR(100),
        Address VARCHAR(200)
    );
END
GO

/* ============================================================
   6. ORDERS
   ============================================================ */

IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Orders (
        OrderID INT PRIMARY KEY IDENTITY(1,1),
        CustomerID INT,
        OrderDate DATETIME2 NOT NULL
            CONSTRAINT DF_Orders_OrderDate DEFAULT SYSDATETIME(),
        Status VARCHAR(20),
        TotalAmount DECIMAL(10,2),

        CONSTRAINT FK_Orders_Customers
            FOREIGN KEY (CustomerID)
            REFERENCES dbo.Customers(CustomerID)
    );
END
GO

/* ============================================================
   7. ORDER ITEMS
   Must come after Orders and Products.
   ============================================================ */

IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderItems (
        OrderItemID INT PRIMARY KEY IDENTITY(1,1),
        OrderID INT NOT NULL,
        ProductID INT NOT NULL,
        Quantity INT NOT NULL,
        UnitPrice DECIMAL(10,2) NOT NULL,

        CONSTRAINT FK_OrderItems_Orders
            FOREIGN KEY (OrderID)
            REFERENCES dbo.Orders(OrderID),

        CONSTRAINT FK_OrderItems_Products
            FOREIGN KEY (ProductID)
            REFERENCES dbo.Products(ProductID)
    );
END
GO

/* ============================================================
   CHECK: list all user tables after setup
   ============================================================ */

SELECT
    SCHEMA_NAME(schema_id) AS SchemaName,
    name AS TableName
FROM sys.tables
WHERE is_ms_shipped = 0
ORDER BY SchemaName, TableName;
GO
