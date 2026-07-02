using System;

namespace CraftConnectPOS.Models
{
    public class CustomerOrder
    {
        public long Id { get; set; }
        public string OrderId { get; set; }
        public string Customer { get; set; }
        public long ProductId { get; set; }
        public string Product { get; set; }
        public int Quantity { get; set; }
        public string Status { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total { get; set; }
        public DateTime OrderDate { get; set; }
    }
}
