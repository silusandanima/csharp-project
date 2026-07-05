CREATE DATABASE ArtisanCraftDB;
GO

USE ArtisanCraftDB;
GO

CREATE TABLE Users (
    Username VARCHAR(50) NOT NULL,
    Password VARCHAR(255) NOT NULL

);

CREATE TABLE Suppliers (
    SupplierID INT PRIMARY KEY IDENTITY(1,1) NOT NULL,
    SupplierName VARCHAR(100) NOT NULL,
    Phone VARCHAR(20),
    Address VARCHAR(200)
);

CREATE TABLE Materials (
    MaterialID INT PRIMARY KEY IDENTITY(1,1),
    MaterialName VARCHAR(100) NOT NULL,
    Quantity DECIMAL(10,2),
    Unit VARCHAR(20),
    SupplierID INT,

    FOREIGN KEY (SupplierID)
    REFERENCES Suppliers(SupplierID)
);

CREATE TABLE Products (
    ProductID INT PRIMARY KEY IDENTITY(1,1),
    ProductName VARCHAR(100) NOT NULL,
    Description VARCHAR(255),
    Price DECIMAL(10,2),
    StockQuantity INT
);

CREATE TABLE Customers (
    CustomerID INT PRIMARY KEY IDENTITY(1,1) NOT NULL,
    CustomerName VARCHAR(100) NOT NULL,
    Phone VARCHAR(20),
    Email VARCHAR(100),
    Address VARCHAR(200)
);

CREATE TABLE Orders (
    OrderID INT PRIMARY KEY IDENTITY(1,1),
    CustomerID INT,
    OrderDate DATE,
    Status VARCHAR(20),
    TotalAmount DECIMAL(10,2),

    FOREIGN KEY (CustomerID)
    REFERENCES Customers(CustomerID)
);

CREATE TABLE OrderItems (
    OrderItemID INT PRIMARY KEY IDENTITY(1,1),
    OrderID INT,
    ProductID INT,
    Quantity INT,
    UnitPrice DECIMAL(10,2),

    FOREIGN KEY (OrderID)
    REFERENCES Orders(OrderID),

    FOREIGN KEY (ProductID)
    REFERENCES Products(ProductID)
);



CREATE TABLE Budget (
    BudgetID INT PRIMARY KEY IDENTITY(1,1),
    BudgetAmount DECIMAL(10,2) NOT NULL,
    SpentAmount DECIMAL(10,2) NOT NULL DEFAULT 0,
    BudgetMonth DATE
);
