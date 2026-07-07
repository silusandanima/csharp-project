using System;
using System.IO;
using System.Linq;
using CraftConnectPOS.Data;
using CraftConnectPOS.Models;
using Microsoft.Data.Sqlite;

namespace CraftConnectPOS.Tests
{
    internal static class Program
    {
        private static int _assertionCount;

        private static int Main()
        {
            string databasePath = Path.Combine(
                Path.GetTempPath(),
                "CraftConnect_Test_" + Guid.NewGuid().ToString("N") + ".db");

            string connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared,
                ForeignKeys = true,
                Pooling = true
            }.ToString();

            try
            {
                DatabaseBootstrapper.EnsureCreated(connectionString);
                DatabaseHelper.Initialize(connectionString);
                DatabaseHelper.TestConnection();

                TestInventoryStatuses();
                TestAuthentication();
                TestCrudAndStockFlow();

                Console.WriteLine(
                    "PASS: " + _assertionCount + " assertions completed.");

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL: " + ex);
                return 1;
            }
            finally
            {
                DeleteTestDatabase(databasePath);
            }
        }

        private static void TestInventoryStatuses()
        {
            Assert(
                new InventoryItem
                {
                    Quantity = 0,
                    ReorderLevel = 5
                }.Status == "Out of Stock",
                "Zero quantity must be out of stock.");

            Assert(
                new InventoryItem
                {
                    Quantity = 3,
                    ReorderLevel = 5
                }.Status == "Low Stock",
                "Quantity below reorder level must be low stock.");

            Assert(
                new InventoryItem
                {
                    Quantity = 8,
                    ReorderLevel = 5
                }.Status == "In Stock",
                "Quantity above reorder level must be in stock.");
        }

        private static void TestAuthentication()
        {
            var authentication = new AuthenticationService();

            Assert(
                !authentication.Register("ab", "CorrectHorse123").IsSuccess,
                "A username shorter than three characters must fail.");
            Assert(
                !authentication.Register("bad user", "CorrectHorse123").IsSuccess,
                "A username containing spaces must fail.");
            Assert(
                !authentication.Register("valid.user", "short").IsSuccess,
                "A password shorter than eight characters must fail.");
            Assert(
                !authentication.Login(string.Empty, string.Empty).IsSuccess,
                "Blank login fields must fail.");

            AuthenticationResult registration =
                authentication.Register("test.user", "CorrectHorse123");

            Assert(registration.IsSuccess, registration.Message);
            Assert(
                !authentication.Register("test.user", "CorrectHorse123").IsSuccess,
                "A duplicate username must fail.");

            AuthenticationResult badLogin =
                authentication.Login("test.user", "wrong-password");

            Assert(!badLogin.IsSuccess, "An incorrect password must fail.");

            AuthenticationResult login =
                authentication.Login("test.user", "CorrectHorse123");

            Assert(login.IsSuccess, login.Message);
        }

