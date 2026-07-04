# CraftConnectPOS

CraftConnectPOS is a modern, responsive WPF point-of-sale (POS) and inventory management frontend designed for craft businesses. It targets .NET Framework 4.8 and utilizes the MVVM architecture to structure its presentation layer.

## Key Frontend Modules

- **Login & Signup**: Secured entry point supporting client-side user accounts, password visibility toggling, validation notifications, and accessibility labels.
- **Dashboard**: High-level operational overview including search-capable recent customer orders list, low-stock notifications, and core business metrics.
- **Product Management**: Catalogue for managing products, including price tracking, category organization, search filtering, and CRUD operations.
- **Material Inventory**: Track raw materials, quantities, unit types, and supplier associations, complete with status indicators (In Stock, Low Stock, Out of Stock).
- **Suppliers Directory**: Maintain supplier contacts, locations, and track materials supplied.
- **Customer Orders**: Plan and manage active customer orders, automatically calculating price totals based on product selections and quantities.
- **Statistics & Revenue**: Visual charts, revenue logs, and category breakdowns.

## Design & Aesthetics

The UI features a premium design system built with a clean green-mint palette, serif/sans hybrid typography (Georgia and Segoe UI), glassmorphism cards, and interactive hover/pressed button states.

## Build and Run

To build the project:

```powershell
dotnet build .\CraftConnectPOS\CraftConnectPOS.csproj -c Debug
```

## Mock Data

The frontend relies on an in-memory mock data service (`MockDataStore`). All creations, modifications, and deletions are session-only and will reset when the application restarts.

