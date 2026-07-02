using System.Collections.ObjectModel;
using CraftConnectPOS.Models;

namespace CraftConnectPOS.ViewModels
{
    public class StatisticsViewModel : ViewModelBase
    {
        public StatisticsViewModel()
        {
            Metrics = new ObservableCollection<MetricCard>
            {
                new MetricCard { Title = "Total Revenue", Value = "Rs. 1.34M", Note = "+12% this quarter" },
                new MetricCard { Title = "Monthly Avg", Value = "Rs. 224K", Note = "+8% vs last month" },
                new MetricCard { Title = "Orders Complete", Value = "584", Note = "94% completed" },
                new MetricCard { Title = "Top Category", Value = "Home Decor", Note = "Best performer" }
            };

            Bars = new ObservableCollection<RevenueBar>
            {
                new RevenueBar { Month = "Jan", Amount = "Rs. 180K", Height = 92 },
                new RevenueBar { Month = "Feb", Amount = "Rs. 210K", Height = 118 },
                new RevenueBar { Month = "Mar", Amount = "Rs. 190K", Height = 104 },
                new RevenueBar { Month = "Apr", Amount = "Rs. 260K", Height = 148 },
                new RevenueBar { Month = "May", Amount = "Rs. 235K", Height = 132 },
                new RevenueBar { Month = "Jun", Amount = "Rs. 290K", Height = 172 }
            };

            Breakdown = new ObservableCollection<MetricCard>
            {
                new MetricCard { Title = "Home Decor", Value = "Rs. 320K", Note = "42% share" },
                new MetricCard { Title = "Pottery", Value = "Rs. 270K", Note = "28% share" },
                new MetricCard { Title = "Textiles", Value = "Rs. 210K", Note = "19% share" },
                new MetricCard { Title = "Wood Crafts", Value = "Rs. 132K", Note = "11% share" }
            };
        }

        public ObservableCollection<MetricCard> Metrics { get; }
        public ObservableCollection<RevenueBar> Bars { get; }
        public ObservableCollection<MetricCard> Breakdown { get; }
    }
}