        private static void TestCrudAndStockFlow()
        {
            var suppliers = new SupplierRepository();
            var materials = new MaterialRepository();
            var products = new ProductRepository();
            var orders = new OrderRepository();
            var statistics = new StatisticsRepository();

            var supplier = new Supplier
            {
                SupplierName = "පරීක්ෂණ සැපයුම්කරු",
                Phone = "0771234567",
                Address = "Colombo"
            };
            suppliers.Add(supplier);
            Assert(supplier.Id > 0, "Supplier identity was not assigned.");
            Assert(suppliers.Exists(supplier.Id), "Supplier existence check failed.");
            Assert(suppliers.GetCount() == 1, "Supplier count is incorrect.");

            supplier.Phone = "0717654321";
            supplier.Address = "Kandy";
            suppliers.Update(supplier);
            Supplier loadedSupplier = suppliers.GetById(supplier.Id);
            Assert(loadedSupplier.Phone == "0717654321", "Supplier phone update failed.");
            Assert(loadedSupplier.Address == "Kandy", "Supplier address update failed.");

            var material = new Material
            {
                MaterialName = "Test Material",
                Quantity = 12,
                Unit = "kg",
                ReorderLevel = 3,
                SupplierId = supplier.Id
            };
            materials.Add(material);
            Assert(material.Id > 0, "Material identity was not assigned.");
            Material loadedMaterial = materials.GetById(material.Id);
            Assert(loadedMaterial.Quantity == 12m, "Material quantity was not persisted.");
            Assert(loadedMaterial.SupplierId == supplier.Id, "Material supplier was not persisted.");

            material.Quantity = 2.5m;
            material.Unit = "bundle";
            material.ReorderLevel = 4;
            materials.Update(material);
            loadedMaterial = materials.GetById(material.Id);
            Assert(loadedMaterial.Quantity == 2.5m, "Material quantity update failed.");
            Assert(loadedMaterial.Unit == "bundle", "Material unit update failed.");
            Assert(loadedMaterial.ReorderLevel == 4, "Material reorder-level update failed.");

            var product = new Product
            {
                ProductName = "පරීක්ෂණ නිෂ්පාදනය",
                Description = "Preserved description",
                Category = "Test Category",
                Price = 25m,
                StockQuantity = 10
            };
            products.Add(product);

            Product loaded = products.GetById(product.Id);
            Assert(loaded.Category == "Test Category", "Category was not persisted.");
            Assert(loaded.StockQuantity == 10, "Initial stock is incorrect.");
            Assert(
                loaded.ProductName == "පරීක්ෂණ නිෂ්පාදනය",
                "Unicode product text was not preserved.");
            Assert(products.Exists(product.Id), "Product existence check failed.");
            Assert(products.Search("නිෂ්පාදනය").Count == 1, "Product name search failed.");
            Assert(products.Search("Preserved").Count == 1, "Product description search failed.");
            Assert(products.Search("missing").Count == 0, "Product search returned a false match.");
            Assert(products.GetProductsInStock().Count == 1, "In-stock product query failed.");

            product.ProductName = "Updated Product";
            product.Description = "Updated description";
            product.Category = "Updated Category";
            product.Price = 30m;
            product.StockQuantity = 10;
            products.Update(product);
            loaded = products.GetById(product.Id);
            Assert(loaded.ProductName == "Updated Product", "Product name update failed.");
            Assert(loaded.Description == "Updated description", "Product description update failed.");
            Assert(loaded.Category == "Updated Category", "Product category update failed.");
            Assert(loaded.Price == 30m, "Product price update failed.");

            bool insufficientStockRejected = false;
            try
            {
                orders.CreateOrder(
                    "Automated Customer",
                    product.Id,
                    11,
                    loaded.Price,
                    "Pending");
            }
            catch (InvalidOperationException)
            {
                insufficientStockRejected = true;
            }

            Assert(
                insufficientStockRejected,
                "An order exceeding available stock must fail.");
            Assert(
                products.GetById(product.Id).StockQuantity == 10,
                "A rejected order must not change stock.");

            int orderId = orders.CreateOrder(
                "Automated Customer",
                product.Id,
                2,
                loaded.Price,
                "Pending");

            Assert(
                products.GetById(product.Id).StockQuantity == 8,
                "Creating an active order must reserve stock.");
            OrderRecord createdOrder = orders.GetAll().Single();
            Assert(createdOrder.CustomerName == "Automated Customer", "Customer name was not persisted.");
            Assert(createdOrder.ProductId == product.Id, "Order product was not persisted.");
            Assert(createdOrder.Quantity == 2, "Order quantity was not persisted.");
            Assert(createdOrder.TotalAmount == 60m, "Order total is incorrect.");
            Assert(
                statistics.GetSummary().TotalRevenue == 0m,
                "Pending orders must not count as revenue.");

            DashboardSummary pendingDashboard = new DashboardRepository().GetSummary();
            Assert(pendingDashboard.TotalProducts == 1, "Dashboard product count is incorrect.");
            Assert(pendingDashboard.TotalOrders == 1, "Dashboard order count is incorrect.");
            Assert(pendingDashboard.TotalRevenue == 0m, "Dashboard pending revenue is incorrect.");
            Assert(pendingDashboard.LowStockCount == 1, "Dashboard low-stock count is incorrect.");
            Assert(
                new DashboardRepository().GetLowStockMaterials().Single().Status == "Low Stock",
                "Dashboard low-stock material status is incorrect.");
            Assert(
                new DashboardRepository().GetRecentOrders().Single().CustomerName == "Automated Customer",
                "Dashboard recent order is incorrect.");

            orders.UpdateOrder(
                orderId,
                "Automated Customer",
                product.Id,
                2,
                loaded.Price,
                "Cancelled");

            Assert(
                products.GetById(product.Id).StockQuantity == 10,
                "Cancelling an order must restore stock.");

            orders.UpdateOrder(
                orderId,
                "Automated Customer",
                product.Id,
                2,
                loaded.Price,
                "Completed");

            Assert(
                products.GetById(product.Id).StockQuantity == 8,
                "Reactivating an order must reserve stock.");

            StatisticsSummary summary = statistics.GetSummary();
            Assert(summary.TotalRevenue == 60m, "Completed revenue is incorrect.");
            Assert(summary.CompletedOrders == 1, "Completed order count is incorrect.");
            Assert(summary.TotalOrders == 1, "Statistics order count is incorrect.");
            Assert(
                statistics.GetLastSixMonthsRevenue().Count == 6,
                "Six-month revenue series must contain six entries.");
            Assert(
                statistics.GetLastSixMonthsRevenue().Sum(item => item.Revenue) == 60m,
                "Monthly revenue series is incorrect.");
            ProductRevenueRecord topProduct = statistics.GetTopProducts().Single();
            Assert(topProduct.ProductName == "Updated Product", "Top-product name is incorrect.");
            Assert(topProduct.Revenue == 60m, "Top-product revenue is incorrect.");

            DashboardSummary completedDashboard = new DashboardRepository().GetSummary();
            Assert(completedDashboard.TotalRevenue == 60m, "Dashboard completed revenue is incorrect.");

            orders.DeleteOrder(orderId);

            Assert(
                products.GetById(product.Id).StockQuantity == 10,
                "Deleting an active order must restore stock.");
            Assert(orders.GetAll().Count == 0, "Order deletion failed.");

            materials.Delete(material.Id);
            suppliers.Delete(supplier.Id);
            products.Delete(product.Id);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }

            _assertionCount++;
        }

        private static void DeleteTestDatabase(string databasePath)
        {
            try
            {
                SqliteConnection.ClearAllPools();

                foreach (string path in new[]
                {
                    databasePath,
                    databasePath + "-wal",
                    databasePath + "-shm"
                })
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
            }
            catch (Exception cleanupError)
            {
                Console.Error.WriteLine(
                    "Warning: test database cleanup failed: "
                    + cleanupError.Message);
            }
        }
    }
}
