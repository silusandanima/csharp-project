using System;
using System.Collections.Generic;
using System.Data;

namespace CraftConnectPOS.Data
{
    public class DashboardSummary
    {
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int LowStockCount { get; set; }
    }

    public class DashboardOrderRecord
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }

        public DashboardOrderRecord()
        {
            CustomerName = string.Empty;
            ProductName = string.Empty;
            Status = string.Empty;
        }
    }

    public class DashboardLowStockRecord
    {
        public string MaterialName { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; }
        public string Status { get; set; }

        public DashboardLowStockRecord()
        {
            MaterialName = string.Empty;
            Unit = string.Empty;
            Status = string.Empty;
        }
    }

    public class DashboardRepository
    {
        public DashboardSummary GetSummary()
        {
            const string query = @"
                SELECT
                    (SELECT COUNT(*)
                     FROM dbo.Products) AS TotalProducts,

                    (SELECT COUNT(*)
                     FROM dbo.Orders) AS TotalOrders,

                    (SELECT ISNULL(SUM(
                        CASE
                            WHEN ISNULL(Status, '') <> 'Cancelled'
                            THEN ISNULL(TotalAmount, 0)
                            ELSE 0
                        END
                    ), 0)
                     FROM dbo.Orders) AS TotalRevenue,

                    (SELECT COUNT(*)
                     FROM dbo.Materials
                     WHERE Quantity <= ISNULL(ReorderLevel, 0)) AS LowStockCount;";

            DataTable table = DatabaseHelper.ExecuteQuery(query);
            DataRow row = table.Rows[0];

            return new DashboardSummary
            {
                TotalProducts = Convert.ToInt32(row["TotalProducts"]),
                TotalOrders = Convert.ToInt32(row["TotalOrders"]),
                TotalRevenue = Convert.ToDecimal(row["TotalRevenue"]),
                LowStockCount = Convert.ToInt32(row["LowStockCount"])
            };
        }

        public List<DashboardOrderRecord> GetRecentOrders()
        {
            const string query = @"
                SELECT TOP (5)
                    o.OrderID,
                    ISNULL(c.CustomerName, 'Unknown customer') AS CustomerName,
                    ISNULL(p.ProductName, 'Unknown product') AS ProductName,
                    ISNULL(oi.Quantity, 0) AS Quantity,
                    ISNULL(o.Status, 'Pending') AS Status,
                    ISNULL(o.TotalAmount, 0) AS TotalAmount
                FROM dbo.Orders o
                INNER JOIN dbo.OrderItems oi
                    ON oi.OrderID = o.OrderID
                LEFT JOIN dbo.Customers c
                    ON c.CustomerID = o.CustomerID
                LEFT JOIN dbo.Products p
                    ON p.ProductID = oi.ProductID
                ORDER BY
                    o.OrderDate DESC,
                    o.OrderID DESC;";

            DataTable table = DatabaseHelper.ExecuteQuery(query);

            var orders = new List<DashboardOrderRecord>();

            foreach (DataRow row in table.Rows)
            {
                orders.Add(new DashboardOrderRecord
                {
                    OrderId = Convert.ToInt32(row["OrderID"]),
                    CustomerName = row["CustomerName"].ToString(),
                    ProductName = row["ProductName"].ToString(),
                    Quantity = Convert.ToInt32(row["Quantity"]),
                    Status = row["Status"].ToString(),
                    TotalAmount = Convert.ToDecimal(row["TotalAmount"])
                });
            }

            return orders;
        }

        public List<DashboardLowStockRecord> GetLowStockMaterials()
        {
            const string query = @"
                SELECT
                    MaterialName,
                    Quantity,
                    Unit,
                    CASE
                        WHEN Quantity <= 0 THEN 'Out of Stock'
                        ELSE 'Low Stock'
                    END AS Status
                FROM dbo.Materials
                WHERE Quantity <= ISNULL(ReorderLevel, 0)
                ORDER BY
                    Quantity ASC,
                    MaterialName ASC;";

            DataTable table = DatabaseHelper.ExecuteQuery(query);

            var materials = new List<DashboardLowStockRecord>();

            foreach (DataRow row in table.Rows)
            {
                materials.Add(new DashboardLowStockRecord
                {
                    MaterialName = row["MaterialName"].ToString(),
                    Quantity = Convert.ToDecimal(row["Quantity"]),
                    Unit = row["Unit"] == DBNull.Value
                        ? string.Empty
                        : row["Unit"].ToString(),

                    Status = row["Status"].ToString()
                });
            }

            return materials;
        }
    }
}