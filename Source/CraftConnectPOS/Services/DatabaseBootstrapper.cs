using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace CraftConnectPOS.Data
{
    public static class DatabaseBootstrapper
    {
        public static string GetDefaultConnectionString()
        {
            string dataDirectory = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Data");

            Directory.CreateDirectory(dataDirectory);

            string databasePath = Path.Combine(
                dataDirectory,
                "CraftConnect.db");

            return new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared,
                ForeignKeys = true,
                Pooling = true,
                DefaultTimeout = 5
            }.ToString();
        }

        public static void EnsureCreated(string connectionString)
        {
            SQLitePCL.Batteries.Init();

            var builder = new SqliteConnectionStringBuilder(connectionString);
            string directory = Path.GetDirectoryName(builder.DataSource);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                Execute(connection, "PRAGMA foreign_keys = ON;");
                Execute(connection, "PRAGMA busy_timeout = 5000;");
                Execute(connection, "PRAGMA journal_mode = DELETE;");

                using (var transaction = connection.BeginTransaction())
                {
                    foreach (string statement in SchemaStatements)
                    {
                        using (var command = connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            command.CommandText = statement;
                            command.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                }
            }
        }

        private static void Execute(
            SqliteConnection connection,
            string commandText)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandText;
                command.ExecuteNonQuery();
            }
        }

        private static readonly string[] SchemaStatements =
        {
            @"
            CREATE TABLE IF NOT EXISTS Users
            (
                UserID INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL COLLATE NOCASE UNIQUE,
                Password TEXT NOT NULL
            );",

            @"
            CREATE TABLE IF NOT EXISTS Suppliers
            (
                SupplierID INTEGER PRIMARY KEY AUTOINCREMENT,
                SupplierName TEXT NOT NULL COLLATE NOCASE UNIQUE,
                Phone TEXT NOT NULL CHECK (length(Phone) <= 20),
                Address TEXT NOT NULL CHECK (length(Address) <= 200)
            );",

            @"
            CREATE TABLE IF NOT EXISTS Products
            (
                ProductID INTEGER PRIMARY KEY AUTOINCREMENT,
                ProductName TEXT NOT NULL CHECK (length(ProductName) <= 100),
                Description TEXT NULL CHECK (
                    Description IS NULL OR length(Description) <= 255),
                Category TEXT NOT NULL CHECK (length(Category) <= 100),
                Price NUMERIC NOT NULL CHECK (Price >= 0),
                StockQuantity INTEGER NOT NULL CHECK (StockQuantity >= 0)
            );",

            @"
            CREATE TABLE IF NOT EXISTS Customers
            (
                CustomerID INTEGER PRIMARY KEY AUTOINCREMENT,
                CustomerName TEXT NOT NULL CHECK (length(CustomerName) <= 100),
                Phone TEXT NULL CHECK (Phone IS NULL OR length(Phone) <= 20),
                Email TEXT NULL CHECK (Email IS NULL OR length(Email) <= 100),
                Address TEXT NULL CHECK (Address IS NULL OR length(Address) <= 200)
            );",

            @"
            CREATE TABLE IF NOT EXISTS Materials
            (
                MaterialID INTEGER PRIMARY KEY AUTOINCREMENT,
                MaterialName TEXT NOT NULL COLLATE NOCASE
                    CHECK (length(MaterialName) <= 100),
                Quantity NUMERIC NOT NULL CHECK (Quantity >= 0),
                Unit TEXT NOT NULL CHECK (length(Unit) <= 20),
                ReorderLevel INTEGER NOT NULL DEFAULT 0
                    CHECK (ReorderLevel >= 0),
                SupplierID INTEGER NOT NULL,
                FOREIGN KEY (SupplierID)
                    REFERENCES Suppliers(SupplierID),
                UNIQUE (SupplierID, MaterialName)
            );",

            @"
            CREATE TABLE IF NOT EXISTS Orders
            (
                OrderID INTEGER PRIMARY KEY AUTOINCREMENT,
                CustomerID INTEGER NULL,
                OrderDate TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                Status TEXT NOT NULL CHECK (
                    Status IN (
                        'Pending',
                        'Processing',
                        'Ready to Ship',
                        'Shipped',
                        'Completed',
                        'Cancelled')),
                TotalAmount NUMERIC NOT NULL CHECK (TotalAmount >= 0),
                FOREIGN KEY (CustomerID)
                    REFERENCES Customers(CustomerID)
            );",

            @"
            CREATE TABLE IF NOT EXISTS OrderItems
            (
                OrderItemID INTEGER PRIMARY KEY AUTOINCREMENT,
                OrderID INTEGER NOT NULL,
                ProductID INTEGER NOT NULL,
                Quantity INTEGER NOT NULL CHECK (Quantity > 0),
                UnitPrice NUMERIC NOT NULL CHECK (UnitPrice >= 0),
                FOREIGN KEY (OrderID)
                    REFERENCES Orders(OrderID),
                FOREIGN KEY (ProductID)
                    REFERENCES Products(ProductID)
            );",

            @"CREATE INDEX IF NOT EXISTS IX_Orders_OrderDate
              ON Orders(OrderDate);",

            @"CREATE INDEX IF NOT EXISTS IX_OrderItems_ProductID
              ON OrderItems(ProductID);",

            @"CREATE INDEX IF NOT EXISTS IX_Materials_SupplierID
              ON Materials(SupplierID);",

            @"PRAGMA user_version = 1;"
        };
    }
}
