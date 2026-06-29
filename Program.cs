using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

// File paths for our simple text-based database
const string ProductsFile = "products.txt";
const string SuppliersFile = "suppliers.txt";
const string OrdersFile = "orders.txt";

// Ensure database files exist with some initial sample data if they are missing
InitializeDatabase();

while (true)
{
    Console.Clear();
    Console.WriteLine("===== CRAFTCONNECT MANAGEMENT MENU =====");
    Console.WriteLine("1. View All Products");
    Console.WriteLine("2. Add New Product");
    Console.WriteLine("3. View All Suppliers");
    Console.WriteLine("4. Add New Supplier");
    Console.WriteLine("5. View All Orders (Read)");
    Console.WriteLine("6. Add New Order (Create)");
    Console.WriteLine("7. Update Order Status (Update)");
    Console.WriteLine("8. Delete Order (Delete)");
    Console.WriteLine("9. Exit");
    Console.Write("Choose an option (1-9): ");

    string? choice = Console.ReadLine();

    Console.WriteLine();
    switch (choice)
    {
        case "1": ViewAllProducts(); break;
        case "2": AddNewProduct(); break;
        case "3": ViewAllSuppliers(); break;
        case "4": AddNewSupplier(); break;
        case "5": ViewAllOrders(); break;
        case "6": AddNewOrder(); break;
        case "7": UpdateOrder(); break;
        case "8": DeleteOrder(); break;
        case "9": 
            Console.WriteLine("Exiting application. Goodbye!"); 
            return;
        default: 
            Console.WriteLine("Invalid option. Please try again."); 
            break;
    }
    
    Console.WriteLine("\nPress any key to return to the menu...");
    Console.ReadKey();
}

// ==========================================
// PRODUCT MANAGEMENT
// ==========================================
void ViewAllProducts()
{
    Console.WriteLine("--- All Products ---");
    var lines = File.ReadAllLines(ProductsFile);
    if (lines.Length == 0) Console.WriteLine("No products found.");
    
    foreach (var line in lines)
    {
        var data = line.Split('|');
        Console.WriteLine($"ID: {data[0]} | Name: {data[1]} | Price: LKR {data[2]} | Stock: {data[3]}");
    }
}

void AddNewProduct()
{
    Console.WriteLine("--- Add New Product ---");
    int newId = GenerateNextId(ProductsFile);
    
    Console.Write("Enter Product Name: ");
    string name = Console.ReadLine() ?? "Unknown";
    
    Console.Write("Enter Price (LKR): ");
    string price = Console.ReadLine() ?? "0";
    
    Console.Write("Enter Initial Stock Quantity: ");
    string stock = Console.ReadLine() ?? "0";

    string record = $"{newId}|{name}|{price}|{stock}";
    File.AppendAllLines(ProductsFile, new[] { record });
    Console.WriteLine($"Product '{name}' added successfully with ID {newId}!");
}

// ==========================================
// SUPPLIER MANAGEMENT
// ==========================================
void ViewAllSuppliers()
{
    Console.WriteLine("--- All Suppliers ---");
    var lines = File.ReadAllLines(SuppliersFile);
    if (lines.Length == 0) Console.WriteLine("No suppliers found.");
    
    foreach (var line in lines)
    {
        var data = line.Split('|');
        Console.WriteLine($"ID: {data[0]} | Name: {data[1]} | Contact: {data[2]}");
    }
}

void AddNewSupplier()
{
    Console.WriteLine("--- Add New Supplier ---");
    int newId = GenerateNextId(SuppliersFile);
    
    Console.Write("Enter Supplier Name: ");
    string name = Console.ReadLine() ?? "Unknown";
    
    Console.Write("Enter Contact Number: ");
    string contact = Console.ReadLine() ?? "N/A";

    string record = $"{newId}|{name}|{contact}";
    File.AppendAllLines(SuppliersFile, new[] { record });
    Console.WriteLine($"Supplier '{name}' added successfully with ID {newId}!");
}

// ==========================================
// ORDER MANAGEMENT (CRUD)
// ==========================================
void ViewAllOrders()
{
    Console.WriteLine("--- All Orders ---");
    var lines = File.ReadAllLines(OrdersFile);
    if (lines.Length == 0) Console.WriteLine("No orders found.");
    
    foreach (var line in lines)
    {
        var data = line.Split('|');
        Console.WriteLine($"Order ID: {data[0]} | Customer: {data[1]} | Product ID: {data[2]} | Qty: {data[3]} | Total: LKR {data[4]} | Status: {data[5]}");
    }
}

