using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using CraftConnectPOS.Commands;
using CraftConnectPOS.Models;
using CraftConnectPOS.Services;

namespace CraftConnectPOS.ViewModels
{
    public class OrdersViewModel : ViewModelBase, IRefreshable
    {
        private readonly IOrderRepository _orders;
        private readonly IProductRepository _products;
        private readonly ObservableCollection<CustomerOrder> _orderItems;
        private CustomerOrder _selectedOrder;
        private ProductItem _selectedProduct;
        private string _orderId;
        private string _customerName;
        private string _quantityText;
        private string _orderStatus = "Pending";
        private string _totalText;
        private string _searchText;
        private string _selectedStatusFilter = "All statuses";
        private string _errorMessage;
        private bool _isBusy;
        private long _loadedProductId;
        private decimal _loadedUnitPrice;
        private DateTime _loadedOrderDate;

        public OrdersViewModel(IOrderRepository orders, IProductRepository products)
        {
            _orders = orders ?? throw new ArgumentNullException(nameof(orders));
            _products = products ?? throw new ArgumentNullException(nameof(products));
            _orderItems = new ObservableCollection<CustomerOrder>();
            ProductOptions = new ObservableCollection<ProductItem>();
            Metrics = new ObservableCollection<MetricCard>();
            StatusOptions = new[] { "Pending", "Processing", "Ready to Ship", "Shipped", "Completed" };
            StatusFilters = new[] { "All statuses", "Pending", "Processing", "Ready to Ship", "Shipped", "Completed" };
            OrdersView = CollectionViewSource.GetDefaultView(_orderItems);
            OrdersView.Filter = FilterOrder;
            NewCommand = new RelayCommand(_ => StartNew(), _ => !IsBusy);
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => !IsBusy);
            DeleteCommand = new RelayCommand(async _ => await DeleteAsync(), _ => !IsBusy && SelectedOrder != null);
            CancelCommand = new RelayCommand(_ => Cancel(), _ => !IsBusy);
        }

        public ObservableCollection<MetricCard> Metrics { get; }
        public ObservableCollection<ProductItem> ProductOptions { get; }
        public ICollectionView OrdersView { get; }
        public IEnumerable<string> StatusOptions { get; }
        public IEnumerable<string> StatusFilters { get; }

        public CustomerOrder SelectedOrder
        {
            get { return _selectedOrder; }
            set
            {
                if (SetProperty(ref _selectedOrder, value) && value != null) LoadOrder(value);
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ProductItem SelectedProduct
        {
            get { return _selectedProduct; }
            set
            {
                if (SetProperty(ref _selectedProduct, value)) RecalculateTotal();
            }
        }

        public string OrderId
        {
            get { return _orderId; }
            set { SetProperty(ref _orderId, value); }
        }

        public string CustomerName
        {
            get { return _customerName; }
            set { SetProperty(ref _customerName, value); }
        }

        public string QuantityText
        {
            get { return _quantityText; }
            set
            {
                if (SetProperty(ref _quantityText, value)) RecalculateTotal();
            }
        }

        public string OrderStatus
        {
            get { return _orderStatus; }
            set { SetProperty(ref _orderStatus, value); }
        }

        public string TotalText
        {
            get { return _totalText; }
            private set { SetProperty(ref _totalText, value); }
        }

        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (SetProperty(ref _searchText, value)) OrdersView.Refresh();
            }
        }

