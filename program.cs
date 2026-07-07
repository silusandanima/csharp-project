using System;
using System.Collections.Generic;
using System.Linq;

namespace CraftConnect.Logic
{
    public class Order
    {
        public int OrderID { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Category { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class MonthlyRevenue
    {
        public int Year { get; set; }
        public int MonthNumber { get; set; }
        public string MonthName { get; set; }
        public decimal Revenue { get; set; }
    }

    public class CategoryRevenue
    {
        public string Category { get; set; }
        public decimal Revenue { get; set; }
        public decimal SharePercentage { get; set; }
    }

    public class RevenueStatistics
    {
        public decimal TotalRevenue { get; set; }
        public decimal MonthlyAverage { get; set; }
        public int CompletedOrders { get; set; }
        public decimal CompletedPercentage { get; set; }
        public string TopCategory { get; set; }
        public decimal TopCategoryShare { get; set; }
        public List<MonthlyRevenue> MonthlyRevenue { get; set; }
        public List<CategoryRevenue> CategoryRevenue { get; set; }
    }

    public class RevenueService
    {
        public RevenueStatistics CalculateStatistics(List<Order> orders)
        {
            if (orders == null || orders.Count == 0)
            {
                return new RevenueStatistics
                {
                    TotalRevenue = 0,
                    MonthlyAverage = 0,
                    CompletedOrders = 0,
                    CompletedPercentage = 0,
                    TopCategory = "N/A",
                    TopCategoryShare = 0,
                    MonthlyRevenue = new List<MonthlyRevenue>(),
                    CategoryRevenue = new List<CategoryRevenue>()
                };
            }

            decimal totalRevenue = orders.Sum(o => o.TotalAmount);
            int completedOrders = orders.Count(o => o.IsCompleted);

            decimal completedPercentage = Math.Round(
                ((decimal)completedOrders / orders.Count) * 100,
                1
            );

            List<MonthlyRevenue> monthlyRevenue = orders
                .GroupBy(o => new
                {
                    o.OrderDate.Year,
                    o.OrderDate.Month
                })
                .Select(g => new MonthlyRevenue
                {
                    Year = g.Key.Year,
                    MonthNumber = g.Key.Month,
                    MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM"),
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.MonthNumber)
                .ToList();

            decimal monthlyAverage = Math.Round(
                monthlyRevenue.Average(x => x.Revenue),
                0
            );

            List<CategoryRevenue> categoryRevenue = orders
                .GroupBy(o => o.Category)
                .Select(g => new CategoryRevenue
                {
                    Category = g.Key,
                    Revenue = g.Sum(o => o.TotalAmount),
                    SharePercentage = totalRevenue == 0
                        ? 0
                        : Math.Round((g.Sum(o => o.TotalAmount) / totalRevenue) * 100, 0)
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            CategoryRevenue topCategory = categoryRevenue.FirstOrDefault();

            return new RevenueStatistics
            {
                TotalRevenue = totalRevenue,
                MonthlyAverage = monthlyAverage,
                CompletedOrders = completedOrders,
                CompletedPercentage = completedPercentage,
                TopCategory = topCategory?.Category ?? "N/A",
                TopCategoryShare = topCategory?.SharePercentage ?? 0,
                MonthlyRevenue = monthlyRevenue,
                CategoryRevenue = categoryRevenue
            };
        }
    }
}