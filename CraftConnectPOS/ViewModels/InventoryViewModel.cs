using System.Collections.ObjectModel;
using CraftConnectPOS.Models;

namespace CraftConnectPOS.ViewModels
{
    public class InventoryViewModel : ViewModelBase
    {
        public InventoryViewModel()
        {
            Metrics = new ObservableCollection<MetricCard>
            {
                new MetricCard { Title = "Total Materials", Value = "58", Note = "Tracked items" },
                new MetricCard { Title = "Out of Stock", Value = "2", Note = "Needs restock" },
                new MetricCard { Title = "Reorder Items", Value = "8", Note = "Below level" },
                new MetricCard { Title = "Inventory Value", Value = "Rs. 185K", Note = "Current stock" }
            };

            Materials = new ObservableCollection<InventoryRow>
            {
                new InventoryRow { Code = "MAT-201", Material = "Terracotta clay", Quantity = "120", Unit = "kg", Supplier = "Ambalangoda Clay Works", Status = "In Stock" },
                new InventoryRow { Code = "MAT-205", Material = "Reeds", Quantity = "12", Unit = "bundle", Supplier = "Ella Reed Co.", Status = "Low Stock" },
                new InventoryRow { Code = "MAT-221", Material = "Wax", Quantity = "50", Unit = "kg", Supplier = "Ambalangoda Clay Works", Status = "In Stock" },
                new InventoryRow { Code = "MAT-245", Material = "Indigo dye", Quantity = "3", Unit = "kg", Supplier = "Galle Mixed Works", Status = "Low Stock" }
            };
        }

        public ObservableCollection<MetricCard> Metrics { get; }
        public ObservableCollection<InventoryRow> Materials { get; }
    }
}

