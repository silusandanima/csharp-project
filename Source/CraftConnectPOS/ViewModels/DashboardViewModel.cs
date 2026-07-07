using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using CraftConnectPOS.Data;
using CraftConnectPOS.Models;

namespace CraftConnectPOS.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly DashboardRepository _repository =
            new DashboardRepository();

        private string _searchText;

        public DashboardViewModel()
        {
            Metrics = new ObservableCollection<MetricCard>();
            RecentOrders = new ObservableCollection<OrderRow>();
            LowStockItems = new ObservableCollection<InventoryRow>();

            RecentOrdersView =
                CollectionViewSource.GetDefaultView(RecentOrders);

            RecentOrdersView.Filter = MatchesSearch;

        }

        public ObservableCollection<MetricCard> Metrics { get; }

        public ObservableCollection<OrderRow> RecentOrders { get; }

        public ICollectionView RecentOrdersView { get; }

        public ObservableCollection<InventoryRow> LowStockItems { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    RecentOrdersView.Refresh();
                }
            }
        }

        public void Refresh()
        {
            LoadDashboard();
        }

        private void LoadDashboard()
        {
            try
            {
                DashboardSummary summary = _repository.GetSummary();

                var recentOrders = _repository.GetRecentOrders();
                var lowStockMaterials = _repository.GetLowStockMaterials();

                LoadMetrics(summary);
                LoadRecentOrders(recentOrders);
                LoadLowStockMaterials(lowStockMaterials);

                RecentOrdersView.Refresh();
            }
            catch (Exception ex)
            {
                LoadErrorState(ex.Message);
            }
        }

        private void LoadMetrics(DashboardSummary summary)
        {
            Metrics.Clear();

            Metrics.Add(new MetricCard
            {
                Title = "Products",
                Value = summary.TotalProducts.ToString(),
                Note = "Products in catalog"
            });

            Metrics.Add(new MetricCard
            {
                Title = "Orders",
                Value = summary.TotalOrders.ToString(),
                Note = "Recorded orders"
            });

            Metrics.Add(new MetricCard
            {
                Title = "Revenue",
                Value = FormatCurrency(summary.TotalRevenue),
                Note = "Completed orders only"
            });

            Metrics.Add(new MetricCard
            {
                Title = "Low Stock",
                Value = summary.LowStockCount.ToString(),
                Note = "At or below reorder level"
            });
        }

        private void LoadRecentOrders(
            System.Collections.Generic.List<DashboardOrderRecord> orders)
        {
            RecentOrders.Clear();

            foreach (DashboardOrderRecord order in orders)
            {
                RecentOrders.Add(new OrderRow
                {
                    OrderId = order.OrderId.ToString(),
                    Customer = order.CustomerName,
                    Product = order.ProductName,
                    Quantity = order.Quantity.ToString(),
                    Status = order.Status,
                    Total = FormatCurrency(order.TotalAmount)
                });
            }
        }

        private void LoadLowStockMaterials(
            System.Collections.Generic.List<DashboardLowStockRecord> materials)
        {
            LowStockItems.Clear();

            foreach (DashboardLowStockRecord material in materials)
            {
                string unit = string.IsNullOrWhiteSpace(material.Unit)
                    ? "units"
                    : material.Unit;

                LowStockItems.Add(new InventoryRow
                {
                    Material = material.MaterialName,
                    Quantity = material.Quantity.ToString("0.##")
                        + " "
                        + unit
                        + " remaining",

                    Status = material.Status
                });
            }
        }

        private bool MatchesSearch(object item)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return true;
            }

            OrderRow order = item as OrderRow;

            if (order == null)
            {
                return false;
            }

            string search = SearchText.Trim();

            return Contains(order.OrderId, search)
                || Contains(order.Customer, search)
                || Contains(order.Product, search)
                || Contains(order.Status, search);
        }

        private static bool Contains(string value, string search)
        {
            return !string.IsNullOrEmpty(value)
                && value.IndexOf(
                    search,
                    StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void LoadErrorState(string errorMessage)
        {
            Metrics.Clear();
            RecentOrders.Clear();
            LowStockItems.Clear();

            Metrics.Add(new MetricCard
            {
                Title = "Products",
                Value = "-",
                Note = "Dashboard could not load"
            });

            Metrics.Add(new MetricCard
            {
                Title = "Orders",
                Value = "-",
                Note = "Dashboard could not load"
            });

            Metrics.Add(new MetricCard
            {
                Title = "Revenue",
                Value = "-",
                Note = "Dashboard could not load"
            });

            Metrics.Add(new MetricCard
            {
                Title = "Low Stock",
                Value = "-",
                Note = errorMessage
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
