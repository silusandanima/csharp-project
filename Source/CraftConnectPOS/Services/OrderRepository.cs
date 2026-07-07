using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;

namespace CraftConnectPOS.Data
{
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
                    IFNULL(c.CustomerName, 'Unknown customer') AS CustomerName,
                    IFNULL(p.ProductName, 'Unknown product') AS ProductName,
                    IFNULL(oi.Quantity, 0) AS Quantity,
                    IFNULL(o.Status, 'Pending') AS Status,
                    IFNULL(oi.UnitPrice, 0) AS UnitPrice,
                    IFNULL(o.TotalAmount, 0) AS TotalAmount
                FROM Orders o
                INNER JOIN OrderItems oi
                    ON oi.OrderID = o.OrderID
                LEFT JOIN Customers c
                    ON c.CustomerID = o.CustomerID
                LEFT JOIN Products p
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
            ValidateOrderInput(
                customerName,
                productId,
                quantity,
                unitPrice,
                status);

            decimal totalAmount = unitPrice * quantity;

            using (SqliteConnection connection = DatabaseHelper.OpenConnection())
            using (SqliteTransaction transaction = connection.BeginTransaction())
            {
                if (!IsCancelled(status))
                {
                    ReserveStock(
                        connection,
                        transaction,
                        productId,
                        quantity);
                }

                int customerId = GetOrCreateCustomer(
                    connection,
                    transaction,
                    customerName.Trim());

                using (SqliteCommand command = CreateCommand(
                    connection,
                    transaction,
                    @"INSERT INTO Orders
                        (CustomerID, OrderDate, Status, TotalAmount)
                      VALUES
                        (@CustomerID, CURRENT_TIMESTAMP, @Status, @TotalAmount);"))
                {
                    command.Parameters.AddWithValue("@CustomerID", customerId);
                    command.Parameters.AddWithValue("@Status", status);
                    command.Parameters.AddWithValue("@TotalAmount", totalAmount);
                    command.ExecuteNonQuery();
                }

                int orderId = GetLastInsertId(connection, transaction);

                using (SqliteCommand command = CreateCommand(
                    connection,
                    transaction,
                    @"INSERT INTO OrderItems
                        (OrderID, ProductID, Quantity, UnitPrice)
                      VALUES
                        (@OrderID, @ProductID, @Quantity, @UnitPrice);"))
                {
                    command.Parameters.AddWithValue("@OrderID", orderId);
                    command.Parameters.AddWithValue("@ProductID", productId);
                    command.Parameters.AddWithValue("@Quantity", quantity);
                    command.Parameters.AddWithValue("@UnitPrice", unitPrice);
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
                return orderId;
            }
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

            ValidateOrderInput(
                customerName,
                productId,
                quantity,
                unitPrice,
                status);

            decimal totalAmount = unitPrice * quantity;

            using (SqliteConnection connection = DatabaseHelper.OpenConnection())
            using (SqliteTransaction transaction = connection.BeginTransaction())
            {
                int oldProductId;
                int oldQuantity;
                string oldStatus;

                using (SqliteCommand command = CreateCommand(
                    connection,
                    transaction,
                    @"SELECT oi.ProductID, oi.Quantity, o.Status
                      FROM Orders o
                      INNER JOIN OrderItems oi
                        ON oi.OrderID = o.OrderID
                      WHERE o.OrderID = @OrderID
                      LIMIT 1;"))
                {
                    command.Parameters.AddWithValue("@OrderID", orderId);

                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            throw new InvalidOperationException(
                                "The selected order no longer exists.");
                        }

                        oldProductId = reader.GetInt32(0);
                        oldQuantity = reader.GetInt32(1);
                        oldStatus = reader.GetString(2);
                    }
                }

                if (!IsCancelled(oldStatus))
                {
                    RestoreStock(
                        connection,
                        transaction,
                        oldProductId,
                        oldQuantity);
                }

                if (!IsCancelled(status))
                {
                    ReserveStock(
                        connection,
                        transaction,
                        productId,
                        quantity);
                }

                int customerId = GetOrCreateCustomer(
                    connection,
                    transaction,
                    customerName.Trim());

                using (SqliteCommand command = CreateCommand(
                    connection,
                    transaction,
                    @"UPDATE Orders
                      SET CustomerID = @CustomerID,
                          Status = @Status,
                          TotalAmount = @TotalAmount
                      WHERE OrderID = @OrderID;"))
                {
                    command.Parameters.AddWithValue("@CustomerID", customerId);
                    command.Parameters.AddWithValue("@Status", status);
                    command.Parameters.AddWithValue("@TotalAmount", totalAmount);
                    command.Parameters.AddWithValue("@OrderID", orderId);

                    if (command.ExecuteNonQuery() == 0)
                    {
                        throw new InvalidOperationException(
                            "The selected order no longer exists.");
                    }
                }

                using (SqliteCommand command = CreateCommand(
                    connection,
                    transaction,
                    @"UPDATE OrderItems
                      SET ProductID = @ProductID,
                          Quantity = @Quantity,
                          UnitPrice = @UnitPrice
                      WHERE OrderID = @OrderID;"))
                {
                    command.Parameters.AddWithValue("@ProductID", productId);
                    command.Parameters.AddWithValue("@Quantity", quantity);
                    command.Parameters.AddWithValue("@UnitPrice", unitPrice);
                    command.Parameters.AddWithValue("@OrderID", orderId);

                    if (command.ExecuteNonQuery() == 0)
                    {
                        throw new InvalidOperationException(
                            "The selected order item no longer exists.");
                    }
                }

                transaction.Commit();
            }
        }

        public void DeleteOrder(int orderId)
        {
            if (orderId <= 0)
            {
                throw new ArgumentException("A valid order is required.");
            }

            using (SqliteConnection connection = DatabaseHelper.OpenConnection())
            using (SqliteTransaction transaction = connection.BeginTransaction())
            {
                int customerId;
                int productId;
                int quantity;
                string status;

                using (SqliteCommand command = CreateCommand(
                    connection,
                    transaction,
                    @"SELECT o.CustomerID, oi.ProductID, oi.Quantity, o.Status
                      FROM Orders o
                      INNER JOIN OrderItems oi
                        ON oi.OrderID = o.OrderID
                      WHERE o.OrderID = @OrderID
                      LIMIT 1;"))
                {
                    command.Parameters.AddWithValue("@OrderID", orderId);

                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            throw new InvalidOperationException(
                                "The selected order no longer exists.");
                        }

                        customerId = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                        productId = reader.GetInt32(1);
                        quantity = reader.GetInt32(2);
                        status = reader.GetString(3);
                    }
                }

                if (!IsCancelled(status))
                {
                    RestoreStock(
                        connection,
                        transaction,
                        productId,
                        quantity);
                }

                ExecuteById(
                    connection,
                    transaction,
                    "DELETE FROM OrderItems WHERE OrderID = @ID;",
                    orderId);

                if (ExecuteById(
                        connection,
                        transaction,
                        "DELETE FROM Orders WHERE OrderID = @ID;",
                        orderId) == 0)
                {
                    throw new InvalidOperationException(
                        "The selected order no longer exists.");
                }

                if (customerId > 0)
                {
                    using (SqliteCommand command = CreateCommand(
                        connection,
                        transaction,
                        @"DELETE FROM Customers
                          WHERE CustomerID = @CustomerID
                            AND NOT EXISTS (
                                SELECT 1
                                FROM Orders
                                WHERE CustomerID = @CustomerID);"))
                    {
                        command.Parameters.AddWithValue(
                            "@CustomerID",
                            customerId);
                        command.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
            }
        }

        private static int GetOrCreateCustomer(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string customerName)
        {
            using (SqliteCommand command = CreateCommand(
                connection,
                transaction,
                @"SELECT CustomerID
                  FROM Customers
                  WHERE CustomerName = @CustomerName COLLATE NOCASE
                  ORDER BY CustomerID
                  LIMIT 1;"))
            {
                command.Parameters.AddWithValue(
                    "@CustomerName",
                    customerName);

                object result = command.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result);
                }
            }

            using (SqliteCommand command = CreateCommand(
                connection,
                transaction,
                @"INSERT INTO Customers
                    (CustomerName, Phone, Email, Address)
                  VALUES
                    (@CustomerName, NULL, NULL, NULL);"))
            {
                command.Parameters.AddWithValue(
                    "@CustomerName",
                    customerName);
                command.ExecuteNonQuery();
            }

            return GetLastInsertId(connection, transaction);
        }

        private static void ReserveStock(
            SqliteConnection connection,
            SqliteTransaction transaction,
            int productId,
            int quantity)
        {
            using (SqliteCommand command = CreateCommand(
                connection,
                transaction,
                @"UPDATE Products
                  SET StockQuantity = StockQuantity - @Quantity
                  WHERE ProductID = @ProductID
                    AND StockQuantity >= @Quantity;"))
            {
                command.Parameters.AddWithValue("@Quantity", quantity);
                command.Parameters.AddWithValue("@ProductID", productId);

                if (command.ExecuteNonQuery() == 0)
                {
                    throw new InvalidOperationException(
                        "The selected product does not have enough stock.");
                }
            }
        }

        private static void RestoreStock(
            SqliteConnection connection,
            SqliteTransaction transaction,
            int productId,
            int quantity)
        {
            using (SqliteCommand command = CreateCommand(
                connection,
                transaction,
                @"UPDATE Products
                  SET StockQuantity = StockQuantity + @Quantity
                  WHERE ProductID = @ProductID;"))
            {
                command.Parameters.AddWithValue("@Quantity", quantity);
                command.Parameters.AddWithValue("@ProductID", productId);

                if (command.ExecuteNonQuery() == 0)
                {
                    throw new InvalidOperationException(
                        "The order references a product that no longer exists.");
                }
            }
        }

        private static int ExecuteById(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string commandText,
            int id)
        {
            using (SqliteCommand command = CreateCommand(
                connection,
                transaction,
                commandText))
            {
                command.Parameters.AddWithValue("@ID", id);
                return command.ExecuteNonQuery();
            }
        }

        private static int GetLastInsertId(
            SqliteConnection connection,
            SqliteTransaction transaction)
        {
            using (SqliteCommand command = CreateCommand(
                connection,
                transaction,
                "SELECT last_insert_rowid();"))
            {
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private static SqliteCommand CreateCommand(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string commandText)
        {
            SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = commandText;
            return command;
        }

        private static bool IsCancelled(string status)
        {
            return string.Equals(
                status,
                "Cancelled",
                StringComparison.OrdinalIgnoreCase);
        }

        private static void ValidateOrderInput(
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
                throw new ArgumentException(
                    "Quantity must be greater than zero.");
            }

            string[] allowedStatuses =
            {
                "Pending",
                "Processing",
                "Ready to Ship",
                "Shipped",
                "Completed",
                "Cancelled"
            };

            if (Array.IndexOf(allowedStatuses, status) < 0)
            {
                throw new ArgumentException("The order status is invalid.");
            }

            if (unitPrice < 0)
            {
                throw new ArgumentException(
                    "Unit price cannot be negative.");
            }
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
