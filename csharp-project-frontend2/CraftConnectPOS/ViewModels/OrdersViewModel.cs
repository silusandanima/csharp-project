using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using CraftConnectPOS.Commands;
using CraftConnectPOS.Data;
using CraftConnectPOS.Models;
using CraftConnectPOS.Services;

namespace CraftConnectPOS.ViewModels
{
    public class OrdersViewModel : ViewModelBase
    {
        private readonly OrderRepository _orderRepository =
            new OrderRepository();

        private readonly ProductRepository _productRepository =
            new ProductRepository();

        private readonly ObservableCollection<CustomerOrder> _orders =
            new ObservableCollection<CustomerOrder>();

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

        // Keep MockDataStore in the constructor so existing navigation
        // code does not break. Orders now load from SQL Server instead.
        public OrdersViewModel(MockDataStore dataStore)
        {
            if (dataStore == null)
            {
                throw new ArgumentNullException(nameof(dataStore));
            }

            Metrics = new ObservableCollection<MetricCard>();

            ProductOptions = new ObservableCollection<ProductItem>();

            StatusOptions = new[]
            {
                "Pending",
                "Processing",
                "Ready to Ship",
                "Shipped",
                "Completed"
            };

            StatusFilters = new[]
            {
                "All statuses",
                "Pending",
                "Processing",
                "Ready to Ship",
                "Shipped",
                "Completed"
            };

            OrdersView = CollectionViewSource.GetDefaultView(_orders);
            OrdersView.Filter = FilterOrder;

            NewCommand = new RelayCommand(_ => StartNew());
            SaveCommand = new RelayCommand(_ => Save());
            DeleteCommand = new RelayCommand(
                _ => Delete(),
                _ => SelectedOrder != null);

            CancelCommand = new RelayCommand(_ => Cancel());

            LoadData();
            ClearForm();
        }

        public ObservableCollection<MetricCard> Metrics { get; }

        public ObservableCollection<ProductItem> ProductOptions { get; }

        public ICollectionView OrdersView { get; }

        public IEnumerable<string> StatusOptions { get; }

        public IEnumerable<string> StatusFilters { get; }

        public CustomerOrder SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                if (SetProperty(ref _selectedOrder, value))
                {
                    if (value != null)
                    {
                        LoadOrder(value);
                    }

                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string OrderId
        {
            get => _orderId;
            set
            {
                if (SetProperty(ref _orderId, value))
                {
                    ClearError();
                }
            }
        }

        public string CustomerName
        {
            get => _customerName;
            set
            {
                if (SetProperty(ref _customerName, value))
                {
                    ClearError();
                }
            }
        }

        public ProductItem SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (SetProperty(ref _selectedProduct, value))
                {
                    ClearError();
                    RecalculateTotal();
                }
            }
        }

        public string QuantityText
        {
            get => _quantityText;
            set
            {
                if (SetProperty(ref _quantityText, value))
                {
                    ClearError();
                    RecalculateTotal();
                }
            }
        }

        public string OrderStatus
        {
            get => _orderStatus;
            set
            {
                if (SetProperty(ref _orderStatus, value))
                {
                    ClearError();
                }
            }
        }

