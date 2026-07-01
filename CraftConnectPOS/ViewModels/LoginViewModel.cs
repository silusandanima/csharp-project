using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CraftConnectPOS.Commands;
using CraftConnectPOS.Models;
using CraftConnectPOS.Services;

namespace CraftConnectPOS.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly MainViewModel _main;
        private readonly MockDataStore _dataStore;
        private string _username = "admin"; // default mock credential
        private string _password = "demo123"; // default mock credential
        private string _errorMessage;
        private bool _hasError;
        private bool _isPasswordVisible;
        private bool _isLoading;

        // Backward compatibility constructor
        public LoginViewModel(MainViewModel main) : this(main, null)
        {
        }

        public LoginViewModel(MainViewModel main, MockDataStore dataStore)
        {
            _main = main ?? throw new ArgumentNullException(nameof(main));
            _dataStore = dataStore; // Will be utilized once integrated globally
            
            SignInCommand = new RelayCommand(async _ => await SignInAsync(), _ => !IsLoading);
            ShowSignupCommand = main.ShowSignupCommand;
        }

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged();
                ClearErrors();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
                ClearErrors();
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        public bool HasError
        {
            get => _hasError;
            set
            {
                _hasError = value;
                OnPropertyChanged();
            }
        }

        public bool IsPasswordVisible
        {
            get => _isPasswordVisible;
            set
            {
                _isPasswordVisible = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
                // Refresh command states
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ICommand SignInCommand { get; }
        public ICommand ShowSignupCommand { get; }

        private async Task SignInAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Username and password are required.";
                HasError = true;
                return;
            }

            // Find user in session-only mock data store
            var user = UserAccount.SessionUsers.FirstOrDefault(
                u => u.Username.Equals(Username.Trim(), StringComparison.OrdinalIgnoreCase) && u.Password == Password);

            if (user == null)
            {
                ErrorMessage = "Invalid username or password.";
                HasError = true;
                return;
            }

            HasError = false;
            ErrorMessage = string.Empty;
            IsLoading = true;

            // Simulated loading delay to replicate API authentication behavior
            await Task.Delay(1500);

            IsLoading = false;
            _main.EnterAppCommand.Execute(null);
        }

        private void ClearErrors()
        {
            if (HasError)
            {
                HasError = false;
                ErrorMessage = string.Empty;
            }
        }
    }
}


