using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using CraftConnectPOS.Commands;
using CraftConnectPOS.Models;

namespace CraftConnectPOS.ViewModels
{
    public class ProductManagementViewModel : ViewModelBase
    {
        private ProductRow _selectedProduct;
        private string _code;
        private string _productName;
        private string _category;
        private string _price;
        private string _stock;
        private string _searchText;
        private string _errorMessage;
        private int _totalProductCount = 248;

        public ProductManagementViewModel()
        {
            Metrics = new ObservableCollection<MetricCard>
            {
                new MetricCard { Title = "Total Products", Value = "248", Note = "Active items" },
                new MetricCard { Title = "Active Categories", Value = "14", Note = "Craft groups" },
                new MetricCard { Title = "Low Stock", Value = "12", Note = "Needs reorder" },
                new MetricCard { Title = "Avg. Price", Value = "Rs. 3.8K", Note = "Across catalogue" }
            };

            Products = new ObservableCollection<ProductRow>
            {
                new ProductRow { Code = "CR-104", Product = "Handwoven reed basket", Category = "Home decor", Stock = "42 units", Price = "Rs. 2,450" },
                new ProductRow { Code = "CR-118", Product = "Clay tea cup set", Category = "Pottery", Stock = "18 units", Price = "Rs. 3,200" },
                new ProductRow { Code = "CR-125", Product = "Batik wall hanging", Category = "Textiles", Stock = "9 units", Price = "Rs. 5,500" },
                new ProductRow { Code = "CR-134", Product = "Carved wooden mask", Category = "Wood craft", Stock = "22 units", Price = "Rs. 7,800" }
            };

            ProductsView = CollectionViewSource.GetDefaultView(Products);
            ProductsView.Filter = MatchesSearch;

            SaveProductCommand = new RelayCommand(_ => SaveProduct());
            AddNewCommand = new RelayCommand(_ => BeginNewProduct());
            EditProductCommand = new RelayCommand(EditProduct);
            DeleteProductCommand = new RelayCommand(DeleteProduct, CanDeleteProduct);

            SelectedProduct = Products.FirstOrDefault();
        }

        public ObservableCollection<MetricCard> Metrics { get; }
        public ObservableCollection<ProductRow> Products { get; }
        public ICollectionView ProductsView { get; }

        public ProductRow SelectedProduct
        {
            get { return _selectedProduct; }
            set
            {
                if (SetProperty(ref _selectedProduct, value))
                {
                    if (value != null)
                    {
                        Code = value.Code;
                        ProductName = value.Product;
                        Category = value.Category;
                        Price = value.Price;
                        Stock = value.Stock;
                    }

                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string Code
        {
            get { return _code; }
            set { SetFormValue(ref _code, value); }
        }

        public string ProductName
        {
            get { return _productName; }
            set { SetFormValue(ref _productName, value); }
        }

        public string Category
        {
            get { return _category; }
            set { SetFormValue(ref _category, value); }
        }

        public string Price
        {
            get { return _price; }
            set { SetFormValue(ref _price, value); }
        }

        public string Stock
        {
            get { return _stock; }
            set { SetFormValue(ref _stock, value); }
        }

        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ProductsView.Refresh();
                }
            }
        }

        public string ErrorMessage
        {
            get { return _errorMessage; }
            private set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }

        public bool HasError
        {
            get { return !string.IsNullOrEmpty(ErrorMessage); }
        }

        public ICommand SaveProductCommand { get; }
        public ICommand AddNewCommand { get; }
        public ICommand EditProductCommand { get; }
        public ICommand DeleteProductCommand { get; }

        private void SaveProduct()
        {
            if (string.IsNullOrWhiteSpace(Code)
                || string.IsNullOrWhiteSpace(ProductName)
                || string.IsNullOrWhiteSpace(Category)
                || string.IsNullOrWhiteSpace(Price)
                || string.IsNullOrWhiteSpace(Stock))
            {
                ErrorMessage = "All product fields are required.";
                return;
            }

            string normalizedCode = Code.Trim();
            bool duplicateCode = Products.Any(product =>
                product != SelectedProduct
                && string.Equals(product.Code, normalizedCode, StringComparison.OrdinalIgnoreCase));

            if (duplicateCode)
            {
                ErrorMessage = "Product code already exists.";
                return;
            }

            var savedProduct = new ProductRow
            {
                Code = normalizedCode,
                Product = ProductName.Trim(),
                Category = Category.Trim(),
                Price = Price.Trim(),
                Stock = Stock.Trim()
            };

            if (SelectedProduct == null)
            {
                Products.Add(savedProduct);
                _totalProductCount++;
            }
            else
            {
                int index = Products.IndexOf(SelectedProduct);
                if (index >= 0)
                {
                    Products[index] = savedProduct;
                }
            }

            UpdateProductMetric();
            ErrorMessage = string.Empty;
            SelectedProduct = savedProduct;
            ProductsView.Refresh();
        }

        private void BeginNewProduct()
        {
            SelectedProduct = null;
            ClearForm();
        }

        private void EditProduct(object parameter)
        {
            var product = parameter as ProductRow;
            if (product != null)
            {
                SelectedProduct = product;
            }
        }

        private void DeleteProduct(object parameter)
        {
            var product = parameter as ProductRow ?? SelectedProduct;
            if (product == null)
            {
                return;
            }

            if (Products.Remove(product))
            {
                _totalProductCount = Math.Max(0, _totalProductCount - 1);
                UpdateProductMetric();
            }

            SelectedProduct = null;
            ClearForm();
            ProductsView.Refresh();
        }

        private bool CanDeleteProduct(object parameter)
        {
            return parameter is ProductRow || SelectedProduct != null;
        }

        private bool MatchesSearch(object item)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return true;
            }

            var product = item as ProductRow;
            if (product == null)
            {
                return false;
            }

            string search = SearchText.Trim();
            return Contains(product.Code, search)
                || Contains(product.Product, search)
                || Contains(product.Category, search)
                || Contains(product.Stock, search)
                || Contains(product.Price, search);
        }

        private static bool Contains(string value, string search)
        {
            return !string.IsNullOrEmpty(value)
                && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void SetFormValue(
            ref string field,
            string value,
            [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            if (SetProperty(ref field, value, propertyName) && HasError)
            {
                ErrorMessage = string.Empty;
            }
        }

        private void ClearForm()
        {
            Code = string.Empty;
            ProductName = string.Empty;
            Category = string.Empty;
            Price = string.Empty;
            Stock = string.Empty;
            ErrorMessage = string.Empty;
            CommandManager.InvalidateRequerySuggested();
        }

        private void UpdateProductMetric()
        {
            Metrics[0] = new MetricCard
            {
                Title = "Total Products",
                Value = _totalProductCount.ToString(),
                Note = "Active items"
            };
        }
    }
}
