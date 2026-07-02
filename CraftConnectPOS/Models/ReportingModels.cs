using System.Collections.Generic;

namespace CraftConnectPOS.Models
{
    public class DashboardData
    {
        public IList<MetricCard> Metrics { get; set; }
        public IList<OrderRow> RecentOrders { get; set; }
        public IList<InventoryRow> LowStockItems { get; set; }
    }

    public class StatisticsData
    {
        public IList<MetricCard> Metrics { get; set; }
        public IList<RevenueBar> Bars { get; set; }
        public IList<MetricCard> Breakdown { get; set; }
    }
}
