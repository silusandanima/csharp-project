namespace CraftConnectPOS.Models
{
    public class MetricCard
    {
        public string Title { get; set; }
        public string Value { get; set; }
        public string Note { get; set; }
    }

    public class ProductRow
    {
        public string Code { get; set; }
        public string Product { get; set; }
        public string Category { get; set; }
        public string Stock { get; set; }
        public string Price { get; set; }
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

    public class SupplierRow
    {
        public string Supplier { get; set; }
        public string Phone { get; set; }
        public string Location { get; set; }
        public string Materials { get; set; }
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
    }
}

