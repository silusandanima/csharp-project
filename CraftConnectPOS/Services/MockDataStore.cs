using System.Collections.ObjectModel;
using System;
using System.Linq;
using CraftConnectPOS.Models;

namespace CraftConnectPOS.Services
{
    public class MockDataStore
    {
        public MockDataStore()
        {
            Materials = new ObservableCollection<InventoryItem>
            {
                new InventoryItem { Code = "MAT-201", Material = "Terracotta clay", Quantity = 120, Unit = "kg", ReorderLevel = 30, Supplier = "Ambalangoda Clay" },
                new InventoryItem { Code = "MAT-205", Material = "Reeds", Quantity = 12, Unit = "bundle", ReorderLevel = 15, Supplier = "Dumbara Weavers" },
                new InventoryItem { Code = "MAT-221", Material = "Wax", Quantity = 50, Unit = "kg", ReorderLevel = 20, Supplier = "Ceylon Bark Co." },
                new InventoryItem { Code = "MAT-245", Material = "Indigo dye", Quantity = 3, Unit = "kg", ReorderLevel = 10, Supplier = "Ceylon Tea Traders" }
            };

            Suppliers = new ObservableCollection<SupplierItem>
            {
                new SupplierItem { Supplier = "Ceylon Tea Traders", Phone = "077-9323450", Location = "Galle" },
                new SupplierItem { Supplier = "Dumbara Weavers", Phone = "077-8927543", Location = "Kandy" },
                new SupplierItem { Supplier = "Ceylon Bark Co.", Phone = "071-7656343", Location = "Colombo" },
                new SupplierItem { Supplier = "Ambalangoda Clay", Phone = "071-5623462", Location = "Ambalangoda" }
            };

            Products = new ObservableCollection<ProductItem>
            {
                new ProductItem { Code = "CR-104", Name = "Handwoven reed basket", UnitPrice = 2450m },
                new ProductItem { Code = "CR-118", Name = "Clay tea cup set", UnitPrice = 3200m },
                new ProductItem { Code = "CR-125", Name = "Batik wall hanging", UnitPrice = 5500m },
                new ProductItem { Code = "CR-134", Name = "Carved wooden mask", UnitPrice = 7800m }
            };

            Orders = new ObservableCollection<CustomerOrder>
            {
                new CustomerOrder { OrderId = "ORD-8421", Customer = "Jane Perera", Product = "Batik wall hanging", Quantity = 1, Status = "Shipped", UnitPrice = 5500m, Total = 5500m },
                new CustomerOrder { OrderId = "ORD-8419", Customer = "Anita Singh", Product = "Handwoven reed basket", Quantity = 3, Status = "Shipped", UnitPrice = 2450m, Total = 7350m },
                new CustomerOrder { OrderId = "ORD-8414", Customer = "Rajesh Kumar", Product = "Clay tea cup set", Quantity = 1, Status = "Pending", UnitPrice = 3200m, Total = 3200m },
                new CustomerOrder { OrderId = "ORD-8405", Customer = "Priya Nair", Product = "Carved wooden mask", Quantity = 2, Status = "Processing", UnitPrice = 7800m, Total = 15600m }
            };

            RefreshSupplierMaterials();
        }

        public ObservableCollection<InventoryItem> Materials { get; }
        public ObservableCollection<SupplierItem> Suppliers { get; }
        public ObservableCollection<ProductItem> Products { get; }
        public ObservableCollection<CustomerOrder> Orders { get; }

        public void RefreshSupplierMaterials()
        {
            foreach (var supplier in Suppliers)
            {
                supplier.Materials = string.Join(", ", Materials
                    .Where(material => string.Equals(
                        material.Supplier,
                        supplier.Supplier,
                        StringComparison.OrdinalIgnoreCase))
                    .Select(material => material.Material)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase));
            }
        }
    }
}
