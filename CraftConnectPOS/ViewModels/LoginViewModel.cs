using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CraftConnectPOS.Commands;
using CraftConnectPOS.Services;

namespace CraftConnectPOS.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly MainViewModel _main;
        private readonly IAuthenticationService _authenticationService;
        private string _username = "admin";
        private string _password = "demo123";
        private string _errorMessage;
        private bool _hasError;
        private bool _isPasswordVisible;
        private bool _isLoading;

        public LoginViewModel(MainViewModel main, IAuthenticationService authenticationService)
        {
            _main = main ?? throw new ArgumentNullException(nameof(main));
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            SignInCommand = new RelayCommand(async _ => await SignInAsync(), _ => !IsLoading);
        }

        public string Username
        {
            get { return _username; }
            set
            {
                if (SetProperty(ref _username, value))
                {
                    ClearErrors();
                }
            }
        }

        public string Password
        {
            get { return _password; }
            set
            {
                if (SetProperty(ref _password, value))
                {
                    ClearErrors();
                }
            }
        }

        public string ErrorMessage
        {
            get { return _errorMessage; }
            private set { SetProperty(ref _errorMessage, value); }
        }

        public bool HasError
        {
            get { return _hasError; }
            private set { SetProperty(ref _hasError, value); }
        }

        public bool IsPasswordVisible
        {
            get { return _isPasswordVisible; }
            set { SetProperty(ref _isPasswordVisible, value); }
        }

        public bool IsLoading
        {
            get { return _isLoading; }
            private set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public ICommand SignInCommand { get; }

        private async Task SignInAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ShowError("Username and password are required.");
                return;
            }

            IsLoading = true;
            try
            {
                var user = await _authenticationService.AuthenticateAsync(Username.Trim(), Password);
                if (user == null)
                {
                    ShowError("Invalid username or password.");
                    return;
                }

                ClearErrors();
                _main.EnterApplication(user);
            }
            catch (Exception exception)
            {
                ShowError("Login failed. " + exception.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ShowError(string message)
        {
            ErrorMessage = message;
            HasError = true;
        }

        private void ClearErrors()
        {
            HasError = false;
            ErrorMessage = string.Empty;
        }
    }
}
