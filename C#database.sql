CREATE DATABASE ArtisanCraftDB;
GO

USE ArtisanCraftDB;
GO

CREATE TABLE Users (
    Username VARCHAR(50) NOT NULL,
    Password VARCHAR(255) NOT NULL

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
