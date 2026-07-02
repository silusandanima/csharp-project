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
    public class ProductManagementViewModel : ViewModelBase, IRefreshable
    {
        private readonly IProductRepository _repository;
        private readonly ObservableCollection<ProductItem> _productItems;
        private ProductItem _selectedProduct;
        private string _code;
        private string _productName;
        private string _category;
        private string _stockText;
        private string _priceText;
        private string _searchText;
        private string _errorMessage;
        private bool _isBusy;

        public ProductManagementViewModel(IProductRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _productItems = new ObservableCollection<ProductItem>();
            Metrics = new ObservableCollection<MetricCard>();
            ProductsView = CollectionViewSource.GetDefaultView(_productItems);
            ProductsView.Filter = FilterProduct;
            NewCommand = new RelayCommand(_ => StartNew(), _ => !IsBusy);
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => !IsBusy);
            DeleteCommand = new RelayCommand(async _ => await DeleteAsync(), _ => !IsBusy && SelectedProduct != null);
            CancelCommand = new RelayCommand(_ => Cancel(), _ => !IsBusy);
        }

        public ObservableCollection<MetricCard> Metrics { get; }
        public ICollectionView ProductsView { get; }

        public ProductItem SelectedProduct
        {
            get { return _selectedProduct; }
            set
            {
                if (SetProperty(ref _selectedProduct, value) && value != null) LoadProduct(value);
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string Code
        {
            get { return _code; }
            set { SetProperty(ref _code, value); }
        }

        public string ProductName
        {
            get { return _productName; }
            set { SetProperty(ref _productName, value); }
        }

        public string Category
        {
            get { return _category; }
            set { SetProperty(ref _category, value); }
        }

        public string StockText
        {
            get { return _stockText; }
            set { SetProperty(ref _stockText, value); }
        }

        public string PriceText
        {
            get { return _priceText; }
            set { SetProperty(ref _priceText, value); }
        }

        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (SetProperty(ref _searchText, value)) ProductsView.Refresh();
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
                Replace(_productItems, await _repository.GetProductsAsync());
                ProductsView.Refresh();
                RefreshMetrics();
                ErrorMessage = string.Empty;
            }
            catch (Exception exception)
            {
                ErrorMessage = "Products could not be loaded. " + exception.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SaveAsync()
        {
            int stock;
            decimal price;
            if (!Validate(out stock, out price)) return;
            IsBusy = true;
            var succeeded = false;
            try
            {
                var product = new ProductItem
                {
                    Id = SelectedProduct == null ? 0 : SelectedProduct.Id,
                    Code = Code.Trim(),
                    Name = ProductName.Trim(),
                    Category = Category.Trim(),
                    StockQuantity = stock,
                    UnitPrice = price
                };
                if (product.Id == 0) await _repository.AddProductAsync(product);
                else await _repository.UpdateProductAsync(product);
                succeeded = true;
                SelectedProduct = null;
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
            if (SelectedProduct == null) return;
            IsBusy = true;
            var succeeded = false;
            try
            {
                await _repository.DeleteProductAsync(SelectedProduct.Id);
                succeeded = true;
                SelectedProduct = null;
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

        private bool Validate(out int stock, out decimal price)
        {
            stock = 0;
            price = 0;
            if (string.IsNullOrWhiteSpace(Code) ||
                string.IsNullOrWhiteSpace(ProductName) ||
                string.IsNullOrWhiteSpace(Category))
            {
                ErrorMessage = "Complete the product code, name, and category.";
                return false;
            }
            if (!int.TryParse(StockText, out stock) || stock < 0)
            {
                ErrorMessage = "Stock must be a whole number of zero or more.";
                return false;
            }
            var normalizedPrice = (PriceText ?? string.Empty).Replace("Rs.", string.Empty).Replace(",", string.Empty).Trim();
            if (!decimal.TryParse(normalizedPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out price) || price < 0)
            {
                ErrorMessage = "Price must be a non-negative number.";
                return false;
            }
            ErrorMessage = string.Empty;
            return true;
        }

        private bool FilterProduct(object item)
        {
            var product = item as ProductItem;
            if (product == null) return false;
            var query = (SearchText ?? string.Empty).Trim();
            return query.Length == 0 ||
                Contains(product.Code, query) ||
                Contains(product.Name, query) ||
                Contains(product.Category, query);
        }

        private static bool Contains(string value, string query)
        {
            return (value ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void StartNew()
        {
            SelectedProduct = null;
            ClearForm();
        }

        private void Cancel()
        {
            if (SelectedProduct != null) LoadProduct(SelectedProduct);
            else ClearForm();
        }

        private void LoadProduct(ProductItem product)
        {
            Code = product.Code;
            ProductName = product.Name;
            Category = product.Category;
            StockText = product.StockQuantity.ToString(CultureInfo.InvariantCulture);
            PriceText = product.UnitPrice.ToString("0.##", CultureInfo.InvariantCulture);
            ErrorMessage = string.Empty;
        }

        private void ClearForm()
        {
            Code = string.Empty;
            ProductName = string.Empty;
            Category = string.Empty;
            StockText = string.Empty;
            PriceText = string.Empty;
            ErrorMessage = string.Empty;
        }

        private void RefreshMetrics()
        {
            Metrics.Clear();
            Metrics.Add(new MetricCard { Title = "Total Products", Value = _productItems.Count.ToString(), Note = "Catalogue items" });
            Metrics.Add(new MetricCard { Title = "Active Categories", Value = _productItems.Select(item => item.Category).Distinct(StringComparer.OrdinalIgnoreCase).Count().ToString(), Note = "Craft groups" });
            Metrics.Add(new MetricCard { Title = "Low Stock", Value = _productItems.Count(item => item.StockQuantity <= 10).ToString(), Note = "10 units or fewer" });
            var average = _productItems.Count == 0 ? 0 : _productItems.Average(item => item.UnitPrice);
            Metrics.Add(new MetricCard { Title = "Avg. Price", Value = "Rs. " + average.ToString("N2", CultureInfo.InvariantCulture), Note = "Across catalogue" });
        }

        private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
        {
            target.Clear();
            foreach (var item in source) target.Add(item);
        }
    }
}
