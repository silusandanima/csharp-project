using System.Collections.ObjectModel;
using CraftConnectPOS.Models;

namespace CraftConnectPOS.ViewModels
{
    public class OrdersViewModel : ViewModelBase
    {
        public OrdersViewModel()
        {
            Metrics = new ObservableCollection<MetricCard>
            {
                new MetricCard { Title = "Delivered", Value = "45", Note = "This month" },
                new MetricCard { Title = "Pending", Value = "12", Note = "Awaiting action" },
                new MetricCard { Title = "Processing", Value = "21", Note = "In workshop" },
                new MetricCard { Title = "Ready to Ship", Value = "12", Note = "Packed orders" }
            };

            Orders = new ObservableCollection<OrderRow>
            {
                new OrderRow { OrderId = "ORD-8421", Customer = "Jane Perera", Product = "Batik wall hanging", Quantity = "1", Status = "Shipped", Total = "Rs. 11,800" },
                new OrderRow { OrderId = "ORD-8419", Customer = "Anita Singh", Product = "Handwoven reed basket", Quantity = "3", Status = "Shipped", Total = "Rs. 7,350" },
                new OrderRow { OrderId = "ORD-8414", Customer = "Rajesh Kumar", Product = "Clay tea cup set", Quantity = "1", Status = "Pending", Total = "Rs. 3,200" },
                new OrderRow { OrderId = "ORD-8405", Customer = "Priya Nair", Product = "Carved wooden mask", Quantity = "2", Status = "Processing", Total = "Rs. 15,600" }
            };
        }

        public ObservableCollection<MetricCard> Metrics { get; }
        public ObservableCollection<OrderRow> Orders { get; }
    }
}

