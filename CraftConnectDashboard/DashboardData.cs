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
        public string OrderId { get; set; } = "";

        public string Customer { get; set; } = "";

        public string Status { get; set; } = "";

        public decimal Total { get; set; }
    }

    public class LowStockMaterial
    {
        public string Name { get; set; } = "";

        public string Supplier { get; set; } = "";

        public string Quantity { get; set; } = "";
    }
}