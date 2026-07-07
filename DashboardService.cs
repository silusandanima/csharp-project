using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using CraftConnect.Models;

namespace CraftConnect.Services
{
    public class DashboardService
    {
        private string connectionString =
            @"Data Source=localhost\SQLEXPRESS;Initial Catalog=ArtisanCraftDB;Integrated Security=True;TrustServerCertificate=True";

        public DashboardData GetDashboardData()
        {
            DashboardData dashboard = new DashboardData();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // PRODUCTS
                SqlCommand cmd1 = new SqlCommand("SELECT COUNT(*) FROM Products", conn);
                dashboard.TotalProducts = Convert.ToInt32(cmd1.ExecuteScalar());

                // ORDERS
                SqlCommand cmd2 = new SqlCommand("SELECT COUNT(*) FROM Orders", conn);
                dashboard.TotalOrders = Convert.ToInt32(cmd2.ExecuteScalar());

                // REVENUE
                SqlCommand cmd3 = new SqlCommand("SELECT ISNULL(SUM(TotalAmount),0) FROM Orders", conn);
                dashboard.Revenue = Convert.ToDecimal(cmd3.ExecuteScalar());

                // LOW STOCK
                SqlCommand cmd4 = new SqlCommand("SELECT COUNT(*) FROM Materials WHERE Quantity < 10", conn);
                dashboard.LowStockMaterialsCount = Convert.ToInt32(cmd4.ExecuteScalar());

                // RECENT ORDERS + PRODUCT + QTY 
                SqlCommand cmd5 = new SqlCommand(@"
                    SELECT TOP 5 
                        o.OrderID,
                        o.CustomerName,
                        o.Status,
                        o.TotalAmount,
                        p.ProductName,
                        oi.Quantity
                    FROM Orders o
                    INNER JOIN OrderItems oi ON o.OrderID = oi.OrderID
                    INNER JOIN Products p ON oi.ProductID = p.ProductID
                    ORDER BY o.OrderDate DESC", conn);

                SqlDataReader reader = cmd5.ExecuteReader();

                while (reader.Read())
                {
                    dashboard.RecentOrders.Add(new Order
                    {
                        OrderId = reader["OrderID"].ToString(),
                        Customer = reader["CustomerName"].ToString(),
                        Status = reader["Status"].ToString(),
                        Total = Convert.ToDecimal(reader["TotalAmount"])
                    });
                }

                reader.Close();

                // LOW STOCK MATERIALS
                SqlCommand cmd6 = new SqlCommand(@"
                    SELECT m.MaterialName, s.SupplierName, m.Quantity
                    FROM Materials m
                    INNER JOIN Suppliers s ON m.SupplierID = s.SupplierID
                    WHERE m.Quantity < 10", conn);

                SqlDataReader reader2 = cmd6.ExecuteReader();

                while (reader2.Read())
                {
                    dashboard.LowStockMaterials.Add(new LowStockMaterial
                    {
                        Name = reader2["MaterialName"].ToString(),
                        Supplier = reader2["SupplierName"].ToString(),
                        Quantity = reader2["Quantity"].ToString()
                    });
                }

                reader2.Close();
            }

            return dashboard;
        }
    }
}