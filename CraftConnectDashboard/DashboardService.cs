using CraftConnect.Models;

namespace CraftConnect.Services
{
    public class DashboardService
    {
        public DashboardData GetDashboardData()
        {
            DashboardData dashboard = new DashboardData();

            dashboard.TotalProducts = 0;

            dashboard.TotalOrders = 0;

            dashboard.Revenue = 0;

            dashboard.LowStockMaterialsCount = 0;
           
            return dashboard;
        }

    }
}