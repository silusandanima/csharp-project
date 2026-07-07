using System;
using System.Windows;
using CraftConnectPOS.Data;

namespace CraftConnectPOS
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                string connectionString =
                    DatabaseBootstrapper.GetDefaultConnectionString();

                DatabaseBootstrapper.EnsureCreated(connectionString);
                DatabaseHelper.Initialize(connectionString);
                DatabaseHelper.TestConnection();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "The application could not initialize its database.\n\n"
                    + "Make sure this folder is writable and all application "
                    + "files remain together.\n\n"
                    + "Technical detail: " + ex.Message,
                    "Database Setup Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown(-1);
                return;
            }

            base.OnStartup(e);
        }
    }
}
