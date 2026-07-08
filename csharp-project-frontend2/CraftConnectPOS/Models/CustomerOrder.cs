namespace CraftConnectPOS.Models
{
    public class CustomerOrder
    {
        // Actual OrderID from SQL Server.
        // Needed for Update and Delete.
        public int Id { get; set; }

        // Actual ProductID from SQL Server.
        // Needed to select the correct product while editing.
        public int ProductId { get; set; }

        // Display value shown in the WPF DataGrid.
        public string OrderId { get; set; }

        public string Customer { get; set; }

        public string Product { get; set; }

        public int Quantity { get; set; }

        public string Status { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal Total { get; set; }
    }
}