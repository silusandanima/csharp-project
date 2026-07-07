namespace CraftConnectPOS.Models
{
    public class InventoryItem
    {
        public int Id { get; set; }

        private string _code;

        public string Code
        {
            get
            {
                return Id > 0 ? Id.ToString() : _code;
            }
            set
            {
                _code = value;
            }
        }

        public string Material { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; }
        public int ReorderLevel { get; set; }

        public int SupplierId { get; set; }
        public string Supplier { get; set; }

        public string Status
        {
            get
            {
                if (Quantity == 0)
                {
                    return "Out of Stock";
                }

                return Quantity <= ReorderLevel
                    ? "Low Stock"
                    : "In Stock";
            }
        }
    }
}