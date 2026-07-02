# CraftConnectPOS

Offline WPF point-of-sale and inventory application targeting .NET Framework 4.8.

## Build and test

```powershell
dotnet build .\CraftConnectPOS.slnx -c Debug
dotnet test .\CraftConnectPOS.Tests\CraftConnectPOS.Tests.csproj -c Debug
```

The x64 executable is written under
`CraftConnectPOS\bin\x64\Debug\net48\win-x64`.

## Local data

The application creates its SQLite database at
`%LOCALAPPDATA%\CraftConnectPOS\craftconnect.db`.

Demonstration login: `admin` / `demo123`.
