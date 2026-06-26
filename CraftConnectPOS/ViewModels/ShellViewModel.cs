using System.Windows.Input;
using CraftConnectPOS.Commands;

namespace CraftConnectPOS.ViewModels
{
    public class ShellViewModel : ViewModelBase
    {
        private object _currentPage;
        private string _pageTitle;
        private string _pageSubtitle;

        public ShellViewModel(MainViewModel main)
        {
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
            CurrentPage = new DashboardViewModel();
        }

        private void ShowProducts()
        {
            PageTitle = "Product Management";
            PageSubtitle = "Add, edit, organize, and monitor craft products";
            CurrentPage = new ProductManagementViewModel();
        }

        private void ShowInventory()
        {
            PageTitle = "Material Inventory";
            PageSubtitle = "Track raw materials and reorder levels";
            CurrentPage = new InventoryViewModel();
        }

        private void ShowSuppliers()
        {
            PageTitle = "Suppliers Directory";
            PageSubtitle = "Manage and review supplier accounts";
            CurrentPage = new SuppliersViewModel();
        }

        private void ShowOrders()
        {
            PageTitle = "Customer Orders";
            PageSubtitle = "Plan, track, and manage customer orders";
            CurrentPage = new OrdersViewModel();
        }

        private void ShowStatistics()
        {
            PageTitle = "Statistics & Revenue";
            PageSubtitle = "Business reports, monthly progress, and product groups";
            CurrentPage = new StatisticsViewModel();
        }
    }
}