        public string SelectedStatusFilter
        {
            get { return _selectedStatusFilter; }
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value)) OrdersView.Refresh();
            }
        }

        public string ErrorMessage
        {
            get { return _errorMessage; }
            private set { SetProperty(ref _errorMessage, value); }
        }

        public bool IsBusy
        {
            get { return _isBusy; }
            private set
            {
                if (SetProperty(ref _isBusy, value)) CommandManager.InvalidateRequerySuggested();
            }
        }

        public ICommand NewCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand CancelCommand { get; }

        public async Task RefreshAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                Replace(ProductOptions, await _products.GetProductsAsync());
                Replace(_orderItems, await _orders.GetOrdersAsync());
                OrdersView.Refresh();
                RefreshMetrics();
                ErrorMessage = string.Empty;
            }
            catch (Exception exception)
            {
                ErrorMessage = "Orders could not be loaded. " + exception.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SaveAsync()
        {
            int quantity;
            if (!Validate(out quantity)) return;
            var unitPrice = SelectedOrder != null && SelectedProduct.Id == _loadedProductId
                ? _loadedUnitPrice
                : SelectedProduct.UnitPrice;
            IsBusy = true;
            var succeeded = false;
            try
            {
                var order = new CustomerOrder
                {
                    Id = SelectedOrder == null ? 0 : SelectedOrder.Id,
                    OrderId = OrderId.Trim(),
                    Customer = CustomerName.Trim(),
                    ProductId = SelectedProduct.Id,
                    Product = SelectedProduct.Name,
                    Quantity = quantity,
                    Status = OrderStatus,
                    UnitPrice = unitPrice,
                    Total = unitPrice * quantity,
                    OrderDate = SelectedOrder == null ? DateTime.UtcNow : _loadedOrderDate
                };
                if (order.Id == 0) await _orders.AddOrderAsync(order);
                else await _orders.UpdateOrderAsync(order);
                succeeded = true;
                SelectedOrder = null;
                ClearForm();
            }
            catch (Exception exception)
            {
                ErrorMessage = exception.Message;
            }
            finally
            {
                IsBusy = false;
            }
            if (succeeded) await RefreshAsync();
        }

        private async Task DeleteAsync()
        {
            if (SelectedOrder == null) return;
            IsBusy = true;
            var succeeded = false;
            try
            {
                await _orders.DeleteOrderAsync(SelectedOrder.Id);
                succeeded = true;
                SelectedOrder = null;
                ClearForm();
            }
            catch (Exception exception)
            {
                ErrorMessage = exception.Message;
            }
            finally
            {
                IsBusy = false;
            }
            if (succeeded) await RefreshAsync();
        }

        private bool Validate(out int quantity)
        {
            quantity = 0;
            if (string.IsNullOrWhiteSpace(OrderId) ||
                string.IsNullOrWhiteSpace(CustomerName) ||
                SelectedProduct == null ||
                string.IsNullOrWhiteSpace(OrderStatus))
            {
                ErrorMessage = "Complete all order fields and select a product.";
                return false;
            }
            if (!int.TryParse(QuantityText, out quantity) || quantity <= 0)
            {
                ErrorMessage = "Quantity must be a whole number greater than zero.";
                return false;
            }
            ErrorMessage = string.Empty;
            return true;
        }

        private bool FilterOrder(object item)
        {
            var order = item as CustomerOrder;
            if (order == null) return false;
            var query = (SearchText ?? string.Empty).Trim();
            var matchesSearch = query.Length == 0 ||
                Contains(order.OrderId, query) ||
                Contains(order.Customer, query) ||
                Contains(order.Product, query);
            var matchesStatus = SelectedStatusFilter == "All statuses" ||
                order.Status == SelectedStatusFilter;
            return matchesSearch && matchesStatus;
        }

        private static bool Contains(string value, string query)
        {
            return (value ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void StartNew()
        {
            SelectedOrder = null;
            ClearForm();
            OrderStatus = "Pending";
        }

        private void Cancel()
        {
            if (SelectedOrder != null) LoadOrder(SelectedOrder);
            else ClearForm();
        }

        private void LoadOrder(CustomerOrder order)
        {
            _loadedProductId = order.ProductId;
            _loadedUnitPrice = order.UnitPrice;
            _loadedOrderDate = order.OrderDate;
            OrderId = order.OrderId;
            CustomerName = order.Customer;
            QuantityText = order.Quantity.ToString(CultureInfo.InvariantCulture);
            OrderStatus = order.Status;
            SelectedProduct = ProductOptions.FirstOrDefault(item => item.Id == order.ProductId);
            TotalText = "Rs. " + order.Total.ToString("N2", CultureInfo.InvariantCulture);
            ErrorMessage = string.Empty;
        }

        private void ClearForm()
        {
            _loadedProductId = 0;
            _loadedUnitPrice = 0;
            _loadedOrderDate = DateTime.UtcNow;
            OrderId = string.Empty;
            CustomerName = string.Empty;
            QuantityText = string.Empty;
            OrderStatus = "Pending";
            SelectedProduct = null;
            TotalText = "Rs. 0";
            ErrorMessage = string.Empty;
        }

        private void RecalculateTotal()
        {
            int quantity;
            if (SelectedProduct == null || !int.TryParse(QuantityText, out quantity) || quantity <= 0)
            {
                TotalText = "Rs. 0";
                return;
            }
            var unitPrice = SelectedOrder != null && SelectedProduct.Id == _loadedProductId
                ? _loadedUnitPrice
                : SelectedProduct.UnitPrice;
            TotalText = "Rs. " + (unitPrice * quantity).ToString("N2", CultureInfo.InvariantCulture);
        }

        private void RefreshMetrics()
        {
            Metrics.Clear();
            Metrics.Add(new MetricCard
            {
                Title = "Active Orders",
                Value = _orderItems.Count(item =>
                    item.Status == "Pending" ||
                    item.Status == "Processing" ||
                    item.Status == "Ready to Ship").ToString(),
                Note = "Awaiting completion"
            });
            Metrics.Add(new MetricCard { Title = "Pending", Value = _orderItems.Count(item => item.Status == "Pending").ToString(), Note = "Awaiting action" });
            Metrics.Add(new MetricCard { Title = "Processing", Value = _orderItems.Count(item => item.Status == "Processing").ToString(), Note = "In workshop" });
            Metrics.Add(new MetricCard { Title = "Ready to Ship", Value = _orderItems.Count(item => item.Status == "Ready to Ship").ToString(), Note = "Packed orders" });
        }

        private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
        {
            target.Clear();
            foreach (var item in source) target.Add(item);
        }
    }
}
