namespace CraftConnectPOS.Models
{
    public class SupplierItem
    {
        // Database SupplierID. Needed for Edit and Delete.
        public int Id { get; set; }

        // UI display fields
        public string Supplier { get; set; }
        public string Phone { get; set; }

        // This maps to the Address column in SQL Server.
        public string Location { get; set; }

        // Temporary display text until Materials/Inventory is connected.
        public string Materials { get; set; }
    }
}