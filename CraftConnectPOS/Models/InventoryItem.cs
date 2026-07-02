namespace CraftConnectPOS.Models
{
    public class InventoryItem
    {
        public string Code { get; set; }
        public string Material { get; set; }
        public int Quantity { get; set; }
        public string Unit { get; set; }
        public int ReorderLevel { get; set; }
        public string Supplier { get; set; }

        public string Status
        {
            get
            {
                if (Quantity == 0)
                {
                    return "Out of Stock";
                }

                return Quantity <= ReorderLevel ? "Low Stock" : "In Stock";
            }
        }
    }
}
