using System;
using System.Configuration;
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
                ConnectionStringSettings settings =
                    ConfigurationManager.ConnectionStrings[
                        "CraftConnectDatabase"];

                if (settings == null ||
                    string.IsNullOrWhiteSpace(settings.ConnectionString))
                {
                    throw new InvalidOperationException(
                        "The CraftConnectDatabase connection string is missing from App.config.");
                }

                DatabaseHelper.Initialize(settings.ConnectionString);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "The application could not connect to the database.\n\n"
                    + "Make sure SQL Server LocalDB is installed and the "
                    + "ArtisanCraftDB setup script has been run.\n\n"
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