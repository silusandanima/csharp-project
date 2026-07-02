using System.Collections.ObjectModel;
using CraftConnectPOS.Models;

namespace CraftConnectPOS.ViewModels
{
    public class ProductManagementViewModel : ViewModelBase
    {
        public ProductManagementViewModel()
        {
            Metrics = new ObservableCollection<MetricCard>
            {
                new MetricCard { Title = "Total Products", Value = "248", Note = "Active items" },
                new MetricCard { Title = "Active Categories", Value = "14", Note = "Craft groups" },
                new MetricCard { Title = "Low Stock", Value = "12", Note = "Needs reorder" },
                new MetricCard { Title = "Avg. Price", Value = "Rs. 3.8K", Note = "Across catalogue" }
            };

            Products = new ObservableCollection<ProductRow>
            {
                new ProductRow { Code = "CR-104", Product = "Handwoven reed basket", Category = "Home decor", Stock = "42 units", Price = "Rs. 2,450" },
                new ProductRow { Code = "CR-118", Product = "Clay tea cup set", Category = "Pottery", Stock = "18 units", Price = "Rs. 3,200" },
                new ProductRow { Code = "CR-125", Product = "Batik wall hanging", Category = "Textiles", Stock = "9 units", Price = "Rs. 5,500" },
                new ProductRow { Code = "CR-134", Product = "Carved wooden mask", Category = "Wood craft", Stock = "22 units", Price = "Rs. 7,800" }
            };
        }

        public ObservableCollection<MetricCard> Metrics { get; }
        public ObservableCollection<ProductRow> Products { get; }
    }
}

