using System.Collections.ObjectModel;
using CraftConnectPOS.Models;

namespace CraftConnectPOS.Services
{
    public class MockDataStore
    {
        public MockDataStore()
        {
            Materials = new ObservableCollection<InventoryItem>
            {
                new InventoryItem { Code = "MAT-201", Material = "Terracotta clay", Quantity = 120, Unit = "kg", ReorderLevel = 30, Supplier = "Ambalangoda Clay Works" },
                new InventoryItem { Code = "MAT-205", Material = "Reeds", Quantity = 12, Unit = "bundle", ReorderLevel = 15, Supplier = "Ella Reed Co." },
                new InventoryItem { Code = "MAT-221", Material = "Wax", Quantity = 50, Unit = "kg", ReorderLevel = 20, Supplier = "Ambalangoda Clay Works" },
                new InventoryItem { Code = "MAT-245", Material = "Indigo dye", Quantity = 3, Unit = "kg", ReorderLevel = 10, Supplier = "Galle Mixed Works" }
            };

            Suppliers = new ObservableCollection<SupplierItem>
            {
                new SupplierItem { Supplier = "Ceylon Tea Traders", Phone = "077-9323450", Location = "Galle", Materials = "Batik fabric, indigo dyes, wax" },
                new SupplierItem { Supplier = "Dumbara Weavers", Phone = "077-8927543", Location = "Kandy", Materials = "Reed fiber, cotton thread" },
                new SupplierItem { Supplier = "Ceylon Bark Co.", Phone = "071-7656343", Location = "Colombo", Materials = "Batik fabric, wax, dye" },
                new SupplierItem { Supplier = "Ambalangoda Clay", Phone = "071-5623462", Location = "Ambalangoda", Materials = "Clay, terracotta, ceramics" }
            };

            Orders = new ObservableCollection<CustomerOrder>
            {
                new CustomerOrder { OrderId = "ORD-8421", Customer = "Jane Perera", Product = "Batik wall hanging", Quantity = 1, Status = "Shipped", Total = 11800m },
                new CustomerOrder { OrderId = "ORD-8419", Customer = "Anita Singh", Product = "Handwoven reed basket", Quantity = 3, Status = "Shipped", Total = 7350m },
                new CustomerOrder { OrderId = "ORD-8414", Customer = "Rajesh Kumar", Product = "Clay tea cup set", Quantity = 1, Status = "Pending", Total = 3200m },
                new CustomerOrder { OrderId = "ORD-8405", Customer = "Priya Nair", Product = "Carved wooden mask", Quantity = 2, Status = "Processing", Total = 15600m }
            };
        }

        public ObservableCollection<InventoryItem> Materials { get; }
        public ObservableCollection<SupplierItem> Suppliers { get; }
        public ObservableCollection<CustomerOrder> Orders { get; }
    }
}
