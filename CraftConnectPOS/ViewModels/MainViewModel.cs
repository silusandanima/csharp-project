using System.Windows.Input;
using CraftConnectPOS.Commands;

namespace CraftConnectPOS.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private object _currentView;

        public MainViewModel()
        {
            ShowSignupCommand = new RelayCommand(_ => CurrentView = new SignupViewModel(this));
            ShowLoginCommand = new RelayCommand(_ => CurrentView = new LoginViewModel(this));
            EnterAppCommand = new RelayCommand(_ => CurrentView = new ShellViewModel(this));
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
    }
}
