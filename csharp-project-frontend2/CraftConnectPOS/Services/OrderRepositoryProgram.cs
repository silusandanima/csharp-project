using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace CraftConnectPOS.Data
{
    // Represents one completed order row read from SQL Server.
    // This is used only by OrderRepository and OrdersViewModel.
    public class OrderRecord
    {
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string CustomerName { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public string Status { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount { get; set; }

        public OrderRecord()
        {
            CustomerName = string.Empty;
            ProductName = string.Empty;
            Status = string.Empty;
        }
    }

    public class OrderRepository
    {
        public List<OrderRecord> GetAll()
        {
            const string query = @"
                SELECT
                    o.OrderID,
                    oi.ProductID,
                    ISNULL(c.CustomerName, 'Unknown customer') AS CustomerName,
                    ISNULL(p.ProductName, 'Unknown product') AS ProductName,
                    ISNULL(oi.Quantity, 0) AS Quantity,
                    ISNULL(o.Status, 'Pending') AS Status,
                    ISNULL(oi.UnitPrice, 0) AS UnitPrice,
                    ISNULL(o.TotalAmount, 0) AS TotalAmount
                FROM dbo.Orders o
                INNER JOIN dbo.OrderItems oi
                    ON oi.OrderID = o.OrderID
                LEFT JOIN dbo.Customers c
                    ON c.CustomerID = o.CustomerID
                LEFT JOIN dbo.Products p
                    ON p.ProductID = oi.ProductID
                ORDER BY o.OrderID DESC;";

            DataTable table = DatabaseHelper.ExecuteQuery(query);
            var orders = new List<OrderRecord>();

            foreach (DataRow row in table.Rows)
            {
                orders.Add(MapDataRowToOrder(row));
            }

            return orders;
        }

        public int CreateOrder(
            string customerName,
            int productId,
            int quantity,
            decimal unitPrice,
            string status)
        {
            if (string.IsNullOrWhiteSpace(customerName))
            {
                throw new ArgumentException("Customer name is required.");
            }

            if (productId <= 0)
            {
                throw new ArgumentException("A valid product is required.");
            }

            if (quantity <= 0)
            {
                throw new ArgumentException("Quantity must be greater than zero.");
            }

            decimal totalAmount = unitPrice * quantity;

            const string query = @"
                SET XACT_ABORT ON;
                BEGIN TRANSACTION;

                DECLARE @CustomerID INT;

                SELECT TOP (1)
                    @CustomerID = CustomerID
                FROM dbo.Customers
                WHERE CustomerName = @CustomerName
                ORDER BY CustomerID;

                IF @CustomerID IS NULL
                BEGIN
                    INSERT INTO dbo.Customers
                        (CustomerName, Phone, Email, Address)
                    VALUES
                        (@CustomerName, NULL, NULL, NULL);

                    SET @CustomerID = CONVERT(INT, SCOPE_IDENTITY());
                END;

                INSERT INTO dbo.Orders
                    (CustomerID, OrderDate, Status, TotalAmount)
                VALUES
                    (@CustomerID, SYSDATETIME(), @Status, @TotalAmount);

                DECLARE @OrderID INT;
                SET @OrderID = CONVERT(INT, SCOPE_IDENTITY());

                INSERT INTO dbo.OrderItems
                    (OrderID, ProductID, Quantity, UnitPrice)
                VALUES
                    (@OrderID, @ProductID, @Quantity, @UnitPrice);

                COMMIT TRANSACTION;

                SELECT @OrderID;";

            object result = DatabaseHelper.ExecuteScalar(
                query,
                new[]
                {
                    new SqlParameter("@CustomerName", customerName.Trim()),
                    new SqlParameter("@ProductID", productId),
                    new SqlParameter("@Quantity", quantity),
                    new SqlParameter("@UnitPrice", unitPrice),
                    new SqlParameter("@Status", status),
                    new SqlParameter("@TotalAmount", totalAmount)
                });

            return Convert.ToInt32(result);
        }

        public void UpdateOrder(
            int orderId,
            string customerName,
            int productId,
            int quantity,
            decimal unitPrice,
            string status)
        {
            if (orderId <= 0)
            {
                throw new ArgumentException("A valid order is required.");
            }

            if (string.IsNullOrWhiteSpace(customerName))
            {
                throw new ArgumentException("Customer name is required.");
            }

            if (productId <= 0)
            {
                throw new ArgumentException("A valid product is required.");
            }

            if (quantity <= 0)
            {
                throw new ArgumentException("Quantity must be greater than zero.");
            }

            decimal totalAmount = unitPrice * quantity;

            const string query = @"
                SET XACT_ABORT ON;
                BEGIN TRANSACTION;

                DECLARE @CustomerID INT;

                SELECT TOP (1)
                    @CustomerID = CustomerID
                FROM dbo.Customers
                WHERE CustomerName = @CustomerName
                ORDER BY CustomerID;

                IF @CustomerID IS NULL
                BEGIN
                    INSERT INTO dbo.Customers
                        (CustomerName, Phone, Email, Address)
                    VALUES
                        (@CustomerName, NULL, NULL, NULL);

                    SET @CustomerID = CONVERT(INT, SCOPE_IDENTITY());
                END;

                UPDATE dbo.Orders
                SET
                    CustomerID = @CustomerID,
                    Status = @Status,
                    TotalAmount = @TotalAmount
                WHERE OrderID = @OrderID;

                UPDATE dbo.OrderItems
                SET
                    ProductID = @ProductID,
                    Quantity = @Quantity,
                    UnitPrice = @UnitPrice
                WHERE OrderID = @OrderID;

                COMMIT TRANSACTION;";

            DatabaseHelper.ExecuteNonQuery(
                query,
                new[]
                {
                    new SqlParameter("@OrderID", orderId),
                    new SqlParameter("@CustomerName", customerName.Trim()),
                    new SqlParameter("@ProductID", productId),
                    new SqlParameter("@Quantity", quantity),
                    new SqlParameter("@UnitPrice", unitPrice),
                    new SqlParameter("@Status", status),
                    new SqlParameter("@TotalAmount", totalAmount)
                });
        }

        public void DeleteOrder(int orderId)
        {
            if (orderId <= 0)
            {
                throw new ArgumentException("A valid order is required.");
            }

            const string query = @"
                SET XACT_ABORT ON;
                BEGIN TRANSACTION;

                DELETE FROM dbo.OrderItems
                WHERE OrderID = @OrderID;

                DELETE FROM dbo.Orders
                WHERE OrderID = @OrderID;

                COMMIT TRANSACTION;";

            DatabaseHelper.ExecuteNonQuery(
                query,
                new[]
                {
                    new SqlParameter("@OrderID", orderId)
                });
        }

        private static OrderRecord MapDataRowToOrder(DataRow row)
        {
            return new OrderRecord
            {
                OrderId = Convert.ToInt32(row["OrderID"]),
                ProductId = Convert.ToInt32(row["ProductID"]),
                CustomerName = row["CustomerName"].ToString(),
                ProductName = row["ProductName"].ToString(),
                Quantity = Convert.ToInt32(row["Quantity"]),
                Status = row["Status"].ToString(),
                UnitPrice = Convert.ToDecimal(row["UnitPrice"]),
                TotalAmount = Convert.ToDecimal(row["TotalAmount"])
            };
        }
    }
}