using System.Windows.Input;
using CraftConnectPOS.Commands;
using CraftConnectPOS.Data;

namespace CraftConnectPOS.ViewModels
{
    public class ShellViewModel : ViewModelBase
    {
        private object _currentPage;

        private string _pageTitle;
        private string _pageSubtitle;
        private readonly MainViewModel _main;
        private readonly DashboardViewModel _dashboardViewModel;
        private readonly ProductManagementViewModel _productManagementViewModel;
        private readonly InventoryViewModel _inventoryViewModel;
        private readonly SuppliersViewModel _suppliersViewModel;
        private readonly OrdersViewModel _ordersViewModel;
        private readonly StatisticsViewModel _statisticsViewModel;

        public ShellViewModel(MainViewModel main)
        {
            _main = main ?? throw new System.ArgumentNullException(nameof(main));
            if (!UserSession.IsSignedIn)
            {
                throw new System.InvalidOperationException("A signed-in user is required.");
            }

            _dashboardViewModel = new DashboardViewModel();
            _productManagementViewModel = new ProductManagementViewModel();
            _inventoryViewModel = new InventoryViewModel();
            _suppliersViewModel = new SuppliersViewModel();
            _ordersViewModel = new OrdersViewModel();
            _statisticsViewModel = new StatisticsViewModel();

            ShowDashboardCommand = new RelayCommand(_ => ShowDashboard());
            ShowProductsCommand = new RelayCommand(_ => ShowProducts());
            ShowInventoryCommand = new RelayCommand(_ => ShowInventory());
            ShowSuppliersCommand = new RelayCommand(_ => ShowSuppliers());
            ShowOrdersCommand = new RelayCommand(_ => ShowOrders());
            ShowStatisticsCommand = new RelayCommand(_ => ShowStatistics());
            LogoutCommand = new RelayCommand(_ => Logout());
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

        public string CurrentUsername
        {
            get { return UserSession.Username; }
        }

        public string CurrentUserInitial
        {
            get
            {
                return string.IsNullOrWhiteSpace(CurrentUsername)
                    ? "?"
                    : CurrentUsername.Substring(0, 1).ToUpperInvariant();
            }
        }

        public bool IsDashboardSelected { get { return CurrentPage == _dashboardViewModel; } }
        public bool IsProductsSelected { get { return CurrentPage == _productManagementViewModel; } }
        public bool IsInventorySelected { get { return CurrentPage == _inventoryViewModel; } }
        public bool IsSuppliersSelected { get { return CurrentPage == _suppliersViewModel; } }
        public bool IsOrdersSelected { get { return CurrentPage == _ordersViewModel; } }
        public bool IsStatisticsSelected { get { return CurrentPage == _statisticsViewModel; } }

        private void ShowDashboard()
        {
            PageTitle = "Dashboard";
            PageSubtitle = "Key metrics and operational overview";
            _dashboardViewModel.Refresh();
            CurrentPage = _dashboardViewModel;
            NotifyNavigationSelectionChanged();
        }

        private void ShowProducts()
        {
            PageTitle = "Product Management";
            PageSubtitle = "Add, edit, organize, and monitor craft products";
            _productManagementViewModel.Refresh();
            CurrentPage = _productManagementViewModel;
            NotifyNavigationSelectionChanged();
        }

        private void ShowInventory()
        {
            PageTitle = "Material Inventory";
            PageSubtitle = "Track raw materials and reorder levels";
            _inventoryViewModel.Refresh();
            CurrentPage = _inventoryViewModel;
            NotifyNavigationSelectionChanged();
        }

        private void ShowSuppliers()
        {
            PageTitle = "Suppliers Directory";
            PageSubtitle = "Manage and review supplier accounts";
            _suppliersViewModel.Refresh();
            CurrentPage = _suppliersViewModel;
            NotifyNavigationSelectionChanged();
        }

        private void ShowOrders()
        {
            PageTitle = "Customer Orders";
            PageSubtitle = "Plan, track, and manage customer orders";
            _ordersViewModel.Refresh();
            CurrentPage = _ordersViewModel;
            NotifyNavigationSelectionChanged();
        }

        private void ShowStatistics()
        {
            PageTitle = "Statistics & Revenue";
            PageSubtitle = "Business reports, monthly progress, and product groups";
            _statisticsViewModel.Refresh();
            CurrentPage = _statisticsViewModel;
            NotifyNavigationSelectionChanged();
        }
        private void Logout()
        {
            _main.Logout();
        }
        private void NotifyNavigationSelectionChanged()
        {
            OnPropertyChanged(nameof(IsDashboardSelected));
            OnPropertyChanged(nameof(IsProductsSelected));
            OnPropertyChanged(nameof(IsInventorySelected));
            OnPropertyChanged(nameof(IsSuppliersSelected));
            OnPropertyChanged(nameof(IsOrdersSelected));
            OnPropertyChanged(nameof(IsStatisticsSelected));
        }
    }
}
