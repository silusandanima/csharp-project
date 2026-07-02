using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CraftConnectPOS.Models;
using CraftConnectPOS.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.SQLite;

namespace CraftConnectPOS.Tests
{
    [TestClass]
    public class SqliteDataServiceTests
    {
        private string _testDirectory;
        private string _databasePath;
        private SqliteDataService _service;

        [TestInitialize]
        public async Task Initialize()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "CraftConnectPOS.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDirectory);
            _databasePath = Path.Combine(_testDirectory, "test.db");
            _service = new SqliteDataService(_databasePath);
            await _service.InitializeAsync();
        }

        [TestCleanup]
        public void Cleanup()
        {
            SQLiteConnection.ClearAllPools();
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [TestMethod]
        public async Task Initialize_IsIdempotent_AndSeedsExpectedData()
        {
            await _service.AddSupplierAsync(new SupplierItem
            {
                Supplier = "Persistent Supplier",
                Phone = "077-1111111",
                Location = "Colombo"
            });
            await _service.InitializeAsync();
            var products = await _service.GetProductsAsync();
            var suppliers = await _service.GetSuppliersAsync();
            var materials = await _service.GetMaterialsAsync();
            var orders = await _service.GetOrdersAsync();

            Assert.AreEqual(4, products.Count);
            Assert.AreEqual(5, suppliers.Count);
            Assert.AreEqual(1, suppliers.Count(item => item.Supplier == "Persistent Supplier"));
            Assert.AreEqual(4, materials.Count);
            Assert.AreEqual(4, orders.Count);
            Assert.AreEqual(1, ScalarCount("SELECT COUNT(*) FROM SchemaVersion;"));
            Assert.AreEqual(1, ScalarCount("SELECT COUNT(*) FROM Users;"));
        }

        [TestMethod]
        public async Task Authentication_UsesHash_AndRejectsWrongPassword()
        {
            var valid = await _service.AuthenticateAsync("ADMIN", "demo123");
            var invalid = await _service.AuthenticateAsync("admin", "wrong-password");

            Assert.IsNotNull(valid);
            Assert.AreEqual("admin", valid.Username);
            Assert.AreEqual("Business Owner", valid.Role);
            Assert.IsNull(invalid);

            using (var connection = OpenRawConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT PasswordHash, PasswordSalt FROM Users WHERE Username = 'admin';";
                using (var reader = command.ExecuteReader())
                {
                    Assert.IsTrue(reader.Read());
                    Assert.AreNotEqual("demo123", reader.GetString(0));
                    Assert.AreNotEqual("demo123", reader.GetString(1));
                }
            }
        }

        [TestMethod]
        public async Task Crud_PersistsAcrossServiceInstances_AndEnforcesRelationships()
        {
            var supplier = new SupplierItem
            {
                Supplier = "Test Supplier",
                Phone = "077-1234567",
                Location = "Colombo"
            };
            await _service.AddSupplierAsync(supplier);
            supplier = (await _service.GetSuppliersAsync()).Single(item => item.Supplier == "Test Supplier");

            var material = new InventoryItem
            {
                Code = "TEST-MAT",
                Material = "Test Material",
                Quantity = 5,
                Unit = "kg",
                ReorderLevel = 2,
                SupplierId = supplier.Id
            };
            await _service.AddMaterialAsync(material);

            var product = new ProductItem
            {
                Code = "TEST-PROD",
                Name = "Test Product",
                Category = "Testing",
                StockQuantity = 8,
                UnitPrice = 1250m
            };
            await _service.AddProductAsync(product);
            product = (await _service.GetProductsAsync()).Single(item => item.Code == "TEST-PROD");

            var order = new CustomerOrder
            {
                OrderId = "TEST-ORDER",
                Customer = "Test Customer",
                ProductId = product.Id,
                Product = product.Name,
                Quantity = 2,
                Status = "Shipped",
                UnitPrice = product.UnitPrice,
                Total = product.UnitPrice * 2,
                OrderDate = DateTime.UtcNow
            };
            await _service.AddOrderAsync(order);

            var secondService = new SqliteDataService(_databasePath);
            await secondService.InitializeAsync();
            var persistedOrder = (await secondService.GetOrdersAsync()).Single(item => item.OrderId == "TEST-ORDER");
            Assert.AreEqual(2500m, persistedOrder.Total);

            product.UnitPrice = 5000m;
            await secondService.UpdateProductAsync(product);
            persistedOrder = (await secondService.GetOrdersAsync()).Single(item => item.OrderId == "TEST-ORDER");
            Assert.AreEqual(1250m, persistedOrder.UnitPrice, "Existing orders must keep their price snapshot.");

            material = (await secondService.GetMaterialsAsync()).Single(item => item.Code == "TEST-MAT");
            material.Quantity = 9;
            await secondService.UpdateMaterialAsync(material);

            supplier.Supplier = "Updated Supplier";
            await secondService.UpdateSupplierAsync(supplier);

            persistedOrder.Quantity = 3;
            persistedOrder.Status = "Completed";
            await secondService.UpdateOrderAsync(persistedOrder);
            persistedOrder = (await secondService.GetOrdersAsync()).Single(item => item.OrderId == "TEST-ORDER");
            Assert.AreEqual(1250m, persistedOrder.UnitPrice);
            Assert.AreEqual(3750m, persistedOrder.Total);

            var supplierError = await AssertThrowsAsync<DataStoreException>(
                () => secondService.DeleteSupplierAsync(supplier.Id));
            StringAssert.Contains(supplierError.Message, "used by one or more materials");
            var productError = await AssertThrowsAsync<DataStoreException>(
                () => secondService.DeleteProductAsync(product.Id));
            StringAssert.Contains(productError.Message, "used by one or more orders");

            await secondService.DeleteOrderAsync(persistedOrder.Id);
            await secondService.DeleteMaterialAsync(material.Id);
            await secondService.DeleteProductAsync(product.Id);
            await secondService.DeleteSupplierAsync(supplier.Id);

            var thirdService = new SqliteDataService(_databasePath);
            Assert.IsFalse((await thirdService.GetOrdersAsync()).Any(item => item.OrderId == "TEST-ORDER"));
            Assert.IsFalse((await thirdService.GetMaterialsAsync()).Any(item => item.Code == "TEST-MAT"));
            Assert.IsFalse((await thirdService.GetProductsAsync()).Any(item => item.Code == "TEST-PROD"));
            Assert.IsFalse((await thirdService.GetSuppliersAsync()).Any(item => item.Supplier == "Updated Supplier"));
        }

        [TestMethod]
        public async Task UniqueAndCheckConstraints_ReturnFriendlyErrors()
        {
            var duplicate = new ProductItem
            {
                Code = "CR-104",
                Name = "Duplicate",
                Category = "Testing",
                StockQuantity = 1,
                UnitPrice = 1
            };
            var duplicateError = await AssertThrowsAsync<DataStoreException>(
                () => _service.AddProductAsync(duplicate));
            Assert.AreEqual("A product with this code already exists.", duplicateError.Message);

            var invalid = new ProductItem
            {
                Code = "INVALID-STOCK",
                Name = "Invalid",
                Category = "Testing",
                StockQuantity = -1,
                UnitPrice = 1
            };
            var validationError = await AssertThrowsAsync<DataStoreException>(
                () => _service.AddProductAsync(invalid));
            Assert.AreEqual("Stock and unit price must be zero or more.", validationError.Message);

            var requiredError = await AssertThrowsAsync<DataStoreException>(
                () => _service.AddSupplierAsync(new SupplierItem()));
            Assert.AreEqual("Supplier name is required.", requiredError.Message);

            var supplierError = await AssertThrowsAsync<DataStoreException>(
                () => _service.AddSupplierAsync(new SupplierItem
                {
                    Supplier = "Ceylon Tea Traders",
                    Phone = "077-0000000",
                    Location = "Colombo"
                }));
            Assert.AreEqual("A supplier with this name already exists.", supplierError.Message);

            var supplier = (await _service.GetSuppliersAsync()).First();
            var materialError = await AssertThrowsAsync<DataStoreException>(
                () => _service.AddMaterialAsync(new InventoryItem
                {
                    Code = "MAT-201",
                    Material = "Duplicate material",
                    Quantity = 1,
                    Unit = "kg",
                    ReorderLevel = 0,
                    SupplierId = supplier.Id
                }));
            Assert.AreEqual("A material with this code already exists.", materialError.Message);

            var product = (await _service.GetProductsAsync()).First();
            await _service.AddOrderAsync(new CustomerOrder
            {
                OrderId = "UNIQUE-ORDER",
                Customer = "Customer",
                ProductId = product.Id,
                Quantity = 1,
                Status = "Pending",
                OrderDate = DateTime.UtcNow
            });
            var orderError = await AssertThrowsAsync<DataStoreException>(
                () => _service.AddOrderAsync(new CustomerOrder
                {
                    OrderId = "UNIQUE-ORDER",
                    Customer = "Another Customer",
                    ProductId = product.Id,
                    Quantity = 1,
                    Status = "Pending",
                    OrderDate = DateTime.UtcNow
                }));
            Assert.AreEqual("An order with this ID already exists.", orderError.Message);
        }

        [TestMethod]
        public async Task Reports_AreDatabaseDriven()
        {
            var before = await _service.GetDashboardAsync();
            var product = (await _service.GetProductsAsync()).First();
            await _service.AddOrderAsync(new CustomerOrder
            {
                OrderId = "REPORT-ORDER",
                Customer = "Report Customer",
                ProductId = product.Id,
                Product = product.Name,
                Quantity = 2,
                Status = "Completed",
                UnitPrice = 1000m,
                Total = 2000m,
                OrderDate = DateTime.UtcNow
            });

            var persisted = (await _service.GetOrdersAsync()).Single(item => item.OrderId == "REPORT-ORDER");
            Assert.AreEqual(product.UnitPrice, persisted.UnitPrice);
            Assert.AreEqual(product.UnitPrice * 2, persisted.Total);

            var after = await _service.GetDashboardAsync();
            var statistics = await _service.GetStatisticsAsync();
            Assert.AreEqual(
                ParseCurrency(before.Metrics.Single(item => item.Title == "Revenue").Value) + product.UnitPrice * 2,
                ParseCurrency(after.Metrics.Single(item => item.Title == "Revenue").Value));
            Assert.AreEqual("4", statistics.Metrics.Single(item => item.Title == "Materials").Value);
            Assert.AreEqual(6, statistics.Bars.Count);
            Assert.IsTrue(statistics.Breakdown.Count > 0);
            Assert.AreEqual(1, statistics.Bars.Count(item => item.IsCurrentMonth));
            Assert.IsTrue(statistics.Breakdown.All(item => item.Progress >= 0 && item.Progress <= 100));
        }

        [TestMethod]
        public async Task FailedSeedTransaction_DoesNotRecordSchemaVersion()
        {
            SQLiteConnection.ClearAllPools();
            var brokenPath = Path.Combine(_testDirectory, "broken-seed.db");
            using (var connection = new SQLiteConnection("Data Source=" + brokenPath + ";Version=3;"))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "CREATE TABLE Suppliers (Id INTEGER PRIMARY KEY, WrongColumn TEXT);";
                    command.ExecuteNonQuery();
                }
            }