void AddNewOrder()
{
    Console.WriteLine("--- Add New Order ---");
    ViewAllProducts(); // Show products to help user choose
    Console.WriteLine();

    Console.Write("Enter Customer Name: ");
    string customer = Console.ReadLine() ?? "Unknown";
    
    Console.Write("Enter Product ID to order: ");
    string productId = Console.ReadLine() ?? "0";

    // Lookup Product Price to calculate Total
    var products = File.ReadAllLines(ProductsFile);
    var productLine = products.FirstOrDefault(p => p.StartsWith(productId + "|"));
    
    if (productLine == null)
    {
        Console.WriteLine("Error: Product ID not found! Order cancelled.");
        return;
    }

    Console.Write("Enter Quantity: ");
    if (!int.TryParse(Console.ReadLine(), out int quantity)) quantity = 1;

    // Calculate Total (Price * Quantity)
    var productData = productLine.Split('|');
    if (double.TryParse(productData[2], out double price))
    {
        double total = price * quantity;
        int newId = GenerateNextId(OrdersFile);
        string status = "Pending";

        // Format: Id|CustomerName|ProductId|Quantity|TotalAmount|Status
        string record = $"{newId}|{customer}|{productId}|{quantity}|{total}|{status}";
        File.AppendAllLines(OrdersFile, new[] { record });
        
        Console.WriteLine($"Order created successfully! Order ID: {newId}, Total: LKR {total}");
    }
    else
    {
        Console.WriteLine("Error calculating price.");
    }
}

void UpdateOrder()
{
    Console.WriteLine("--- Update Order Status ---");
    ViewAllOrders();
    Console.WriteLine();
    
    Console.Write("Enter Order ID to update: ");
    string orderId = Console.ReadLine() ?? "";

    var lines = File.ReadAllLines(OrdersFile).ToList();
    int index = lines.FindIndex(l => l.StartsWith(orderId + "|"));

    if (index == -1)
    {
        Console.WriteLine("Order not found.");
        return;
    }

    var data = lines[index].Split('|');
    Console.WriteLine($"Current Status: {data[5]}");
    Console.WriteLine("Select new status: 1. Pending  2. Shipped  3. Delivered  4. Cancelled");
    Console.Write("Choice (1-4): ");
    
    string newStatus = Console.ReadLine() switch
    {
        "1" => "Pending",
        "2" => "Shipped",
        "3" => "Delivered",
        "4" => "Cancelled",
        _ => data[5] // keep original if invalid choice
    };

    data[5] = newStatus;
    lines[index] = string.Join("|", data);
    File.WriteAllLines(OrdersFile, lines);
    
    Console.WriteLine("Order status updated successfully!");
}

void DeleteOrder()
{
    Console.WriteLine("--- Delete Order ---");
    ViewAllOrders();
    Console.WriteLine();

    Console.Write("Enter Order ID to delete: ");
    string orderId = Console.ReadLine() ?? "";

    var lines = File.ReadAllLines(OrdersFile).ToList();
    int initialCount = lines.Count;
    
    // Keep all lines EXCEPT the one that starts with the Order ID
    lines.RemoveAll(l => l.StartsWith(orderId + "|"));

    if (lines.Count < initialCount)
    {
        File.WriteAllLines(OrdersFile, lines);
        Console.WriteLine("Order deleted successfully!");
    }
    else
    {
        Console.WriteLine("Order not found.");
    }
}

// ==========================================
// UTILITY FUNCTIONS
// ==========================================
int GenerateNextId(string filePath)
{
    var lines = File.ReadAllLines(filePath);
    if (lines.Length == 0) return 1;
    
    int maxId = 0;
    foreach (var line in lines)
    {
        if (int.TryParse(line.Split('|')[0], out int id) && id > maxId)
        {
            maxId = id;
        }
    }
    return maxId + 1;
}

void InitializeDatabase()
{
    if (!File.Exists(ProductsFile))
    {
        File.WriteAllLines(ProductsFile, new[] {
            "1|Hand-carved Wooden Elephant|2500.00|15",
            "2|Batik Wall Hanging|4500.00|8",
            "3|Traditional Dumbara Mat|3200.00|20"
        });
    }
    
    if (!File.Exists(SuppliersFile))
    {
        File.WriteAllLines(SuppliersFile, new[] {
            "1|Lakpahana Craft Supplies|0112345678",
            "2|Kandy Woodworks Ltd|0812345678"
        });
    }

    if (!File.Exists(OrdersFile))
    {
        File.WriteAllLines(OrdersFile, new[] {
            "1|Sunil Perera|1|2|5000.00|Delivered",
            "2|Nimali Fernando|2|1|4500.00|Pending"
        });
    }
}