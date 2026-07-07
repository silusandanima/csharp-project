# CraftConnectPOS SQLite Package

## Run immediately

1. Extract the ZIP file.
2. Open the `Release` folder.
3. Double-click `CraftConnectPOS.exe`.

No Visual Studio, .NET installation, SQL Server, MySQL, database scripts, or
connection configuration is required. This build supports 64-bit Windows.

Keep `CraftConnectPOS.exe` and the `Data` folder together. The file
`Data\CraftConnect.db` stores accounts, products, suppliers, inventory,
orders, and statistics.

## Move to another computer

Close the application, then copy the entire `Release` folder to the other
computer. Do not run the application from inside the ZIP file.

## Backup

Close the application and copy `Release\Data\CraftConnect.db` to a safe
location. Restore it by closing the application and replacing that file.

## Source code

The clean source is in the `Source` folder. Generated build directories and
duplicate executables are intentionally excluded.
