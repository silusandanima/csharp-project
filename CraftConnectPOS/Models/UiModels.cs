namespace CraftConnectPOS.Models
{
    public class MetricCard
    {
        public string Title { get; set; }
        public string Value { get; set; }
        public string Note { get; set; }
        public double Progress { get; set; }
    }

    public class InventoryRow
    {
        public string Code { get; set; }
        public string Material { get; set; }
        public string Quantity { get; set; }
        public string Unit { get; set; }
        public string Supplier { get; set; }
        public string Status { get; set; }
    }

    public class OrderRow
    {
        public string OrderId { get; set; }
        public string Customer { get; set; }
        public string Product { get; set; }
        public string Quantity { get; set; }
        public string Status { get; set; }
        public string Total { get; set; }
    }

    public class RevenueBar
    {
        public string Month { get; set; }
        public string Amount { get; set; }
        public double Height { get; set; }
        public bool IsCurrentMonth { get; set; }
    }
}

