using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CraftConnectPOS.Commands;
using CraftConnectPOS.Models;
using CraftConnectPOS.Services;

namespace CraftConnectPOS.ViewModels
{
    public class SignupViewModel : ViewModelBase
    {
        private readonly MainViewModel _main;
        private readonly MockDataStore _dataStore;
        private string _username = "newuser"; // default mock credential
        private string _password = "demo123"; // default mock credential
        private string _confirmPassword = "demo123"; // default mock credential
        private string _errorMessage;
        private bool _hasError;
        private bool _isPasswordVisible;
        private bool _isLoading;

        // Backward compatibility constructor
        public SignupViewModel(MainViewModel main) : this(main, null)
        {
        }

        public SignupViewModel(MainViewModel main, MockDataStore dataStore)
        {
            _main = main ?? throw new ArgumentNullException(nameof(main));
            _dataStore = dataStore; // Will be utilized once integrated globally
            
            CreateAccountCommand = new RelayCommand(async _ => await CreateAccountAsync(), _ => !IsLoading);
            ShowLoginCommand = main.ShowLoginCommand;
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

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                _confirmPassword = value;
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

        public ICommand CreateAccountCommand { get; }
        public ICommand ShowLoginCommand { get; }

        private async Task CreateAccountAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                ErrorMessage = "All fields are required.";
                HasError = true;
                return;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match.";
                HasError = true;
                return;
            }

            // Check duplicate usernames (case insensitive)
            bool exists = UserAccount.SessionUsers.Any(
                u => u.Username.Equals(Username.Trim(), StringComparison.OrdinalIgnoreCase));

            if (exists)
            {
                ErrorMessage = "Username is already taken.";
                HasError = true;
                return;
            }

            HasError = false;
            ErrorMessage = string.Empty;
            IsLoading = true;

            // Simulated loading delay to replicate API register behavior
            await Task.Delay(1500);

            // Add the new user to session-only collection
            UserAccount.SessionUsers.Add(new UserAccount
            {
                Username = Username.Trim(),
                Password = Password,
                Role = "Team Member"
            });

            IsLoading = false;
            
            // Navigate back to Login screen on success
            _main.ShowLoginCommand.Execute(null);
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


