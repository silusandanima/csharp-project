using System.Collections.ObjectModel;
using CraftConnectPOS.Models;

namespace CraftConnectPOS.ViewModels
{
    public class SuppliersViewModel : ViewModelBase
    {
        public SuppliersViewModel()
        {
            Metrics = new ObservableCollection<MetricCard>
            {
                new MetricCard { Title = "Total Suppliers", Value = "24", Note = "Active contacts" },
                new MetricCard { Title = "Active Contracts", Value = "18", Note = "In progress" },
                new MetricCard { Title = "Avg. Lead Time", Value = "4-5 days", Note = "Across suppliers" },
                new MetricCard { Title = "Material Coverage", Value = "94%", Note = "Supply coverage" }
            };

            Suppliers = new ObservableCollection<SupplierRow>
            {
                new SupplierRow { Supplier = "Ceylon Tea Traders", Phone = "077-9323450", Location = "Galle", Materials = "Batik fabric, indigo dyes, wax" },
                new SupplierRow { Supplier = "Dumbara Weavers", Phone = "077-8927543", Location = "Kandy", Materials = "Reed fiber, cotton thread" },
                new SupplierRow { Supplier = "Ceylon Bark Co.", Phone = "071-7656343", Location = "Colombo", Materials = "Batik fabric, wax, dye" },
                new SupplierRow { Supplier = "Ambalangoda Clay", Phone = "071-5623462", Location = "Ambalangoda", Materials = "Clay, terracotta, ceramics" }
            };
        }

        public ObservableCollection<MetricCard> Metrics { get; }
        public ObservableCollection<SupplierRow> Suppliers { get; }
    }
}

