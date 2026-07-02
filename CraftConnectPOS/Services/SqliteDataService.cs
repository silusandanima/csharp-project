using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CraftConnectPOS.Models;
using System.Data.SQLite;

namespace CraftConnectPOS.Services
{
    public class SqliteDataService :
        IDatabaseInitializer,
        IAuthenticationService,
        ISupplierRepository,
        IMaterialRepository,
        IProductRepository,
        IOrderRepository,
        IReportingService
    {
        private const int CurrentSchemaVersion = 1;
        private static readonly HashSet<string> ValidOrderStatuses =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "Pending",
                "Processing",
                "Ready to Ship",
                "Shipped",
                "Completed"
            };
        private readonly string _connectionString;

        public SqliteDataService(string databasePath)
        {
            if (string.IsNullOrWhiteSpace(databasePath))
            {
                throw new ArgumentException("A database path is required.", nameof(databasePath));
            }

            DatabasePath = Path.GetFullPath(databasePath);
            _connectionString = "Data Source=" + DatabasePath + ";Version=3;Pooling=True;";
        }

        public string DatabasePath { get; }

        public Task InitializeAsync()
        {
            return RunAsync(connection =>
            {
                using (var transaction = connection.BeginTransaction())
                {
                    ExecuteNonQuery(connection, transaction, @"
CREATE TABLE IF NOT EXISTS SchemaVersion (
    Version INTEGER NOT NULL,
    AppliedUtc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Username TEXT NOT NULL COLLATE NOCASE UNIQUE,
    PasswordHash TEXT NOT NULL,
    PasswordSalt TEXT NOT NULL,
    PasswordIterations INTEGER NOT NULL,
    Role TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Suppliers (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL COLLATE NOCASE UNIQUE,
    Phone TEXT NOT NULL,
    Location TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Products (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Code TEXT NOT NULL COLLATE NOCASE UNIQUE,
    Name TEXT NOT NULL,
    Category TEXT NOT NULL,
    StockQuantity INTEGER NOT NULL CHECK (StockQuantity >= 0),
    UnitPrice NUMERIC NOT NULL CHECK (UnitPrice >= 0)
);

CREATE TABLE IF NOT EXISTS Materials (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Code TEXT NOT NULL COLLATE NOCASE UNIQUE,
    Name TEXT NOT NULL,
    Quantity INTEGER NOT NULL CHECK (Quantity >= 0),
    Unit TEXT NOT NULL,
    ReorderLevel INTEGER NOT NULL CHECK (ReorderLevel >= 0),
    SupplierId INTEGER NOT NULL,
    FOREIGN KEY (SupplierId) REFERENCES Suppliers(Id) ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS Orders (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    OrderNumber TEXT NOT NULL COLLATE NOCASE UNIQUE,
    CustomerName TEXT NOT NULL,
    ProductId INTEGER NOT NULL,
    Quantity INTEGER NOT NULL CHECK (Quantity > 0),
    Status TEXT NOT NULL,
    UnitPrice NUMERIC NOT NULL CHECK (UnitPrice >= 0),
    Total NUMERIC NOT NULL CHECK (Total >= 0),
    OrderDate TEXT NOT NULL,
    FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE RESTRICT
);");

                    var version = ExecuteScalar<long>(connection, transaction,
                        "SELECT COALESCE(MAX(Version), 0) FROM SchemaVersion;");

                    if (version > CurrentSchemaVersion)
                    {
                        throw new InvalidOperationException(
                            "The database was created by a newer version of CraftConnectPOS.");
                    }

                    if (version == 0)
                    {
                        SeedDatabase(connection, transaction);
                        using (var command = CreateCommand(connection, transaction,
                            "INSERT INTO SchemaVersion (Version, AppliedUtc) VALUES (@version, @appliedUtc);"))
                        {
                            command.Parameters.AddWithValue("@version", CurrentSchemaVersion);
                            command.Parameters.AddWithValue("@appliedUtc", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
                            command.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                }
            });
        }

        public Task<UserAccount> AuthenticateAsync(string username, string password)
        {
            return RunAsync(connection =>
            {
                using (var command = CreateCommand(connection, null, @"
SELECT Id, Username, PasswordHash, PasswordSalt, PasswordIterations, Role
FROM Users
WHERE Username = @username
LIMIT 1;"))
                {
                    command.Parameters.AddWithValue("@username", (username ?? string.Empty).Trim());
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return null;
                        }

                        var passwordHash = reader.GetString(2);
                        var passwordSalt = reader.GetString(3);
                        var iterations = reader.GetInt32(4);
                        if (!PasswordHasher.Verify(password, passwordSalt, passwordHash, iterations))
                        {
                            return null;
                        }

                        return new UserAccount
                        {
                            Id = reader.GetInt64(0),
                            Username = reader.GetString(1),
                            Role = reader.GetString(5)
                        };
                    }
                }
            });
        }

        public Task<IList<SupplierItem>> GetSuppliersAsync()
        {
            return RunAsync<IList<SupplierItem>>(connection =>
            {
                var results = new List<SupplierItem>();
                using (var command = CreateCommand(connection, null, @"
SELECT s.Id, s.Name, s.Phone, s.Location,
       COALESCE(GROUP_CONCAT(m.Name, ', '), '') AS Materials
FROM Suppliers s
LEFT JOIN Materials m ON m.SupplierId = s.Id
GROUP BY s.Id, s.Name, s.Phone, s.Location
ORDER BY s.Name COLLATE NOCASE;"))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new SupplierItem
                        {
                            Id = reader.GetInt64(0),
                            Supplier = reader.GetString(1),
                            Phone = reader.GetString(2),
                            Location = reader.GetString(3),
                            Materials = reader.GetString(4)
                        });
                    }
                }

                return results;
            });
        }

        public Task AddSupplierAsync(SupplierItem supplier)
        {
            ValidateSupplier(supplier, false);
            return ExecuteWriteAsync(@"
INSERT INTO Suppliers (Name, Phone, Location)
VALUES (@name, @phone, @location);",
                command =>
                {
                    command.Parameters.AddWithValue("@name", supplier.Supplier.Trim());
                    command.Parameters.AddWithValue("@phone", supplier.Phone.Trim());
                    command.Parameters.AddWithValue("@location", supplier.Location.Trim());
                }, "A supplier with this name already exists.");
        }

        public Task UpdateSupplierAsync(SupplierItem supplier)
        {
            ValidateSupplier(supplier, true);
            return ExecuteWriteAsync(@"
UPDATE Suppliers
SET Name = @name, Phone = @phone, Location = @location
WHERE Id = @id;",
                command =>
                {
                    command.Parameters.AddWithValue("@name", supplier.Supplier.Trim());
                    command.Parameters.AddWithValue("@phone", supplier.Phone.Trim());
                    command.Parameters.AddWithValue("@location", supplier.Location.Trim());
                    command.Parameters.AddWithValue("@id", supplier.Id);
                }, "A supplier with this name already exists.");
        }

        public Task DeleteSupplierAsync(long id)
        {
            ValidateId(id, "supplier");
            return ExecuteWriteAsync("DELETE FROM Suppliers WHERE Id = @id;",
                command => command.Parameters.AddWithValue("@id", id),
                null,
                "This supplier is used by one or more materials. Reassign those materials before deleting it.");
        }

        public Task<IList<InventoryItem>> GetMaterialsAsync()
        {
            return RunAsync<IList<InventoryItem>>(connection =>
            {
                var results = new List<InventoryItem>();
                using (var command = CreateCommand(connection, null, @"
SELECT m.Id, m.Code, m.Name, m.Quantity, m.Unit, m.ReorderLevel,
       m.SupplierId, s.Name
FROM Materials m
INNER JOIN Suppliers s ON s.Id = m.SupplierId
ORDER BY m.Code COLLATE NOCASE;"))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new InventoryItem
                        {
                            Id = reader.GetInt64(0),
                            Code = reader.GetString(1),
                            Material = reader.GetString(2),
                            Quantity = reader.GetInt32(3),
                            Unit = reader.GetString(4),
                            ReorderLevel = reader.GetInt32(5),
                            SupplierId = reader.GetInt64(6),
                            Supplier = reader.GetString(7)
                        });
                    }
                }

                return results;
            });
        }

        public Task AddMaterialAsync(InventoryItem material)
        {
            ValidateMaterial(material, false);
            return ExecuteWriteAsync(@"
INSERT INTO Materials (Code, Name, Quantity, Unit, ReorderLevel, SupplierId)
VALUES (@code, @name, @quantity, @unit, @reorderLevel, @supplierId);",
                command => AddMaterialParameters(command, material, false),
                "A material with this code already exists.");
        }

        public Task UpdateMaterialAsync(InventoryItem material)
        {
            ValidateMaterial(material, true);
            return ExecuteWriteAsync(@"
UPDATE Materials
SET Code = @code, Name = @name, Quantity = @quantity, Unit = @unit,
    ReorderLevel = @reorderLevel, SupplierId = @supplierId
WHERE Id = @id;",
                command => AddMaterialParameters(command, material, true),
                "A material with this code already exists.");
        }

        public Task DeleteMaterialAsync(long id)
        {
            ValidateId(id, "material");
            return ExecuteWriteAsync("DELETE FROM Materials WHERE Id = @id;",
                command => command.Parameters.AddWithValue("@id", id),
                null);
        }

        public Task<IList<ProductItem>> GetProductsAsync()
        {
            return RunAsync<IList<ProductItem>>(connection =>
            {
                var results = new List<ProductItem>();
                using (var command = CreateCommand(connection, null, @"
SELECT Id, Code, Name, Category, StockQuantity, UnitPrice
FROM Products
ORDER BY Code COLLATE NOCASE;"))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new ProductItem
                        {
                            Id = reader.GetInt64(0),
                            Code = reader.GetString(1),
                            Name = reader.GetString(2),
                            Category = reader.GetString(3),
                            StockQuantity = reader.GetInt32(4),
                            UnitPrice = Convert.ToDecimal(reader.GetValue(5), CultureInfo.InvariantCulture)
                        });
                    }
                }

                return results;
            });
        }

        public Task AddProductAsync(ProductItem product)
        {
            ValidateProduct(product, false);
            return ExecuteWriteAsync(@"
INSERT INTO Products (Code, Name, Category, StockQuantity, UnitPrice)
VALUES (@code, @name, @category, @stock, @price);",
                command => AddProductParameters(command, product, false),
                "A product with this code already exists.");
        }

        public Task UpdateProductAsync(ProductItem product)
        {
            ValidateProduct(product, true);
            return ExecuteWriteAsync(@"
UPDATE Products
SET Code = @code, Name = @name, Category = @category,
    StockQuantity = @stock, UnitPrice = @price
WHERE Id = @id;",
                command => AddProductParameters(command, product, true),
                "A product with this code already exists.");
        }

        public Task DeleteProductAsync(long id)
        {
            ValidateId(id, "product");
            return ExecuteWriteAsync("DELETE FROM Products WHERE Id = @id;",
                command => command.Parameters.AddWithValue("@id", id),
                null,
                "This product is used by one or more orders. Delete those orders before deleting it.");
        }

        public Task<IList<CustomerOrder>> GetOrdersAsync()
        {
            return RunAsync<IList<CustomerOrder>>(connection =>
            {
                var results = new List<CustomerOrder>();
                using (var command = CreateCommand(connection, null, @"
SELECT o.Id, o.OrderNumber, o.CustomerName, o.ProductId, p.Name,
       o.Quantity, o.Status, o.UnitPrice, o.Total, o.OrderDate
FROM Orders o
INNER JOIN Products p ON p.Id = o.ProductId
ORDER BY o.OrderDate DESC, o.Id DESC;"))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(ReadOrder(reader));
                    }
                }

                return results;
            });
        }

        public Task AddOrderAsync(CustomerOrder order)
        {
            ValidateOrder(order, false);
            return SaveOrderAsync(order, false);
        }

        public Task UpdateOrderAsync(CustomerOrder order)
        {
            ValidateOrder(order, true);
            return SaveOrderAsync(order, true);
        }

        public Task DeleteOrderAsync(long id)
        {
            ValidateId(id, "order");
            return ExecuteWriteAsync("DELETE FROM Orders WHERE Id = @id;",
                command => command.Parameters.AddWithValue("@id", id),
                null);
        }

        public Task<DashboardData> GetDashboardAsync()
        {
            return RunAsync(connection =>
            {
                var productCount = ExecuteScalar<long>(connection, null, "SELECT COUNT(*) FROM Products;");
                var orderCount = ExecuteScalar<long>(connection, null, "SELECT COUNT(*) FROM Orders;");
                var lowStockCount = ExecuteScalar<long>(connection, null,
                    "SELECT COUNT(*) FROM Materials WHERE Quantity <= ReorderLevel;");
                var revenue = ExecuteScalar<decimal>(connection, null,
                    "SELECT COALESCE(SUM(Total), 0) FROM Orders WHERE Status IN ('Shipped', 'Completed');");

                var recentOrders = new List<OrderRow>();
                using (var command = CreateCommand(connection, null, @"
SELECT o.OrderNumber, o.CustomerName, p.Name, o.Quantity, o.Status, o.Total
FROM Orders o
INNER JOIN Products p ON p.Id = o.ProductId
ORDER BY o.OrderDate DESC, o.Id DESC
LIMIT 4;"))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        recentOrders.Add(new OrderRow
                        {
                            OrderId = reader.GetString(0),
                            Customer = reader.GetString(1),
                            Product = reader.GetString(2),
                            Quantity = reader.GetInt32(3).ToString(CultureInfo.InvariantCulture),
                            Status = reader.GetString(4),
                            Total = FormatCurrency(Convert.ToDecimal(reader.GetValue(5), CultureInfo.InvariantCulture))
                        });
                    }
                }

                var lowStockItems = new List<InventoryRow>();
                using (var command = CreateCommand(connection, null, @"
SELECT Name, Quantity, Unit
FROM Materials
WHERE Quantity <= ReorderLevel
ORDER BY Quantity ASC, Name COLLATE NOCASE
LIMIT 5;"))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lowStockItems.Add(new InventoryRow
                        {
                            Material = reader.GetString(0),
                            Quantity = reader.GetInt32(1) + " " + reader.GetString(2) + " remaining",
                            Status = "Low Stock"
                        });
                    }
                }

                return new DashboardData
                {
                    Metrics = new List<MetricCard>
                    {
                        new MetricCard { Title = "Products", Value = productCount.ToString(CultureInfo.InvariantCulture), Note = "Catalogue items" },
                        new MetricCard { Title = "Orders", Value = orderCount.ToString(CultureInfo.InvariantCulture), Note = "All recorded orders" },
                        new MetricCard { Title = "Revenue", Value = FormatCurrency(revenue), Note = "Shipped and completed" },
                        new MetricCard { Title = "Low Stock", Value = lowStockCount.ToString(CultureInfo.InvariantCulture), Note = "Items to reorder" }
                    },
                    RecentOrders = recentOrders,
                    LowStockItems = lowStockItems
                };
            });
        }

        public Task<StatisticsData> GetStatisticsAsync()
        {
            return RunAsync(connection =>
            {
                var totalRevenue = ExecuteScalar<decimal>(connection, null,
                    "SELECT COALESCE(SUM(Total), 0) FROM Orders WHERE Status IN ('Shipped', 'Completed');");
                var materialCount = ExecuteScalar<long>(connection, null, "SELECT COUNT(*) FROM Materials;");
                var activeOrderCount = ExecuteScalar<long>(connection, null,
                    "SELECT COUNT(*) FROM Orders WHERE Status IN ('Pending', 'Processing', 'Ready to Ship');");

                var monthValues = new Dictionary<string, decimal>(StringComparer.Ordinal);
                using (var command = CreateCommand(connection, null, @"
SELECT strftime('%Y-%m', OrderDate) AS RevenueMonth, COALESCE(SUM(Total), 0)
FROM Orders
WHERE Status IN ('Shipped', 'Completed')
GROUP BY strftime('%Y-%m', OrderDate);"))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        monthValues[reader.GetString(0)] =
                            Convert.ToDecimal(reader.GetValue(1), CultureInfo.InvariantCulture);
                    }
                }

                var months = new List<Tuple<DateTime, decimal>>();
                var currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                for (var offset = 5; offset >= 0; offset--)
                {
                    var month = currentMonth.AddMonths(-offset);
                    decimal amount;
                    monthValues.TryGetValue(month.ToString("yyyy-MM", CultureInfo.InvariantCulture), out amount);
                    months.Add(Tuple.Create(month, amount));
                }

                var maxRevenue = Math.Max(1m, months.Max(item => item.Item2));
                var bars = months.Select(item => new RevenueBar
                {
                    Month = item.Item1.ToString("MMM", CultureInfo.InvariantCulture),
                    Amount = FormatCurrency(item.Item2),
                    Height = item.Item2 == 0 ? 8 : Math.Max(24, (double)(item.Item2 / maxRevenue) * 172),
                    IsCurrentMonth = item.Item1 == currentMonth
                }).ToList();

                var breakdown = new List<MetricCard>();
                using (var command = CreateCommand(connection, null, @"
SELECT p.Category, COALESCE(SUM(o.Total), 0) AS Revenue
FROM Orders o
INNER JOIN Products p ON p.Id = o.ProductId
WHERE o.Status IN ('Shipped', 'Completed')
GROUP BY p.Category
ORDER BY Revenue DESC;"))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var categoryRevenue = Convert.ToDecimal(reader.GetValue(1), CultureInfo.InvariantCulture);
                        var share = totalRevenue <= 0 ? 0 : Math.Round(categoryRevenue / totalRevenue * 100, 0);
                        breakdown.Add(new MetricCard
                        {
                            Title = reader.GetString(0),
                            Value = FormatCurrency(categoryRevenue),
                            Note = share.ToString("0", CultureInfo.InvariantCulture) + "% share",
                            Progress = (double)share
                        });
                    }
                }

                var sixMonthRevenue = months.Sum(item => item.Item2);
                return new StatisticsData
                {
                    Metrics = new List<MetricCard>
                    {
                        new MetricCard { Title = "Total Revenue", Value = FormatCurrency(totalRevenue), Note = "Shipped and completed" },
                        new MetricCard { Title = "Monthly Avg", Value = FormatCurrency(sixMonthRevenue / 6m), Note = "Across the displayed months" },
                        new MetricCard { Title = "Active Orders", Value = activeOrderCount.ToString(CultureInfo.InvariantCulture), Note = "Awaiting completion" },
                        new MetricCard { Title = "Materials", Value = materialCount.ToString(CultureInfo.InvariantCulture), Note = "Tracked inventory items" }
                    },
                    Bars = bars,
                    Breakdown = breakdown
                };
            });
        }

        private Task ExecuteWriteAsync(
            string sql,
            Action<SQLiteCommand> configure,
            string duplicateError,
            string relationshipError = null)
        {
            return RunAsync(connection =>
            {
                try
                {
                    using (var command = CreateCommand(connection, null, sql))
                    {
                        configure(command);
                        if (command.ExecuteNonQuery() == 0)
                        {
                            throw new DataStoreException("The selected record no longer exists.");
                        }
                    }
                }
                catch (SQLiteException exception)
                {
                    throw TranslateWriteException(exception, duplicateError, relationshipError);
                }
            });
        }

        private Task SaveOrderAsync(CustomerOrder order, bool isUpdate)
        {
            return RunAsync(connection =>
            {
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        decimal unitPrice;
                        DateTime orderDate;
                        if (isUpdate)
                        {
                            long existingProductId;
                            decimal existingUnitPrice;
                            using (var existingCommand = CreateCommand(connection, transaction, @"
SELECT ProductId, UnitPrice, OrderDate
FROM Orders
WHERE Id = @id;"))
                            {
                                existingCommand.Parameters.AddWithValue("@id", order.Id);
                                using (var reader = existingCommand.ExecuteReader())
                                {
                                    if (!reader.Read())
                                    {
                                        throw new DataStoreException("The selected order no longer exists.");
                                    }

                                    existingProductId = reader.GetInt64(0);
                                    existingUnitPrice =
                                        Convert.ToDecimal(reader.GetValue(1), CultureInfo.InvariantCulture);
                                    orderDate = DateTime.Parse(
                                        reader.GetString(2),
                                        CultureInfo.InvariantCulture,
                                        DateTimeStyles.RoundtripKind);
                                }
                            }

                            unitPrice = existingProductId == order.ProductId
                                ? existingUnitPrice
                                : GetProductPrice(connection, transaction, order.ProductId);
                        }
                        else
                        {
                            unitPrice = GetProductPrice(connection, transaction, order.ProductId);
                            orderDate = order.OrderDate == default(DateTime) ? DateTime.UtcNow : order.OrderDate;
                        }

                        var sql = isUpdate
                            ? @"
UPDATE Orders
SET OrderNumber = @orderNumber, CustomerName = @customer, ProductId = @productId,
    Quantity = @quantity, Status = @status, UnitPrice = @unitPrice,
    Total = @total, OrderDate = @orderDate
WHERE Id = @id;"
                            : @"
INSERT INTO Orders
    (OrderNumber, CustomerName, ProductId, Quantity, Status, UnitPrice, Total, OrderDate)
VALUES
    (@orderNumber, @customer, @productId, @quantity, @status, @unitPrice, @total, @orderDate);";

                        using (var command = CreateCommand(connection, transaction, sql))
                        {
                            command.Parameters.AddWithValue("@orderNumber", order.OrderId.Trim());
                            command.Parameters.AddWithValue("@customer", order.Customer.Trim());
                            command.Parameters.AddWithValue("@productId", order.ProductId);
                            command.Parameters.AddWithValue("@quantity", order.Quantity);
                            command.Parameters.AddWithValue("@status", order.Status);
                            command.Parameters.AddWithValue("@unitPrice", unitPrice);
                            command.Parameters.AddWithValue("@total", unitPrice * order.Quantity);
                            command.Parameters.AddWithValue("@orderDate", orderDate.ToString("o", CultureInfo.InvariantCulture));
                            if (isUpdate)
                            {
                                command.Parameters.AddWithValue("@id", order.Id);
                            }
                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch (SQLiteException exception)
                    {
                        throw TranslateWriteException(
                            exception,
                            "An order with this ID already exists.",
                            "Select a product that still exists.");
                    }
                }
            });
        }

        private Task RunAsync(Action<SQLiteConnection> work)
        {
            return Task.Run(() =>
            {
                using (var connection = OpenConnection())
                {
                    work(connection);
                }
            });
        }

        private Task<T> RunAsync<T>(Func<SQLiteConnection, T> work)
        {
            return Task.Run(() =>
            {
                using (var connection = OpenConnection())
                {
                    return work(connection);
                }
            });
        }

        private SQLiteConnection OpenConnection()
        {
            var directory = Path.GetDirectoryName(DatabasePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var connection = new SQLiteConnection(_connectionString);
            try
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "PRAGMA foreign_keys = ON; PRAGMA busy_timeout = 5000; PRAGMA journal_mode = WAL;";
                    command.ExecuteNonQuery();
                }
                return connection;
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        private static SQLiteCommand CreateCommand(SQLiteConnection connection, SQLiteTransaction transaction, string sql)
        {
            var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = transaction;
            return command;
        }

        private static void ExecuteNonQuery(SQLiteConnection connection, SQLiteTransaction transaction, string sql)
        {
            using (var command = CreateCommand(connection, transaction, sql))
            {
                command.ExecuteNonQuery();
            }
        }

        private static T ExecuteScalar<T>(SQLiteConnection connection, SQLiteTransaction transaction, string sql)
        {
            using (var command = CreateCommand(connection, transaction, sql))
            {
                var value = command.ExecuteScalar();
                if (value == null || value == DBNull.Value)
                {
                    return default(T);
                }

                return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
            }
        }

        private static void AddMaterialParameters(SQLiteCommand command, InventoryItem material, bool includeId)
        {
            command.Parameters.AddWithValue("@code", material.Code.Trim());
            command.Parameters.AddWithValue("@name", material.Material.Trim());
            command.Parameters.AddWithValue("@quantity", material.Quantity);
            command.Parameters.AddWithValue("@unit", material.Unit.Trim());
            command.Parameters.AddWithValue("@reorderLevel", material.ReorderLevel);
            command.Parameters.AddWithValue("@supplierId", material.SupplierId);
            if (includeId)
            {
                command.Parameters.AddWithValue("@id", material.Id);
            }
        }

        private static void AddProductParameters(SQLiteCommand command, ProductItem product, bool includeId)
        {
            command.Parameters.AddWithValue("@code", product.Code.Trim());
            command.Parameters.AddWithValue("@name", product.Name.Trim());
            command.Parameters.AddWithValue("@category", product.Category.Trim());
            command.Parameters.AddWithValue("@stock", product.StockQuantity);
            command.Parameters.AddWithValue("@price", product.UnitPrice);
            if (includeId)
            {
                command.Parameters.AddWithValue("@id", product.Id);
            }
        }

        private static decimal GetProductPrice(
            SQLiteConnection connection,
            SQLiteTransaction transaction,
            long productId)
        {
            using (var command = CreateCommand(
                connection,
                transaction,
                "SELECT UnitPrice FROM Products WHERE Id = @id;"))
            {
                command.Parameters.AddWithValue("@id", productId);
                var value = command.ExecuteScalar();
                if (value == null || value == DBNull.Value)
                {
                    throw new DataStoreException("Select a product that still exists.");
                }

                return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            }
        }

        private static DataStoreException TranslateWriteException(
            SQLiteException exception,
            string duplicateError,
            string relationshipError)
        {
            var code = exception.ResultCode;
            if (code == SQLiteErrorCode.Constraint_Unique ||
                code == SQLiteErrorCode.Constraint_PrimaryKey ||
                Contains(exception.Message, "UNIQUE constraint failed"))
            {
                return new DataStoreException(
                    duplicateError ?? "A record with the same unique value already exists.",
                    exception);
            }

            if (code == SQLiteErrorCode.Constraint_ForeignKey ||
                Contains(exception.Message, "FOREIGN KEY constraint failed"))
            {
                return new DataStoreException(
                    relationshipError ?? "The selected related record does not exist.",
                    exception);
            }

            if (code == SQLiteErrorCode.Constraint_Check ||
                code == SQLiteErrorCode.Constraint_NotNull ||
                code == SQLiteErrorCode.Constraint ||
                Contains(exception.Message, "constraint failed"))
            {
                return new DataStoreException("The record contains invalid values.", exception);
            }

            return new DataStoreException("The database operation could not be completed.", exception);
        }

        private static bool Contains(string value, string fragment)
        {
            return (value ?? string.Empty).IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void ValidateSupplier(SupplierItem supplier, bool requireId)
        {
            if (supplier == null)
            {
                throw new DataStoreException("Supplier details are required.");
            }
            if (requireId)
            {
                ValidateId(supplier.Id, "supplier");
            }
            RequireText(supplier.Supplier, "Supplier name");
            RequireText(supplier.Phone, "Phone");
            RequireText(supplier.Location, "Location");
        }

        private static void ValidateMaterial(InventoryItem material, bool requireId)
        {
            if (material == null)
            {
                throw new DataStoreException("Material details are required.");
            }
            if (requireId)
            {
                ValidateId(material.Id, "material");
            }
            RequireText(material.Code, "Material code");
            RequireText(material.Material, "Material name");
            RequireText(material.Unit, "Unit");
            if (material.Quantity < 0 || material.ReorderLevel < 0)
            {
                throw new DataStoreException("Quantity and reorder level must be zero or more.");
            }
            if (material.SupplierId <= 0)
            {
                throw new DataStoreException("Select a supplier.");
            }
        }

        private static void ValidateProduct(ProductItem product, bool requireId)
        {
            if (product == null)
            {
                throw new DataStoreException("Product details are required.");
            }
            if (requireId)
            {
                ValidateId(product.Id, "product");
            }
            RequireText(product.Code, "Product code");
            RequireText(product.Name, "Product name");
            RequireText(product.Category, "Category");
            if (product.StockQuantity < 0 || product.UnitPrice < 0)
            {
                throw new DataStoreException("Stock and unit price must be zero or more.");
            }
        }

        private static void ValidateOrder(CustomerOrder order, bool requireId)
        {
            if (order == null)
            {
                throw new DataStoreException("Order details are required.");
            }
            if (requireId)
            {
                ValidateId(order.Id, "order");
            }
            RequireText(order.OrderId, "Order ID");
            RequireText(order.Customer, "Customer name");
            if (order.ProductId <= 0)
            {
                throw new DataStoreException("Select a product.");
            }
            if (order.Quantity <= 0)
            {
                throw new DataStoreException("Quantity must be greater than zero.");
            }
            if (!ValidOrderStatuses.Contains(order.Status ?? string.Empty))
            {
                throw new DataStoreException("Select a valid order status.");
            }
        }

        private static void ValidateId(long id, string recordName)
        {
            if (id <= 0)
            {
                throw new DataStoreException("Select a valid " + recordName + ".");
            }
        }

        private static void RequireText(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new DataStoreException(fieldName + " is required.");
            }
        }

        private static CustomerOrder ReadOrder(SQLiteDataReader reader)
        {
            return new CustomerOrder
            {
                Id = reader.GetInt64(0),
                OrderId = reader.GetString(1),
                Customer = reader.GetString(2),
                ProductId = reader.GetInt64(3),
                Product = reader.GetString(4),
                Quantity = reader.GetInt32(5),
                Status = reader.GetString(6),
                UnitPrice = Convert.ToDecimal(reader.GetValue(7), CultureInfo.InvariantCulture),
                Total = Convert.ToDecimal(reader.GetValue(8), CultureInfo.InvariantCulture),
                OrderDate = DateTime.Parse(reader.GetString(9), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
            };
        }

        private static void SeedDatabase(SQLiteConnection connection, SQLiteTransaction transaction)
        {
            var password = PasswordHasher.Create("demo123");
            using (var command = CreateCommand(connection, transaction, @"
INSERT INTO Users (Username, PasswordHash, PasswordSalt, PasswordIterations, Role)
VALUES (@username, @hash, @salt, @iterations, @role);"))
            {
                command.Parameters.AddWithValue("@username", "admin");
                command.Parameters.AddWithValue("@hash", password.Hash);
                command.Parameters.AddWithValue("@salt", password.Salt);
                command.Parameters.AddWithValue("@iterations", password.Iterations);
                command.Parameters.AddWithValue("@role", "Business Owner");
                command.ExecuteNonQuery();
            }

            InsertSupplier(connection, transaction, "Ceylon Tea Traders", "077-9323450", "Galle");
            InsertSupplier(connection, transaction, "Dumbara Weavers", "077-8927543", "Kandy");
            InsertSupplier(connection, transaction, "Ceylon Bark Co.", "071-7656343", "Colombo");
            InsertSupplier(connection, transaction, "Ambalangoda Clay Works", "071-5623462", "Ambalangoda");

            InsertProduct(connection, transaction, "CR-104", "Handwoven reed basket", "Home Decor", 42, 2450m);
            InsertProduct(connection, transaction, "CR-118", "Clay tea cup set", "Pottery", 18, 3200m);
            InsertProduct(connection, transaction, "CR-125", "Batik wall hanging", "Textiles", 9, 5500m);
            InsertProduct(connection, transaction, "CR-134", "Carved wooden mask", "Wood Crafts", 22, 7800m);

            InsertMaterial(connection, transaction, "MAT-201", "Terracotta clay", 120, "kg", 30, "Ambalangoda Clay Works");
            InsertMaterial(connection, transaction, "MAT-205", "Reeds", 12, "bundle", 15, "Dumbara Weavers");
            InsertMaterial(connection, transaction, "MAT-221", "Wax", 50, "kg", 20, "Ceylon Bark Co.");
            InsertMaterial(connection, transaction, "MAT-245", "Indigo dye", 3, "kg", 10, "Ceylon Tea Traders");

            InsertOrder(connection, transaction, "ORD-8421", "Jane Perera", "CR-125", 1, "Shipped", DateTime.UtcNow.AddMonths(-1));
            InsertOrder(connection, transaction, "ORD-8419", "Anita Singh", "CR-104", 3, "Shipped", DateTime.UtcNow.AddMonths(-2));
            InsertOrder(connection, transaction, "ORD-8414", "Rajesh Kumar", "CR-118", 1, "Pending", DateTime.UtcNow.AddDays(-12));
            InsertOrder(connection, transaction, "ORD-8405", "Priya Nair", "CR-134", 2, "Processing", DateTime.UtcNow.AddDays(-5));
        }

        private static void InsertSupplier(SQLiteConnection connection, SQLiteTransaction transaction, string name, string phone, string location)
        {
            using (var command = CreateCommand(connection, transaction,
                "INSERT INTO Suppliers (Name, Phone, Location) VALUES (@name, @phone, @location);"))
            {
                command.Parameters.AddWithValue("@name", name);
                command.Parameters.AddWithValue("@phone", phone);
                command.Parameters.AddWithValue("@location", location);
                command.ExecuteNonQuery();
            }
        }

        private static void InsertProduct(SQLiteConnection connection, SQLiteTransaction transaction, string code, string name, string category, int stock, decimal price)
        {
            using (var command = CreateCommand(connection, transaction, @"
INSERT INTO Products (Code, Name, Category, StockQuantity, UnitPrice)
VALUES (@code, @name, @category, @stock, @price);"))
            {
                command.Parameters.AddWithValue("@code", code);
                command.Parameters.AddWithValue("@name", name);
                command.Parameters.AddWithValue("@category", category);
                command.Parameters.AddWithValue("@stock", stock);
                command.Parameters.AddWithValue("@price", price);
                command.ExecuteNonQuery();
            }
        }

        private static void InsertMaterial(SQLiteConnection connection, SQLiteTransaction transaction, string code, string name, int quantity, string unit, int reorderLevel, string supplierName)
        {
            var supplierId = GetId(
                connection,
                transaction,
                "SELECT Id FROM Suppliers WHERE Name = @value LIMIT 1;",
                supplierName);
            using (var command = CreateCommand(connection, transaction, @"
INSERT INTO Materials (Code, Name, Quantity, Unit, ReorderLevel, SupplierId)
VALUES (@code, @name, @quantity, @unit, @reorderLevel, @supplierId);"))
            {
                command.Parameters.AddWithValue("@code", code);
                command.Parameters.AddWithValue("@name", name);
                command.Parameters.AddWithValue("@quantity", quantity);
                command.Parameters.AddWithValue("@unit", unit);
                command.Parameters.AddWithValue("@reorderLevel", reorderLevel);
                command.Parameters.AddWithValue("@supplierId", supplierId);
                command.ExecuteNonQuery();
            }
        }

        private static void InsertOrder(SQLiteConnection connection, SQLiteTransaction transaction, string number, string customer, string productCode, int quantity, string status, DateTime date)
        {
            var productId = GetId(
                connection,
                transaction,
                "SELECT Id FROM Products WHERE Code = @value LIMIT 1;",
                productCode);
            decimal unitPrice;
            using (var priceCommand = CreateCommand(connection, transaction,
                "SELECT UnitPrice FROM Products WHERE Id = @id;"))
            {
                priceCommand.Parameters.AddWithValue("@id", productId);
                unitPrice = Convert.ToDecimal(priceCommand.ExecuteScalar(), CultureInfo.InvariantCulture);
            }

            using (var command = CreateCommand(connection, transaction, @"
INSERT INTO Orders
    (OrderNumber, CustomerName, ProductId, Quantity, Status, UnitPrice, Total, OrderDate)
VALUES
    (@number, @customer, @productId, @quantity, @status, @unitPrice, @total, @date);"))
            {
                command.Parameters.AddWithValue("@number", number);
                command.Parameters.AddWithValue("@customer", customer);
                command.Parameters.AddWithValue("@productId", productId);
                command.Parameters.AddWithValue("@quantity", quantity);
                command.Parameters.AddWithValue("@status", status);
                command.Parameters.AddWithValue("@unitPrice", unitPrice);
                command.Parameters.AddWithValue("@total", unitPrice * quantity);
                command.Parameters.AddWithValue("@date", date.ToString("o", CultureInfo.InvariantCulture));
                command.ExecuteNonQuery();
            }
        }

        private static long GetId(
            SQLiteConnection connection,
            SQLiteTransaction transaction,
            string sql,
            string value)
        {
            using (var command = CreateCommand(connection, transaction, sql))
            {
                command.Parameters.AddWithValue("@value", value);
                return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }

        private static string FormatCurrency(decimal amount)
        {
            return "Rs. " + amount.ToString("N2", CultureInfo.InvariantCulture);
        }
    }
}
