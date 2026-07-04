using System;
using CraftConnect.Models;
using CraftConnect.Services;

namespace CraftConnect
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("======================================");
            Console.WriteLine("      CraftConnect Business Manager");
            Console.WriteLine("        Dashboard Overview");
            Console.WriteLine("======================================\n");

            // ---------------------------------------------------------
            // ABSTRACTION
            // The Program class does not know how dashboard data is collected.
            // It simply requests the data from the
            // DashboardService class.
            // ---------------------------------------------------------
            DashboardService service = new DashboardService();

            DashboardData data = service.GetDashboardData();

            Console.WriteLine("========== DASHBOARD SUMMARY ==========\n");

            Console.WriteLine($"Products          : {data.TotalProducts}");
            Console.WriteLine($"Orders            : {data.TotalOrders}");
            Console.WriteLine($"Revenue           : Rs. {data.Revenue}");
            Console.WriteLine($"Low Stock Items   : {data.LowStockMaterialsCount}");

            Console.WriteLine("\n========== RECENT CUSTOMER ORDERS ==========\n");

            if (data.RecentOrders.Count == 0)
            {
                Console.WriteLine("No recent orders available.");
            }
            else
            {
                foreach (Order order in data.RecentOrders)
                {
                    Console.WriteLine(
                        $"{order.OrderId} | {order.Customer} | {order.Status} | Rs. {order.Total}");
                }
            }

            Console.WriteLine("\n========== LOW STOCK MATERIALS ==========\n");

            if (data.LowStockMaterials.Count == 0)
            {
                Console.WriteLine("No low stock materials.");
            }
            else
            {
                foreach (LowStockMaterial material in data.LowStockMaterials)
                {
                    Console.WriteLine(
                        $"{material.Name} | {material.Supplier} | {material.Quantity}");
                }
            }
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}