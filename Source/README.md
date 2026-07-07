# CraftConnectPOS - Portable SQLite Edition

CraftConnectPOS is a self-contained Windows point-of-sale and inventory
application for local craft businesses.

## For shop owners

No developer or database software is required:

- No Visual Studio
- No SQL Server or LocalDB
- No SQL Server Management Studio
- No .NET installation
- No connection configuration
- No SQL scripts

Open the `Release` folder and run:

```text
CraftConnectPOS.exe
```

On first launch, the application automatically creates:

```text
Data\CraftConnect.db
```

That single SQLite file contains accounts, products, suppliers, materials,
orders, and statistics.

## Backup

1. Close CraftConnectPOS.
2. Copy `Data\CraftConnect.db` to a USB drive or another safe location.

To restore a backup, close the application and replace the database file with
the saved copy.

## Moving the application

Copy the entire `Release` folder. Keep all files and subfolders together.
The folder must be in a location where the user can create and update files,
such as Desktop or Documents.

## Implemented modules

- Login and account creation with PBKDF2-SHA256 password hashing
- Dashboard with live metrics and low-stock information
- Product catalogue with categories, prices, stock, search, and CRUD
- Material inventory linked to suppliers
- Supplier directory with assigned-material summaries
- Orders with transactional stock reservation and restoration
- Completed-order revenue and product statistics

## Order and stock behavior

- Pending, Processing, Ready to Ship, Shipped, and Completed orders reserve
  product stock.
- Cancelling or deleting an order restores its reserved stock.
- Reactivating a cancelled order reserves stock again.
- Editing an existing order retains its historical unit price unless a
  different product is selected.

## For developers

Requirements:

- .NET 10 SDK

Build:

```powershell
dotnet build .\CraftConnectPOS.slnx -c Release
```

Run automated integration checks:

```powershell
dotnet run --project .\CraftConnectPOS.Tests\CraftConnectPOS.Tests.csproj -c Release
```

Create the standalone customer folder:

```powershell
dotnet publish .\CraftConnectPOS\CraftConnectPOS.csproj -c Release -r win-x64 --self-contained true -o .\Release
```

The application uses `Microsoft.Data.Sqlite` with an embedded SQLite native
library. The current user does not need to install any runtime or database
engine.
