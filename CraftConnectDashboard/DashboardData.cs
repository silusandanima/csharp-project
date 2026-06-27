using System.Collections.Generic;

namespace CraftConnect.Models
{
    public class DashboardData
    {
        public int TotalProducts { get; set; }

        public int TotalOrders { get; set; }

        public decimal Revenue { get; set; }

        public int LowStockMaterialsCount { get; set; }
    }

    public class Order
    {

    }

    public class LowStockMaterial
    {

    }
}