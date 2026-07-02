using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Data;
using CraftConnectPOS.Models;
using CraftConnectPOS.Services;

namespace CraftConnectPOS.ViewModels
{
    public class DashboardViewModel : ViewModelBase, IRefreshable
    {
        private readonly IReportingService _reportingService;
        private string _errorMessage;
        private string _orderSearchText;

        public DashboardViewModel(IReportingService reportingService)
        {
            _reportingService = reportingService ?? throw new ArgumentNullException(nameof(reportingService));
            Metrics = new ObservableCollection<MetricCard>();
            RecentOrders = new ObservableCollection<OrderRow>();
            LowStockItems = new ObservableCollection<InventoryRow>();
            RecentOrdersView = CollectionViewSource.GetDefaultView(RecentOrders);
            RecentOrdersView.Filter = FilterOrder;
        }

        public ObservableCollection<MetricCard> Metrics { get; }
        public ObservableCollection<OrderRow> RecentOrders { get; }
        public ObservableCollection<InventoryRow> LowStockItems { get; }
        public ICollectionView RecentOrdersView { get; }

        public string OrderSearchText
        {
            get { return _orderSearchText; }
            set
            {
                if (SetProperty(ref _orderSearchText, value))
                {
                    RecentOrdersView.Refresh();
                }
            }
        }

        public string ErrorMessage
        {
            get { return _errorMessage; }
            private set { SetProperty(ref _errorMessage, value); }
        }

        public async Task RefreshAsync()
        {
            try
            {
                var data = await _reportingService.GetDashboardAsync();
                Replace(Metrics, data.Metrics);
                Replace(RecentOrders, data.RecentOrders);
                Replace(LowStockItems, data.LowStockItems);
                RecentOrdersView.Refresh();
                ErrorMessage = string.Empty;
            }
            catch (Exception exception)
            {
                ErrorMessage = "Dashboard data could not be loaded. " + exception.Message;
            }
        }

        private bool FilterOrder(object item)
        {
            var order = item as OrderRow;
            if (order == null)
            {
                return false;
            }

            var query = (OrderSearchText ?? string.Empty).Trim();
            return query.Length == 0 ||
                Contains(order.OrderId, query) ||
                Contains(order.Customer, query) ||
                Contains(order.Product, query) ||
                Contains(order.Status, query);
        }

        private static bool Contains(string value, string query)
        {
            return (value ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void Replace<T>(ObservableCollection<T> target, System.Collections.Generic.IEnumerable<T> source)
        {
            target.Clear();
            foreach (var item in source)
            {
                target.Add(item);
            }
        }
    }
}
