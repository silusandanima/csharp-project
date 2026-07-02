using System;
using System.IO;
using System.Windows;
using CraftConnectPOS.Services;
using CraftConnectPOS.ViewModels;

namespace CraftConnectPOS
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var databasePath = Path.Combine(appData, "CraftConnectPOS", "craftconnect.db");
            var dataService = new SqliteDataService(databasePath);
            var session = new AppSession();

            try
            {
                await dataService.InitializeAsync();
                var window = new MainWindow
                {
                    DataContext = new MainViewModel(dataService, session)
                };
                MainWindow = window;
                ShutdownMode = ShutdownMode.OnMainWindowClose;
                window.Show();
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    "CraftConnect could not initialize its database.\n\n" +
                    databasePath + "\n\n" + exception.Message,
                    "CraftConnect startup error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(-1);
            }
        }
    }
}

