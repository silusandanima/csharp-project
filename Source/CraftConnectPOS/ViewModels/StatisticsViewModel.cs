using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using CraftConnectPOS.Data;
using CraftConnectPOS.Models;

namespace CraftConnectPOS.ViewModels
{
    public class StatisticsViewModel : ViewModelBase
    {
        private readonly StatisticsRepository _repository =
            new StatisticsRepository();

        public StatisticsViewModel()
        {
            Metrics = new ObservableCollection<MetricCard>();
            Bars = new ObservableCollection<RevenueBar>();
            Breakdown = new ObservableCollection<ProductRevenueBreakdown>();

        }

        public ObservableCollection<MetricCard> Metrics { get; }

        public ObservableCollection<RevenueBar> Bars { get; }

        public ObservableCollection<ProductRevenueBreakdown> Breakdown { get; }

        public void Refresh()
        {
            LoadStatistics();
        }

        private void LoadStatistics()
        {
            try
            {
                StatisticsSummary summary = _repository.GetSummary();

                var monthlyRevenue =
                    _repository.GetLastSixMonthsRevenue();

                var topProducts =
                    _repository.GetTopProducts();

                decimal monthlyAverage = monthlyRevenue.Count == 0
                    ? 0m
                    : monthlyRevenue.Average(item => item.Revenue);

                decimal completionPercentage = summary.TotalOrders == 0
                    ? 0m
                    : Math.Round(
                        (decimal)summary.CompletedOrders
                        / summary.TotalOrders
                        * 100m,
                        1);

                ProductRevenueRecord topProduct =
                    topProducts.FirstOrDefault();

                LoadMetrics(
                    summary,
                    monthlyAverage,
                    completionPercentage,
                    topProduct);

                LoadRevenueBars(monthlyRevenue);

                LoadProductBreakdown(
                    topProducts,
                    summary.TotalRevenue);
            }
            catch (Exception ex)
            {
                LoadErrorState(ex.Message);
            }
        }

        private void LoadMetrics(
            StatisticsSummary summary,
            decimal monthlyAverage,
            decimal completionPercentage,
            ProductRevenueRecord topProduct)
        {
            Metrics.Clear();

            Metrics.Add(new MetricCard
            {
                Title = "Total Revenue",
                Value = FormatCurrency(summary.TotalRevenue),
                Note = summary.TotalOrders + " recorded order(s)"
            });

            Metrics.Add(new MetricCard
            {
                Title = "Monthly Avg",
                Value = FormatCurrency(monthlyAverage),
                Note = "Based on the last 6 months"
            });

            Metrics.Add(new MetricCard
            {
                Title = "Orders Complete",
                Value = summary.CompletedOrders.ToString(),
                Note = completionPercentage.ToString("0.#")
                    + "% completed"
            });

            Metrics.Add(new MetricCard
            {
                Title = "Top Product",
                Value = topProduct == null
                    ? "No sales yet"
                    : topProduct.ProductName,

                Note = topProduct == null
                    ? "Create an order to begin"
                    : FormatCurrency(topProduct.Revenue)
                        + " revenue"
            });
        }

        private void LoadRevenueBars(
            System.Collections.Generic.List<MonthlyRevenueRecord> monthlyRevenue)
        {
            Bars.Clear();

            decimal highestRevenue = monthlyRevenue.Count == 0
                ? 0m
                : monthlyRevenue.Max(item => item.Revenue);

            foreach (MonthlyRevenueRecord month in monthlyRevenue)
            {
                int barHeight = 0;

                if (highestRevenue > 0)
                {
                    barHeight = Math.Max(
                        12,
                        Convert.ToInt32(
                            Math.Round(
                                (month.Revenue / highestRevenue) * 172m,
                                0)));
                }

                string monthName = new DateTime(
                    month.Year,
                    month.MonthNumber,
                    1).ToString("MMM", CultureInfo.InvariantCulture);

                Bars.Add(new RevenueBar
                {
                    Month = monthName,
                    Amount = FormatCurrency(month.Revenue),
                    Height = barHeight
                });
            }
        }

        private void LoadProductBreakdown(
            System.Collections.Generic.List<ProductRevenueRecord> topProducts,
            decimal totalRevenue)
        {
            Breakdown.Clear();

            foreach (ProductRevenueRecord product in topProducts)
            {
                double percentage = totalRevenue <= 0
                    ? 0
                    : Convert.ToDouble(
                        Math.Round(
                            (product.Revenue / totalRevenue) * 100m,
                            1));

                Breakdown.Add(new ProductRevenueBreakdown
                {
                    Title = product.ProductName,
                    Value = FormatCurrency(product.Revenue),
                    Note = percentage.ToString("0.#")
                        + "% share",
                    Percentage = percentage
                });
            }
        }

        private void LoadErrorState(string errorMessage)
        {
            Metrics.Clear();
            Bars.Clear();
            Breakdown.Clear();

            Metrics.Add(new MetricCard
            {
                Title = "Statistics",
                Value = "Unable to load",
                Note = errorMessage
            });

            Metrics.Add(new MetricCard
            {
                Title = "Monthly Avg",
                Value = "Rs. 0.00",
                Note = "No data available"
            });

            Metrics.Add(new MetricCard
            {
                Title = "Orders Complete",
                Value = "0",
                Note = "No data available"
            });

            Metrics.Add(new MetricCard
            {
                Title = "Top Product",
                Value = "No data",
                Note = "No data available"
            });
        }

        private static string FormatCurrency(decimal amount)
        {
            return "Rs. "
                + amount.ToString(
                    "N2",
                    CultureInfo.InvariantCulture);
        }
    }
}
