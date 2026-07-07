# CraftConnectPOS SQLite - Runtime Required

## Requirement

Install one of the following for 64-bit Windows:

- .NET 10 Desktop Runtime (x64), or
- .NET 10 SDK (x64)

Visual Studio configured for current .NET desktop development normally
provides a compatible SDK. The base .NET Runtime or ASP.NET Core Runtime alone
is not sufficient for a WPF desktop application.

## Run the application

1. Extract the ZIP file completely.
2. Open the `Release` folder.
3. Double-click `CraftConnectPOS.exe`.

No SQL Server, MySQL, LocalDB, database scripts, or connection configuration
is required.

The application uses:

```text
Release\Data\CraftConnect.db
```

Keep the executable, supporting files, and `Data` folder together.

## Missing-runtime message

If Windows reports that a required framework is missing, install the
**.NET 10 Desktop Runtime for Windows x64**, then reopen the application.

## Moving and backing up

Copy the entire `Release` folder when moving the application to another
computer.

To back up shop data:

1. Close CraftConnectPOS.
2. Copy `Release\Data\CraftConnect.db` to a safe location.
