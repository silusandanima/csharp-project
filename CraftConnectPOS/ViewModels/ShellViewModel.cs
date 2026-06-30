using System.Windows.Input;
using CraftConnectPOS.Commands;
using CraftConnectPOS.Services;

namespace CraftConnectPOS.ViewModels
{
    public class ShellViewModel : ViewModelBase
    {
        private object _currentPage;
        private string _pageTitle;
        private string _pageSubtitle;
        private readonly DashboardViewModel _dashboardViewModel;
        private readonly ProductManagementViewModel _productManagementViewModel;
        private readonly InventoryViewModel _inventoryViewModel;
        private readonly SuppliersViewModel _suppliersViewModel;
        private readonly OrdersViewModel _ordersViewModel;
        private readonly StatisticsViewModel _statisticsViewModel;

        public ShellViewModel(MainViewModel main, MockDataStore dataStore)
        {
            _dashboardViewModel = new DashboardViewModel();
            _productManagementViewModel = new ProductManagementViewModel();
            _inventoryViewModel = new InventoryViewModel(dataStore);
            _suppliersViewModel = new SuppliersViewModel(dataStore);
            _ordersViewModel = new OrdersViewModel(dataStore);
            _statisticsViewModel = new StatisticsViewModel();

            ShowDashboardCommand = new RelayCommand(_ => ShowDashboard());
            ShowProductsCommand = new RelayCommand(_ => ShowProducts());
            ShowInventoryCommand = new RelayCommand(_ => ShowInventory());
            ShowSuppliersCommand = new RelayCommand(_ => ShowSuppliers());
            ShowOrdersCommand = new RelayCommand(_ => ShowOrders());
            ShowStatisticsCommand = new RelayCommand(_ => ShowStatistics());
            LogoutCommand = main.ShowLoginCommand;
            ShowDashboard();
        }

        public object CurrentPage
        {
            get { return _currentPage; }
            set
            {
                _currentPage = value;
                OnPropertyChanged();
            }
        }

        public string PageTitle
        {
            get { return _pageTitle; }
            set
            {
                _pageTitle = value;
                OnPropertyChanged();
            }
        }

        public string PageSubtitle
        {
            get { return _pageSubtitle; }
            set
            {
                _pageSubtitle = value;
                OnPropertyChanged();
            }
        }

        public ICommand ShowDashboardCommand { get; }
        public ICommand ShowProductsCommand { get; }
        public ICommand ShowInventoryCommand { get; }
        public ICommand ShowSuppliersCommand { get; }
        public ICommand ShowOrdersCommand { get; }
        public ICommand ShowStatisticsCommand { get; }
        public ICommand LogoutCommand { get; }

        private void ShowDashboard()
        {
            PageTitle = "Dashboard";
            PageSubtitle = "Key metrics and operational overview";
            CurrentPage = _dashboardViewModel;
        }

        private void ShowProducts()
        {
            PageTitle = "Product Management";
            PageSubtitle = "Add, edit, organize, and monitor craft products";
            CurrentPage = _productManagementViewModel;
        }

        private void ShowInventory()
        {
            PageTitle = "Material Inventory";
            PageSubtitle = "Track raw materials and reorder levels";
            CurrentPage = _inventoryViewModel;
        }

        private void ShowSuppliers()
        {
            PageTitle = "Suppliers Directory";
            PageSubtitle = "Manage and review supplier accounts";
            CurrentPage = _suppliersViewModel;
        }

        private void ShowOrders()
        {
            PageTitle = "Customer Orders";
            PageSubtitle = "Plan, track, and manage customer orders";
            CurrentPage = _ordersViewModel;
        }

        private void ShowStatistics()
        {
            PageTitle = "Statistics & Revenue";
            PageSubtitle = "Business reports, monthly progress, and product groups";
            CurrentPage = _statisticsViewModel;
        }
    }
}