            var brokenService = new SqliteDataService(brokenPath);
            await AssertThrowsAsync<Exception>(() => brokenService.InitializeAsync());
            SQLiteConnection.ClearAllPools();

            using (var connection = new SQLiteConnection("Data Source=" + brokenPath + ";Version=3;"))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='SchemaVersion';";
                    var tableExists = Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) == 1;
                    if (tableExists)
                    {
                        command.CommandText = "SELECT COUNT(*) FROM SchemaVersion;";
                        Assert.AreEqual(0, Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture));
                    }

                    Assert.AreEqual(0, CountIfTableExists(connection, "Users"));
                    Assert.AreEqual(0, CountIfTableExists(connection, "Products"));
                    Assert.AreEqual(0, CountIfTableExists(connection, "Materials"));
                    Assert.AreEqual(0, CountIfTableExists(connection, "Orders"));
                }
            }
        }

        [TestMethod]
        public async Task CorruptDatabase_ProducesControlledInitializationFailure()
        {
            SQLiteConnection.ClearAllPools();
            var corruptPath = Path.Combine(_testDirectory, "corrupt.db");
            File.WriteAllText(corruptPath, "this is not a sqlite database");
            var corruptService = new SqliteDataService(corruptPath);
            await AssertThrowsAsync<Exception>(() => corruptService.InitializeAsync());
        }

        [TestMethod]
        public async Task InaccessibleDatabasePath_ProducesControlledInitializationFailure()
        {
            var blockingFile = Path.Combine(_testDirectory, "not-a-directory");
            File.WriteAllText(blockingFile, "blocked");
            var service = new SqliteDataService(Path.Combine(blockingFile, "database.db"));

            await AssertThrowsAsync<IOException>(() => service.InitializeAsync());
        }

        private int ScalarCount(string sql)
        {
            using (var connection = OpenRawConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }

        private SQLiteConnection OpenRawConnection()
        {
            var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;");
            connection.Open();
            return connection;
        }

        private static int CountIfTableExists(SQLiteConnection connection, string tableName)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @name;";
                command.Parameters.AddWithValue("@name", tableName);
                if (Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) == 0)
                {
                    return 0;
                }

                command.CommandText = "SELECT COUNT(*) FROM [" + tableName + "];";
                command.Parameters.Clear();
                return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }

        private static decimal ParseCurrency(string value)
        {
            return decimal.Parse(
                value.Replace("Rs.", string.Empty).Replace(",", string.Empty).Trim(),
                CultureInfo.InvariantCulture);
        }

        private static async Task<TException> AssertThrowsAsync<TException>(Func<Task> action)
            where TException : Exception
        {
            try
            {
                await action();
            }
            catch (TException exception)
            {
                return exception;
            }

            Assert.Fail("Expected exception of type " + typeof(TException).FullName + ".");
            return null;
        }
    }
}
