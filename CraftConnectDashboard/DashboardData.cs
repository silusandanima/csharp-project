using System.Collections.Generic;

namespace CraftConnect.Models
{
    // ==========================================================
    // ENCAPSULATION
    //
    // DashboardData stores all dashboard information together.
    // Data is accessed safely through public properties (get and set).
    // ==========================================================

    public class DashboardData
    {
        // Dashboard Summary

        public int TotalProducts { get; set; }

        public int TotalOrders { get; set; }

        public decimal Revenue { get; set; }

        public int LowStockMaterialsCount { get; set; }

        // Collections used by the dashboard tables

        public List<Order> RecentOrders { get; set; } = new List<Order>();

        public List<LowStockMaterial> LowStockMaterials { get; set; } = new List<LowStockMaterial>();
    }


    // ==========================================================
    // ENCAPSULATION
    //
    // Represents one customer order displayed on the dashboard.
    // ==========================================================

    public class Order
    {
        public string OrderId { get; set; } = "";

        public string Customer { get; set; } = "";

        public string Status { get; set; } = "";

        public decimal Total { get; set; }
    }


    // ==========================================================
    // ENCAPSULATION
    //
    // Represents one material that is running low in stock.
    // ==========================================================

    public class LowStockMaterial
    {
        public string Name { get; set; } = "";

        public string Supplier { get; set; } = "";

        public string Quantity { get; set; } = "";
    }
}