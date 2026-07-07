namespace CraftConnectPOS.Models
{
    public class ProductItem
    {
        // Actual ProductID from SQLite.
        public int Id { get; set; }

        // Display code derived from the SQL identity.
        public string Code { get; set; }

        public string Name { get; set; }

        public decimal UnitPrice { get; set; }

        public int StockQuantity { get; set; }
    }
}
