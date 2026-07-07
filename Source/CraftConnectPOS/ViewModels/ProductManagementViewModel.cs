using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using CraftConnectPOS.Commands;
using CraftConnectPOS.Models;

// Backend namespaces
//using DatabaseHelperNamespace.Data;
using CraftConnectPOS.Data;

namespace CraftConnectPOS.ViewModels
{
    public class ProductManagementViewModel : ViewModelBase
    {
        // Repository
        private readonly ProductRepository repository = new ProductRepository();

        private ProductRow _selectedProduct;
        private string _code;
        private string _productName;
        private string _category;
        private string _price;
        private string _stock;
        private string _searchText;
        private string _errorMessage;
        private int _totalProductCount = 0;

        public ProductManagementViewModel()
        {
            Metrics = new ObservableCollection<MetricCard>
            {
                new MetricCard
                {
                    Title = "Total Products",
                    Value = "0",
                    Note = "Active items"
                },
                //new MetricCard
                //{
                //    Title = "Active Categories",
                //    Value = "14",
                //    Note = "Craft groups"
                //},
                new MetricCard
                {
                    Title = "Low Stock",
                    Value = "0",
                    Note = "Needs reorder"
                },
                new MetricCard
                {
                    Title = "Avg. Price",
                    Value = "0",
                    Note = "Across catalogue"
                }
            };

            Products = new ObservableCollection<ProductRow>();

            ProductsView = CollectionViewSource.GetDefaultView(Products);
            ProductsView.Filter = MatchesSearch;

            SaveProductCommand = new RelayCommand(_ => SaveProduct());
            AddNewCommand = new RelayCommand(_ => BeginNewProduct());
            EditProductCommand = new RelayCommand(EditProduct);
            DeleteProductCommand = new RelayCommand(DeleteProduct, CanDeleteProduct);

        }

        //====================================================
        // PROPERTIES
        //====================================================

        public ObservableCollection<MetricCard> Metrics { get; }

        public ObservableCollection<ProductRow> Products { get; }

        public ICollectionView ProductsView { get; }

        public ProductRow SelectedProduct
        {
            get => _selectedProduct;
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
            get => _code;
            set => SetFormValue(ref _code, value);
        }

        public string ProductName
        {
            get => _productName;
            set => SetFormValue(ref _productName, value);
        }

        public string Category
        {
            get => _category;
            set => SetFormValue(ref _category, value);
        }

        public string Price
        {
            get => _price;
            set => SetFormValue(ref _price, value);
        }

        public string Stock
        {
            get => _stock;
            set => SetFormValue(ref _stock, value);
        }

        public string SearchText
        {
            get => _searchText;
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
            get => _errorMessage;
            private set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }

        public bool HasError =>
            !string.IsNullOrEmpty(ErrorMessage);

        public ICommand SaveProductCommand { get; }

        public ICommand AddNewCommand { get; }

        public ICommand EditProductCommand { get; }

        public ICommand DeleteProductCommand { get; }

        //====================================================
        // LOAD PRODUCTS FROM SQL
        //====================================================

        public void Refresh()
        {
            LoadProducts();
        }

        private void LoadProducts()
        {
            try
            {
                Products.Clear();

                var databaseProducts = repository.GetAll();

                foreach (var product in databaseProducts)
                {
                    Products.Add(new ProductRow
                    {
                        Code = product.Id.ToString(),
                        Product = product.ProductName,
                        Category = string.IsNullOrWhiteSpace(product.Category)
                            ? "Uncategorized"
                            : product.Category,
                        Price = product.Price.ToString("0.00"),
                        Stock = product.StockQuantity.ToString()
                    });
                }

                _totalProductCount = Products.Count;

                UpdateProductMetrics(databaseProducts);
                ProductsView.Refresh();
                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                Products.Clear();
                UpdateProductMetrics(Array.Empty<Product>());
                ErrorMessage = "Could not load products: " + ex.Message;
            }
        }

        //====================================================
        // SAVE PRODUCT
        //====================================================

