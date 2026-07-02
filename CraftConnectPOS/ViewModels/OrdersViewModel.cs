using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using CraftConnectPOS.Commands;
using CraftConnectPOS.Models;
using CraftConnectPOS.Services;

namespace CraftConnectPOS.ViewModels
{
    public class OrdersViewModel : ViewModelBase
    {
        private readonly MockDataStore _dataStore;
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

        public OrdersViewModel(MockDataStore dataStore)
        {
            _dataStore = dataStore;
            Metrics = new ObservableCollection<MetricCard>();
            StatusOptions = new[] { "Pending", "Processing", "Ready to Ship", "Shipped", "Completed" };
            StatusFilters = new[] { "All statuses", "Pending", "Processing", "Ready to Ship", "Shipped", "Completed" };
            OrdersView = CollectionViewSource.GetDefaultView(_dataStore.Orders);
            OrdersView.Filter = FilterOrder;

            NewCommand = new RelayCommand(_ => StartNew());
            SaveCommand = new RelayCommand(_ => Save());
            DeleteCommand = new RelayCommand(_ => Delete(), _ => SelectedOrder != null);
            CancelCommand = new RelayCommand(_ => Cancel());

            RefreshMetrics();
        }

        public ObservableCollection<MetricCard> Metrics { get; }
        public ObservableCollection<ProductItem> ProductOptions
        {
            get { return _dataStore.Products; }
        }
        public ICollectionView OrdersView { get; }
        public IEnumerable<string> StatusOptions { get; }
        public IEnumerable<string> StatusFilters { get; }

        public CustomerOrder SelectedOrder
        {
            get { return _selectedOrder; }
            set
            {
                if (SetProperty(ref _selectedOrder, value) && value != null)
                {
                    LoadOrder(value);
                }

                CommandManager.InvalidateRequerySuggested();
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

        public ProductItem SelectedProduct
        {
            get { return _selectedProduct; }
            set
            {
                if (SetProperty(ref _selectedProduct, value))
                {
                    RecalculateTotal();
                }
            }
        }

        public string QuantityText
        {
            get { return _quantityText; }
            set
            {
                if (SetProperty(ref _quantityText, value))
                {
                    RecalculateTotal();
                }
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
                if (SetProperty(ref _searchText, value))
                {
                    OrdersView.Refresh();
                }
            }
        }

        public string SelectedStatusFilter
        {
            get { return _selectedStatusFilter; }
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
                {
                    OrdersView.Refresh();
                }
            }
        }

        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { SetProperty(ref _errorMessage, value); }
        }

        public ICommand NewCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand CancelCommand { get; }

        private bool FilterOrder(object item)
        {
            var order = item as CustomerOrder;
            if (order == null)
            {
                return false;
            }

            var query = (SearchText ?? string.Empty).Trim();
            var matchesSearch = query.Length == 0
                || Contains(order.OrderId, query)
                || Contains(order.Customer, query)
                || Contains(order.Product, query);
            var matchesStatus = SelectedStatusFilter == "All statuses"
                || order.Status == SelectedStatusFilter;

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

        private void Save()
        {
            int quantity;
            if (!Validate(out quantity))
            {
                return;
            }

            var total = SelectedProduct.UnitPrice * quantity;
            if (SelectedOrder == null)
            {
                _dataStore.Orders.Add(new CustomerOrder
                {
                    OrderId = OrderId.Trim(),
                    Customer = CustomerName.Trim(),
                    Product = SelectedProduct.Name,
                    Quantity = quantity,
                    Status = OrderStatus,
                    UnitPrice = SelectedProduct.UnitPrice,
                    Total = total
                });
            }
            else
            {
                SelectedOrder.OrderId = OrderId.Trim();
                SelectedOrder.Customer = CustomerName.Trim();
                SelectedOrder.Product = SelectedProduct.Name;
                SelectedOrder.Quantity = quantity;
                SelectedOrder.Status = OrderStatus;
                SelectedOrder.UnitPrice = SelectedProduct.UnitPrice;
                SelectedOrder.Total = total;
            }

            OrdersView.Refresh();
            RefreshMetrics();
            SelectedOrder = null;
            ClearForm();
        }

        private bool Validate(out int quantity)
        {
            quantity = 0;

            if (string.IsNullOrWhiteSpace(OrderId)
                || string.IsNullOrWhiteSpace(CustomerName)
                || SelectedProduct == null
                || string.IsNullOrWhiteSpace(OrderStatus))
            {
                ErrorMessage = "Complete all order fields and select a product.";
                return false;
            }

            if (!int.TryParse(QuantityText, out quantity) || quantity <= 0)
            {
                ErrorMessage = "Quantity must be a whole number greater than zero.";
                return false;
            }

            var duplicate = _dataStore.Orders.Any(item =>
                item != SelectedOrder
                && string.Equals(item.OrderId, OrderId.Trim(), StringComparison.OrdinalIgnoreCase));
            if (duplicate)
            {
                ErrorMessage = "An order with this ID already exists.";
                return false;
            }

            ErrorMessage = string.Empty;
            return true;
        }

        private void Delete()
        {
            if (SelectedOrder == null)
            {
                return;
            }

            _dataStore.Orders.Remove(SelectedOrder);
            SelectedOrder = null;
            ClearForm();
            RefreshMetrics();
            OrdersView.Refresh();
        }

        private void Cancel()
        {
            if (SelectedOrder != null)
            {
                LoadOrder(SelectedOrder);
                return;
            }

            ClearForm();
        }

        private void LoadOrder(CustomerOrder order)
        {
            OrderId = order.OrderId;
            CustomerName = order.Customer;
            QuantityText = order.Quantity.ToString();
            OrderStatus = order.Status;
            SelectedProduct = ProductOptions.FirstOrDefault(item =>
                string.Equals(item.Name, order.Product, StringComparison.OrdinalIgnoreCase));
            TotalText = "Rs. " + order.Total.ToString("N2", CultureInfo.InvariantCulture);
            ErrorMessage = string.Empty;
        }

        private void ClearForm()
        {
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
            if (SelectedProduct == null ||
                !int.TryParse(QuantityText, out quantity) ||
                quantity <= 0)
            {
                TotalText = "Rs. 0";
                return;
            }

            TotalText = "Rs. " +
                (SelectedProduct.UnitPrice * quantity).ToString("N2", CultureInfo.InvariantCulture);
        }

        private void RefreshMetrics()
        {
            Metrics.Clear();
            Metrics.Add(new MetricCard { Title = "Active Orders", Value = _dataStore.Orders.Count(item => item.Status != "Completed").ToString(), Note = "All active" });
            Metrics.Add(new MetricCard { Title = "Pending", Value = _dataStore.Orders.Count(item => item.Status == "Pending").ToString(), Note = "Awaiting action" });
            Metrics.Add(new MetricCard { Title = "Processing", Value = _dataStore.Orders.Count(item => item.Status == "Processing").ToString(), Note = "In workshop" });
            Metrics.Add(new MetricCard { Title = "Ready to Ship", Value = _dataStore.Orders.Count(item => item.Status == "Ready to Ship").ToString(), Note = "Packed orders" });
        }
    }
}
