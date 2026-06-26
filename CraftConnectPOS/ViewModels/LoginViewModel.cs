using System.Windows.Input;

namespace CraftConnectPOS.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        public LoginViewModel(MainViewModel main)
        {
            SignInCommand = main.EnterAppCommand;
            ShowSignupCommand = main.ShowSignupCommand;
        }

        public ICommand SignInCommand { get; }
        public ICommand ShowSignupCommand { get; }
    }
}

