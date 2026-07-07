using System.Windows.Input;
using CraftConnectPOS.Commands;
using CraftConnectPOS.Data;

namespace CraftConnectPOS.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private object _currentView;
        private ShellViewModel _shellViewModel;

        public MainViewModel()
        {
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
            if (!UserSession.IsSignedIn)
            {
                CurrentView = new LoginViewModel(this);
                return;
            }

            if (_shellViewModel == null)
            {
                _shellViewModel = new ShellViewModel(this);
            }

            CurrentView = _shellViewModel;
        }

        public void Logout()
        {
            UserSession.Clear();
            _shellViewModel = null;
            CurrentView = new LoginViewModel(this);
        }
    }
}
