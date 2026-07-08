namespace CraftConnectPOS.Models
{
    public class ProductItem
    {
        // Actual ProductID from SQL Server.
        public int Id { get; set; }

        // Kept because MockDataStore and other UI code may still use it.
        public string Code { get; set; }

        public string Name { get; set; }

        public decimal UnitPrice { get; set; }
    }
}