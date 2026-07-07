using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CraftConnectPOS.Commands;
using CraftConnectPOS.Data;

namespace CraftConnectPOS.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly MainViewModel _main;
        private readonly AuthenticationService _authenticationService;

        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _errorMessage;
        private bool _hasError;
        private bool _isPasswordVisible;
        private bool _isLoading;

        public LoginViewModel(MainViewModel main)
        {
            _main = main ?? throw new ArgumentNullException(nameof(main));

            _authenticationService =
                new AuthenticationService();

            SignInCommand = new RelayCommand(
                async _ => await SignInAsync(),
                _ => !IsLoading);

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
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ICommand SignInCommand { get; }

        public ICommand ShowSignupCommand { get; }

        private async Task SignInAsync()
        {
            string username = (Username ?? string.Empty).Trim();
            string password = Password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username)
                || string.IsNullOrWhiteSpace(password))
            {
                ErrorMessage = "Username and password are required.";
                HasError = true;
                return;
            }

            IsLoading = true;
            HasError = false;
            ErrorMessage = string.Empty;

            try
            {
                AuthenticationResult result = await Task.Run(
                    () => _authenticationService.Login(
                        username,
                        password));

                if (!result.IsSuccess)
                {
                    ErrorMessage = result.Message;
                    HasError = true;
                    return;
                }

                UserSession.Start(result.Username);

                Password = string.Empty;
                HasError = false;
                ErrorMessage = string.Empty;

                _main.EnterAppCommand.Execute(null);
            }
            catch
            {
                ErrorMessage =
                    "Login could not be completed. Check the database connection.";
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
