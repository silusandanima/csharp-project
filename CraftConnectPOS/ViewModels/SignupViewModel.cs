using System.Windows.Input;

namespace CraftConnectPOS.ViewModels
{
    public class SignupViewModel : ViewModelBase
    {
        public SignupViewModel(MainViewModel main)
        {
            CreateAccountCommand = main.EnterAppCommand;
            ShowLoginCommand = main.ShowLoginCommand;
        }

        public ICommand CreateAccountCommand { get; }
        public ICommand ShowLoginCommand { get; }
    }
}