        private void SaveProduct()
        {
            if (string.IsNullOrWhiteSpace(ProductName)
                || string.IsNullOrWhiteSpace(Category)
                || string.IsNullOrWhiteSpace(Price)
                || string.IsNullOrWhiteSpace(Stock))
            {
                ErrorMessage = "Product name, category, price, and stock are required.";
                return;
            }

            if (ProductName.Trim().Length > 100 || Category.Trim().Length > 100)
            {
                ErrorMessage = "Product name and category are limited to 100 characters.";
                return;
            }

            decimal price;
            int stock;

            if (!decimal.TryParse(Price, out price))
            {
                ErrorMessage = "Invalid price.";
                return;
            }

            if (!int.TryParse(Stock, out stock))
            {
                ErrorMessage = "Invalid stock quantity.";
                return;
            }

            if (price < 0 || price > 99999999.99m || stock < 0)
            {
                ErrorMessage = "Price and stock must be non-negative and within database limits.";
                return;
            }

            try
            {
                Product existing = SelectedProduct == null
                    ? null
                    : repository.GetById(int.Parse(SelectedProduct.Code));

                var backendProduct = new Product
                {
                    ProductName = ProductName.Trim(),
                    Description = existing?.Description ?? string.Empty,
                    Category = Category.Trim(),
                    Price = price,
                    StockQuantity = stock
                };

                if (SelectedProduct == null)
                {
                    repository.Add(backendProduct);
                }
                else
                {
                    backendProduct.Id = int.Parse(SelectedProduct.Code);
                    repository.Update(backendProduct);
                }

                LoadProducts();
                SelectedProduct = null;
                ClearForm();
                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = "Could not save product: " + ex.Message;
            }
        }
        //====================================================
        // BEGIN NEW PRODUCT
        //====================================================

        private void BeginNewProduct()
        {
            SelectedProduct = null;
            ClearForm();
        }

        //====================================================
        // EDIT PRODUCT
        //====================================================

        private void EditProduct(object parameter)
        {
            var product = parameter as ProductRow;

            if (product == null)
                return;

            SelectedProduct = product;
        }

        //====================================================
        // DELETE PRODUCT
        //====================================================

        private void DeleteProduct(object parameter)
        {
            var product = parameter as ProductRow ?? SelectedProduct;

            if (product == null)
                return;

            try
            {
                repository.Delete(int.Parse(product.Code));

                LoadProducts();

                SelectedProduct = null;

                ClearForm();

                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        private bool CanDeleteProduct(object parameter)
        {
            return parameter is ProductRow || SelectedProduct != null;
        }

        //====================================================
        // SEARCH
        //====================================================

        private bool MatchesSearch(object item)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            var product = item as ProductRow;

            if (product == null)
                return false;

            string search = SearchText.Trim();

            return Contains(product.Code, search)
                || Contains(product.Product, search)
                || Contains(product.Category, search)
                || Contains(product.Price, search)
                || Contains(product.Stock, search);
        }

        private static bool Contains(string value, string search)
        {
            return !string.IsNullOrWhiteSpace(value)
                && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        //====================================================
        // FORM HELPERS
        //====================================================

        private void SetFormValue(
            ref string field,
            string value,
            [System.Runtime.CompilerServices.CallerMemberName]
            string propertyName = null)
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

        //====================================================
        // DASHBOARD METRICS
        //====================================================

        private void UpdateProductMetrics(System.Collections.Generic.IReadOnlyCollection<Product> products)
        {
            Metrics[0] = new MetricCard
            {
                Title = "Total Products",
                Value = Products.Count.ToString(),
                Note = "Active items"
            };

            Metrics[1] = new MetricCard
            {
                Title = "Low Stock",
                Value = products.Count(product => product.StockQuantity <= 5).ToString(),
                Note = "5 or fewer remaining"
            };

            Metrics[2] = new MetricCard
            {
                Title = "Avg. Price",
                Value = products.Count == 0
                    ? "Rs. 0.00"
                    : "Rs. " + products.Average(product => product.Price).ToString("N2"),
                Note = "Across catalogue"
            };
        }
    }
}