        public string TotalText
        {
            get => _totalText;
            private set => SetProperty(ref _totalText, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    RefreshViewAndSelection();
                }
            }
        }

        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
                {
                    RefreshViewAndSelection();
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        public ICommand NewCommand { get; }

        public ICommand SaveCommand { get; }

        public ICommand DeleteCommand { get; }

        public ICommand CancelCommand { get; }

        public void Refresh()
        {
            LoadData();
            SelectedOrder = null;
            ClearForm();
        }

        private void LoadData()
        {
            try
            {
                ProductOptions.Clear();

                foreach (Product product in _productRepository.GetAll())
                {
                    ProductOptions.Add(new ProductItem
                    {
                        Id = product.Id,
                        Code = product.Id.ToString(),
                        Name = product.ProductName,
                        UnitPrice = product.Price
                    });
                }

                _orders.Clear();

                foreach (OrderRecord order in _orderRepository.GetAll())
                {
                    _orders.Add(new CustomerOrder
                    {
                        Id = order.OrderId,
                        ProductId = order.ProductId,
                        OrderId = order.OrderId.ToString(),
                        Customer = order.CustomerName,
                        Product = order.ProductName,
                        Quantity = order.Quantity,
                        Status = order.Status,
                        UnitPrice = order.UnitPrice,
                        Total = order.TotalAmount
                    });
                }

                OrdersView.Refresh();
                RefreshMetrics();
                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = "Could not load orders: " + ex.Message;
            }
        }

        private bool FilterOrder(object item)
        {
            CustomerOrder order = item as CustomerOrder;

            if (order == null)
            {
                return false;
            }

            string query = (SearchText ?? string.Empty).Trim();

            bool matchesSearch =
                query.Length == 0
                || Contains(order.OrderId, query)
                || Contains(order.Customer, query)
                || Contains(order.Product, query);

            bool matchesStatus =
                SelectedStatusFilter == "All statuses"
                || string.Equals(
                    order.Status,
                    SelectedStatusFilter,
                    StringComparison.OrdinalIgnoreCase);

            return matchesSearch && matchesStatus;
        }

        private static bool Contains(string value, string query)
        {
            return (value ?? string.Empty)
                .IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void StartNew()
        {
            SelectedOrder = null;
            ClearForm();
        }

        private void Save()
        {
            int quantity;

            if (!Validate(out quantity))
            {
                return;
            }

            try
            {
                if (SelectedOrder == null)
                {
                    _orderRepository.CreateOrder(
                        CustomerName.Trim(),
                        SelectedProduct.Id,
                        quantity,
                        SelectedProduct.UnitPrice,
                        OrderStatus);
                }
                else
                {
                    _orderRepository.UpdateOrder(
                        SelectedOrder.Id,
                        CustomerName.Trim(),
                        SelectedProduct.Id,
                        quantity,
                        SelectedProduct.UnitPrice,
                        OrderStatus);
                }

                LoadData();

                SelectedOrder = null;
                ClearForm();

                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = "Could not save order: " + ex.Message;
            }
        }

        private bool Validate(out int quantity)
        {
            quantity = 0;

            if (string.IsNullOrWhiteSpace(CustomerName)
                || SelectedProduct == null
                || string.IsNullOrWhiteSpace(OrderStatus))
            {
                ErrorMessage =
                    "Enter a customer name, select a product, quantity, and status.";

                return false;
            }

            if (!int.TryParse(QuantityText, out quantity)
                || quantity <= 0)
            {
                ErrorMessage =
                    "Quantity must be a whole number greater than zero.";

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

            try
            {
                _orderRepository.DeleteOrder(SelectedOrder.Id);

                LoadData();

                SelectedOrder = null;
                ClearForm();

                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = "Could not delete order: " + ex.Message;
            }
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

            SelectedProduct = ProductOptions.FirstOrDefault(
                item => item.Id == order.ProductId);

            TotalText = "Rs. " +
                order.Total.ToString("N2", CultureInfo.InvariantCulture);

            ErrorMessage = string.Empty;
        }

        private void ClearForm()
        {
            OrderId = string.Empty;
            CustomerName = string.Empty;
            QuantityText = string.Empty;
            OrderStatus = "Pending";
            SelectedProduct = null;
            TotalText = "Rs. 0.00";
            ErrorMessage = string.Empty;

            CommandManager.InvalidateRequerySuggested();
        }

        private void RecalculateTotal()
        {
            int quantity;

            if (SelectedProduct == null
                || !int.TryParse(QuantityText, out quantity)
                || quantity <= 0)
            {
                TotalText = "Rs. 0.00";
                return;
            }

            decimal total = SelectedProduct.UnitPrice * quantity;

            TotalText = "Rs. " +
                total.ToString("N2", CultureInfo.InvariantCulture);
        }

        private void RefreshViewAndSelection()
        {
            OrdersView.Refresh();

            if (SelectedOrder != null
                && !OrdersView.Contains(SelectedOrder))
            {
                SelectedOrder = null;
                ClearForm();
            }
        }

        private void ClearError()
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ErrorMessage = string.Empty;
            }
        }

        private void RefreshMetrics()
        {
            Metrics.Clear();

            Metrics.Add(new MetricCard
            {
                Title = "Active Orders",
                Value = _orders.Count(item =>
                    item.Status == "Pending"
                    || item.Status == "Processing"
                    || item.Status == "Ready to Ship").ToString(),
                Note = "Awaiting completion"
            });

            Metrics.Add(new MetricCard
            {
                Title = "Pending",
                Value = _orders.Count(item =>
                    item.Status == "Pending").ToString(),
                Note = "Awaiting action"
            });

            Metrics.Add(new MetricCard
            {
                Title = "Processing",
                Value = _orders.Count(item =>
                    item.Status == "Processing").ToString(),
                Note = "In workshop"
            });

            Metrics.Add(new MetricCard
            {
                Title = "Ready to Ship",
                Value = _orders.Count(item =>
                    item.Status == "Ready to Ship").ToString(),
                Note = "Packed orders"
            });
        }
    }
}