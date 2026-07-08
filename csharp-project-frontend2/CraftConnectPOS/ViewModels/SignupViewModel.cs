using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CraftConnectPOS.Commands;
using CraftConnectPOS.Data;
using CraftConnectPOS.Services;

namespace CraftConnectPOS.ViewModels
{
    public class SignupViewModel : ViewModelBase
    {
        private readonly MainViewModel _main;
        private readonly AuthenticationService _authenticationService;

        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _confirmPassword = string.Empty;
        private string _errorMessage;
        private bool _hasError;
        private bool _isPasswordVisible;
        private bool _isLoading;

        public SignupViewModel(MainViewModel main)
            : this(main, null)
        {
        }

        // Kept for compatibility with your existing navigation setup.
        public SignupViewModel(
            MainViewModel main,
            MockDataStore dataStore)
        {
            _main = main ?? throw new ArgumentNullException(nameof(main));

            _ = dataStore;

            _authenticationService =
                new AuthenticationService();

            CreateAccountCommand = new RelayCommand(
                async _ => await CreateAccountAsync(),
                _ => !IsLoading);

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
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ICommand CreateAccountCommand { get; }

        public ICommand ShowLoginCommand { get; }

        private async Task CreateAccountAsync()
        {
            string username = (Username ?? string.Empty).Trim();
            string password = Password ?? string.Empty;
            string confirmPassword = ConfirmPassword ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username)
                || string.IsNullOrWhiteSpace(password)
                || string.IsNullOrWhiteSpace(confirmPassword))
            {
                ErrorMessage = "All fields are required.";
                HasError = true;
                return;
            }

            if (password != confirmPassword)
            {
                ErrorMessage = "Passwords do not match.";
                HasError = true;
                return;
            }

            IsLoading = true;
            HasError = false;
            ErrorMessage = string.Empty;

            try
            {
                AuthenticationResult result = await Task.Run(
                    () => _authenticationService.Register(
                        username,
                        password));

                if (!result.IsSuccess)
                {
                    ErrorMessage = result.Message;
                    HasError = true;
                    return;
                }

                Password = string.Empty;
                ConfirmPassword = string.Empty;
                HasError = false;
                ErrorMessage = string.Empty;

                _main.ShowLoginCommand.Execute(null);
            }
            catch
            {
                ErrorMessage =
                    "Account could not be created. Check the database connection.";
                HasError = true;
            }
            finally
            {
                IsLoading = false;
            }
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