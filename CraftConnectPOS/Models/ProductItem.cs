namespace CraftConnectPOS.Models
{
    public class ProductItem
    {
        public long Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public int StockQuantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
