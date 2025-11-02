# Copy WPF Database Manager Files
# Run from msim/temp directory

$ErrorActionPreference = "Stop"

Write-Host "Copying WPF Database Manager Files..." -ForegroundColor Cyan
Write-Host ""

# Check location
if (-not (Test-Path "WPF_DB\ViewModels")) {
    Write-Host "ERROR: Run from msim\temp folder" -ForegroundColor Red
    exit 1
}

# Copy ViewModels
Write-Host "Copying ViewModels..." -ForegroundColor Green
Copy-Item "WPF_DB\ViewModels\*.cs" "..\ManufacturingSimulation.WPF\ViewModels\" -Force

# Copy Views
Write-Host "Copying Views..." -ForegroundColor Green
Copy-Item "WPF_DB\Views\*" "..\ManufacturingSimulation.WPF\Views\" -Force

Write-Host ""
Write-Host "✓ Files copied!" -ForegroundColor Green
Write-Host ""
Write-Host "Next:" -ForegroundColor Yellow
Write-Host "1. cd ..\ManufacturingSimulation.WPF" -ForegroundColor White
Write-Host "2. dotnet add reference ..\ManufacturingSimulation.Database" -ForegroundColor White
Write-Host "3. dotnet add reference ..\ManufacturingSimulation.Bridge" -ForegroundColor White
Write-Host "4. Copy-Item '..\ManufacturingSimulation.Database\mes_training.db' '.'" -ForegroundColor White
Write-Host "5. cd .." -ForegroundColor White
Write-Host "6. dotnet build" -ForegroundColor White
