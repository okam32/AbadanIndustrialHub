$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "..")
Write-Host "Checking .NET 8 SDK..."
dotnet --version
if ($LASTEXITCODE -ne 0) { throw "Install .NET 8 SDK first." }
dotnet restore .\AbadanIndustrialHub.sln
if ($LASTEXITCODE -ne 0) { throw "Restore failed." }
dotnet build .\AbadanIndustrialHub.sln
if ($LASTEXITCODE -ne 0) { throw "Build failed." }
dotnet tool update --global dotnet-ef --version 8.0.33
if ($LASTEXITCODE -ne 0) { throw "EF tool installation failed." }
# First run only: generate initial migration (requires a working SQL Server connection).
if (!(Test-Path .\AbadanHub.Api\Migrations)) {
    dotnet ef migrations add InitialCreate --project .\AbadanHub.Api --startup-project .\AbadanHub.Api
    if ($LASTEXITCODE -ne 0) { throw "Migration creation failed." }
}
dotnet ef database update --project .\AbadanHub.Api --startup-project .\AbadanHub.Api
if ($LASTEXITCODE -ne 0) { throw "Database update failed." }
Write-Host "Setup completed. Run: dotnet run --project .\AbadanHub.Api --launch-profile https"
