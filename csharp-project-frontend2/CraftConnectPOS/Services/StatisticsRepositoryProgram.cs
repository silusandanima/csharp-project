using System;
using System.Collections.Generic;
using System.Data;

namespace CraftConnectPOS.Data
{
    public class StatisticsSummary
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
    }

    public class MonthlyRevenueRecord
    {
        public int Year { get; set; }
        public int MonthNumber { get; set; }
        public decimal Revenue { get; set; }
    }

    public class ProductRevenueBreakdown
    {
        public string Title { get; set; }
        public string Value { get; set; }
        public string Note { get; set; }
        public double Percentage { get; set; }

        public ProductRevenueBreakdown()
        {
            Title = string.Empty;
            Value = string.Empty;
            Note = string.Empty;
        }
    }

    public class ProductRevenueRecord
    {
        public string ProductName { get; set; }
        public decimal Revenue { get; set; }

        public ProductRevenueRecord()
        {
            ProductName = string.Empty;
        }
    }

    public class StatisticsRepository
    {
        public StatisticsSummary GetSummary()
        {
            const string query = @"
                SELECT
                    ISNULL(SUM(
                        CASE
                            WHEN ISNULL(Status, '') <> 'Cancelled'
                            THEN ISNULL(TotalAmount, 0)
                            ELSE 0
                        END
                    ), 0) AS TotalRevenue,

                    COUNT(*) AS TotalOrders,

                    ISNULL(SUM(
                        CASE
                            WHEN Status = 'Completed'
                            THEN 1
                            ELSE 0
                        END
                    ), 0) AS CompletedOrders
                FROM dbo.Orders;";

            DataTable table = DatabaseHelper.ExecuteQuery(query);
            DataRow row = table.Rows[0];

            return new StatisticsSummary
            {
                TotalRevenue = Convert.ToDecimal(row["TotalRevenue"]),
                TotalOrders = Convert.ToInt32(row["TotalOrders"]),
                CompletedOrders = Convert.ToInt32(row["CompletedOrders"])
            };
        }

        public List<MonthlyRevenueRecord> GetLastSixMonthsRevenue()
        {
            const string query = @"
                WITH MonthOffsets AS
                (
                    SELECT 0 AS OffsetValue
                    UNION ALL SELECT 1
                    UNION ALL SELECT 2
                    UNION ALL SELECT 3
                    UNION ALL SELECT 4
                    UNION ALL SELECT 5
                ),
                Months AS
                (
                    SELECT DATEADD(
                        MONTH,
                        -OffsetValue,
                        DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)
                    ) AS MonthStart
                    FROM MonthOffsets
                )
                SELECT
                    YEAR(m.MonthStart) AS [Year],
                    MONTH(m.MonthStart) AS MonthNumber,

                    ISNULL(SUM(
                        CASE
                            WHEN ISNULL(o.Status, '') <> 'Cancelled'
                            THEN ISNULL(o.TotalAmount, 0)
                            ELSE 0
                        END
                    ), 0) AS Revenue
                FROM Months m
                LEFT JOIN dbo.Orders o
                    ON o.OrderDate >= m.MonthStart
                    AND o.OrderDate < DATEADD(MONTH, 1, m.MonthStart)
                GROUP BY
                    m.MonthStart
                ORDER BY
                    m.MonthStart;";

            DataTable table = DatabaseHelper.ExecuteQuery(query);
            var results = new List<MonthlyRevenueRecord>();

            foreach (DataRow row in table.Rows)
            {
                results.Add(new MonthlyRevenueRecord
                {
                    Year = Convert.ToInt32(row["Year"]),
                    MonthNumber = Convert.ToInt32(row["MonthNumber"]),
                    Revenue = Convert.ToDecimal(row["Revenue"])
                });
            }

            return results;
        }

        public List<ProductRevenueRecord> GetTopProducts()
        {
            const string query = @"
                SELECT TOP (4)
                    ISNULL(p.ProductName, 'Unknown product') AS ProductName,

                    ISNULL(SUM(
                        oi.Quantity * oi.UnitPrice
                    ), 0) AS Revenue
                FROM dbo.OrderItems oi
                INNER JOIN dbo.Orders o
                    ON o.OrderID = oi.OrderID
                LEFT JOIN dbo.Products p
                    ON p.ProductID = oi.ProductID
                WHERE ISNULL(o.Status, '') <> 'Cancelled'
                GROUP BY
                    p.ProductName
                ORDER BY
                    Revenue DESC;";

            DataTable table = DatabaseHelper.ExecuteQuery(query);
            var results = new List<ProductRevenueRecord>();

            foreach (DataRow row in table.Rows)
            {
                results.Add(new ProductRevenueRecord
                {
                    ProductName = row["ProductName"].ToString(),
                    Revenue = Convert.ToDecimal(row["Revenue"])
                });
            }

            return results;
        }
    }
}