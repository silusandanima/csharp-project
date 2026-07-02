using System;
using System.Threading.Tasks;
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

        public ShellViewModel(MainViewModel main, SqliteDataService dataService, IAppSession session)
        {
            if (main == null) throw new ArgumentNullException(nameof(main));
            if (dataService == null) throw new ArgumentNullException(nameof(dataService));
            if (session == null || session.CurrentUser == null) throw new InvalidOperationException("An authenticated session is required.");

            Username = session.CurrentUser.Username;
            Role = session.CurrentUser.Role;
            UserInitial = string.IsNullOrWhiteSpace(Username) ? "A" : Username.Substring(0, 1).ToUpperInvariant();

            _dashboardViewModel = new DashboardViewModel(dataService);
            _productManagementViewModel = new ProductManagementViewModel(dataService);
            _inventoryViewModel = new InventoryViewModel(dataService, dataService);
            _suppliersViewModel = new SuppliersViewModel(dataService);
            _ordersViewModel = new OrdersViewModel(dataService, dataService);
            _statisticsViewModel = new StatisticsViewModel(dataService);

            ShowDashboardCommand = new RelayCommand(async _ => await ShowPageAsync("Dashboard", "Key metrics and operational overview", _dashboardViewModel));
            ShowProductsCommand = new RelayCommand(async _ => await ShowPageAsync("Product Management", "Add, edit, organize, and monitor craft products", _productManagementViewModel));
            ShowInventoryCommand = new RelayCommand(async _ => await ShowPageAsync("Material Inventory", "Track raw materials and reorder levels", _inventoryViewModel));
            ShowSuppliersCommand = new RelayCommand(async _ => await ShowPageAsync("Suppliers Directory", "Manage and review supplier accounts", _suppliersViewModel));
            ShowOrdersCommand = new RelayCommand(async _ => await ShowPageAsync("Customer Orders", "Plan, track, and manage customer orders", _ordersViewModel));
            ShowStatisticsCommand = new RelayCommand(async _ => await ShowPageAsync("Statistics & Revenue", "Business reports, monthly progress, and product groups", _statisticsViewModel));
            LogoutCommand = main.LogoutCommand;
            ShowDashboardCommand.Execute(null);
        }

        public object CurrentPage
        {
            get { return _currentPage; }
            private set { SetProperty(ref _currentPage, value); }
        }

        public string PageTitle
        {
            get { return _pageTitle; }
            private set { SetProperty(ref _pageTitle, value); }
        }

        public string PageSubtitle
        {
            get { return _pageSubtitle; }
            private set { SetProperty(ref _pageSubtitle, value); }
        }

        public string Username { get; }
        public string Role { get; }
        public string UserInitial { get; }
        public ICommand ShowDashboardCommand { get; }
        public ICommand ShowProductsCommand { get; }
        public ICommand ShowInventoryCommand { get; }
        public ICommand ShowSuppliersCommand { get; }
        public ICommand ShowOrdersCommand { get; }
        public ICommand ShowStatisticsCommand { get; }
        public ICommand LogoutCommand { get; }

        private async Task ShowPageAsync(string title, string subtitle, object page)
        {
            PageTitle = title;
            PageSubtitle = subtitle;
            CurrentPage = page;
            var refreshable = page as IRefreshable;
            if (refreshable != null)
            {
                await refreshable.RefreshAsync();
            }
        }
    }
}
