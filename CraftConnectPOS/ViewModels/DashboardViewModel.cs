using System.Collections.ObjectModel;
using CraftConnectPOS.Models;

namespace CraftConnectPOS.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        public DashboardViewModel()
        {
            Metrics = new ObservableCollection<MetricCard>
            {
                new MetricCard { Title = "Products", Value = "248", Note = "+18 this month" },
                new MetricCard { Title = "Orders", Value = "84", Note = "+12 new" },
                new MetricCard { Title = "Revenue", Value = "Rs. 245K", Note = "+8% growth" },
                new MetricCard { Title = "Low Stock", Value = "12", Note = "Items reorder" }
            };

            RecentOrders = new ObservableCollection<OrderRow>
            {
                new OrderRow { OrderId = "ORD-001", Customer = "Anita Singh", Product = "Handwoven reed basket", Quantity = "2", Status = "Processing", Total = "Rs. 4,900" },
                new OrderRow { OrderId = "ORD-002", Customer = "Rajesh Kumar", Product = "Clay tea cup set", Quantity = "5", Status = "Shipped", Total = "Rs. 16,000" },
                new OrderRow { OrderId = "ORD-003", Customer = "Jane Perera", Product = "Palm leaf notebook", Quantity = "1", Status = "Pending", Total = "Rs. 1,250" }
            };

            LowStockItems = new ObservableCollection<InventoryRow>
            {
                new InventoryRow { Material = "Reed fiber", Quantity = "7 bundles remaining", Status = "Low Stock" },
                new InventoryRow { Material = "Clay powder", Quantity = "3 kg remaining", Status = "Low Stock" },
                new InventoryRow { Material = "Indigo Dyes", Quantity = "5 bottles remaining", Status = "Low Stock" }
            };
        }

        public ObservableCollection<MetricCard> Metrics { get; }
        public ObservableCollection<OrderRow> RecentOrders { get; }
        public ObservableCollection<InventoryRow> LowStockItems { get; }
    }
}

