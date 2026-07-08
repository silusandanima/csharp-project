using System.Windows.Input;
using CraftConnectPOS.Commands;
using CraftConnectPOS.Services;

namespace CraftConnectPOS.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private object _currentView;
        private readonly MockDataStore _dataStore;
        private ShellViewModel _shellViewModel;

        public MainViewModel()
        {
            _dataStore = new MockDataStore();
            ShowSignupCommand = new RelayCommand(_ => CurrentView = new SignupViewModel(this));
            ShowLoginCommand = new RelayCommand(_ => CurrentView = new LoginViewModel(this));
            EnterAppCommand = new RelayCommand(_ => EnterApplication());
            CurrentView = new LoginViewModel(this);
        }

        public object CurrentView
        {
            get { return _currentView; }
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        public ICommand ShowSignupCommand { get; }
        public ICommand ShowLoginCommand { get; }
        public ICommand EnterAppCommand { get; }

        private void EnterApplication()
        {
            if (_shellViewModel == null)
            {
                _shellViewModel = new ShellViewModel(this, _dataStore);
            }

            CurrentView = _shellViewModel;
        }
    }
}
