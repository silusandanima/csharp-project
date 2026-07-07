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
                    IFNULL(SUM(
                        CASE
                            WHEN Status = 'Completed'
                            THEN IFNULL(TotalAmount, 0)
                            ELSE 0
                        END
                    ), 0) AS TotalRevenue,

                    COUNT(*) AS TotalOrders,

                    IFNULL(SUM(
                        CASE
                            WHEN Status = 'Completed'
                            THEN 1
                            ELSE 0
                        END
                    ), 0) AS CompletedOrders
                FROM Orders;";

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
                WITH RECURSIVE MonthOffsets(OffsetValue) AS
                (
                    VALUES(0)
                    UNION ALL
                    SELECT OffsetValue + 1
                    FROM MonthOffsets
                    WHERE OffsetValue < 5
                ),
                Months AS
                (
                    SELECT date(
                        'now',
                        'start of month',
                        printf('-%d months', OffsetValue)
                    ) AS MonthStart
                    FROM MonthOffsets
                )
                SELECT
                    CAST(strftime('%Y', m.MonthStart) AS INTEGER) AS Year,
                    CAST(strftime('%m', m.MonthStart) AS INTEGER) AS MonthNumber,

                    IFNULL(SUM(
                        CASE
                            WHEN o.Status = 'Completed'
                            THEN IFNULL(o.TotalAmount, 0)
                            ELSE 0
                        END
                    ), 0) AS Revenue
                FROM Months m
                LEFT JOIN Orders o
                    ON o.OrderDate >= m.MonthStart
                    AND o.OrderDate < date(m.MonthStart, '+1 month')
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
                SELECT
                    IFNULL(p.ProductName, 'Unknown product') AS ProductName,

                    IFNULL(SUM(
                        oi.Quantity * oi.UnitPrice
                    ), 0) AS Revenue
                FROM OrderItems oi
                INNER JOIN Orders o
                    ON o.OrderID = oi.OrderID
                LEFT JOIN Products p
                    ON p.ProductID = oi.ProductID
                WHERE o.Status = 'Completed'
                GROUP BY
                    oi.ProductID,
                    p.ProductName
                ORDER BY
                    Revenue DESC
                LIMIT 4;";

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
