namespace CraftConnectPOS.Models
{
    public class CustomerOrder
    {
        public string OrderId { get; set; }
        public string Customer { get; set; }
        public string Product { get; set; }
        public int Quantity { get; set; }
        public string Status { get; set; }
        public decimal Total { get; set; }
    }
}
