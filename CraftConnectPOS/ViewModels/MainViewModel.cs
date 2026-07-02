using System;
using System.Windows.Input;
using CraftConnectPOS.Commands;
using CraftConnectPOS.Models;
using CraftConnectPOS.Services;

namespace CraftConnectPOS.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly SqliteDataService _dataService;
        private readonly IAppSession _session;
        private object _currentView;

        public MainViewModel(SqliteDataService dataService, IAppSession session)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            LogoutCommand = new RelayCommand(_ => Logout());
            ShowLogin();
        }

        public object CurrentView
        {
            get { return _currentView; }
            private set { SetProperty(ref _currentView, value); }
        }

        public ICommand LogoutCommand { get; }

        public void EnterApplication(UserAccount user)
        {
            _session.CurrentUser = user ?? throw new ArgumentNullException(nameof(user));
            CurrentView = new ShellViewModel(this, _dataService, _session);
        }

        private void ShowLogin()
        {
            CurrentView = new LoginViewModel(this, _dataService);
        }

        private void Logout()
        {
            _session.Clear();
            ShowLogin();
        }
    }
}
