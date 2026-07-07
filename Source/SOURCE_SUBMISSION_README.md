# CraftConnectPOS Source Submission

This ZIP contains the complete SQLite source code, UI resources, database
schema, and automated integration tests. It intentionally excludes generated
build output (`bin`, `obj`, `.vs`), the standalone customer release, Git
history, and live SQLite database files.

Nothing required to build or inspect the application has been removed.

## Open and build

1. Extract the ZIP.
2. Open `CraftConnectPOS.slnx` in Visual Studio with .NET desktop development
   support, or use the .NET 10 SDK.
3. Build with:

   ```powershell
   dotnet build .\CraftConnectPOS.slnx -c Release
   ```

## Run all automated checks

```powershell
dotnet run --project .\CraftConnectPOS.Tests\CraftConnectPOS.Tests.csproj -c Release
```

The test runner uses a temporary SQLite database and removes it afterward. It
does not modify the application's normal database.

## Create the customer-ready application

```powershell
dotnet publish .\CraftConnectPOS\CraftConnectPOS.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\Release
```

For end-user operation, backup, and maintenance instructions, see `README.md`.
