# Install-FactoryLayout.ps1
# Copies Factory Layout files to correct project directories

param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectRoot
)

# Validate project root exists
if (-not (Test-Path $ProjectRoot)) {
    Write-Error "Project root not found: $ProjectRoot"
    exit 1
}

Write-Host "Installing Factory Layout Editor files..." -ForegroundColor Green
Write-Host "Project Root: $ProjectRoot" -ForegroundColor Cyan

# Define source directory (where you extracted the ZIP)
$SourceDir = Read-Host "Enter path to extracted ZIP files (e.g., C:\Downloads\FactoryLayout)"

if (-not (Test-Path $SourceDir)) {
    Write-Error "Source directory not found: $SourceDir"
    exit 1
}

# Create directories if they don't exist
$WPFDir = Join-Path $ProjectRoot "ManufacturingSimulation.WPF"
$ViewModelsDir = Join-Path $WPFDir "ViewModels"
$UtilitiesDir = Join-Path $WPFDir "Utilities"
$ViewsDir = Join-Path $WPFDir "Views"

Write-Host "`nCreating directories..." -ForegroundColor Yellow

if (-not (Test-Path $ViewModelsDir)) {
    New-Item -ItemType Directory -Path $ViewModelsDir -Force | Out-Null
    Write-Host "  Created: ViewModels/" -ForegroundColor Green
}

if (-not (Test-Path $UtilitiesDir)) {
    New-Item -ItemType Directory -Path $UtilitiesDir -Force | Out-Null
    Write-Host "  Created: Utilities/" -ForegroundColor Green
}

# Copy files
Write-Host "`nCopying files..." -ForegroundColor Yellow

# 1. FactoryLayoutView.xaml (NEW)
$sourceFile = Join-Path $SourceDir "FactoryLayoutView.xaml"
$destFile = Join-Path $ViewsDir "FactoryLayoutView.xaml"
if (Test-Path $sourceFile) {
    Copy-Item $sourceFile $destFile -Force
    Write-Host "  Copied: Views/FactoryLayoutView.xaml" -ForegroundColor Green
} else {
    Write-Warning "  NOT FOUND: FactoryLayoutView.xaml"
}

# 2. FactoryLayoutView.xaml.cs
$sourceFile = Join-Path $SourceDir "FactoryLayoutView.xaml.cs"
$destFile = Join-Path $ViewsDir "FactoryLayoutView.xaml.cs"
if (Test-Path $sourceFile) {
    Copy-Item $sourceFile $destFile -Force
    Write-Host "  Copied: Views/FactoryLayoutView.xaml.cs" -ForegroundColor Green
} else {
    Write-Warning "  NOT FOUND: FactoryLayoutView.xaml.cs"
}

# 3. FactoryLayoutViewModel.cs (NEW)
$sourceFile = Join-Path $SourceDir "FactoryLayoutViewModel.cs"
$destFile = Join-Path $ViewModelsDir "FactoryLayoutViewModel.cs"
if (Test-Path $sourceFile) {
    Copy-Item $sourceFile $destFile -Force
    Write-Host "  Copied: ViewModels/FactoryLayoutViewModel.cs" -ForegroundColor Green
} else {
    Write-Warning "  NOT FOUND: FactoryLayoutViewModel.cs"
}

# 4. DistanceCalculator.cs (NEW)
$sourceFile = Join-Path $SourceDir "DistanceCalculator.cs"
$destFile = Join-Path $UtilitiesDir "DistanceCalculator.cs"
if (Test-Path $sourceFile) {
    Copy-Item $sourceFile $destFile -Force
    Write-Host "  Copied: Utilities/DistanceCalculator.cs" -ForegroundColor Green
} else {
    Write-Warning "  NOT FOUND: DistanceCalculator.cs"
}

# 5. Optional: FactoryLayoutExample.cs
$sourceFile = Join-Path $SourceDir "FactoryLayoutExample.cs"
if (Test-Path $sourceFile) {
    $ExamplesDir = Join-Path $ProjectRoot "Examples"
    if (-not (Test-Path $ExamplesDir)) {
        New-Item -ItemType Directory -Path $ExamplesDir -Force | Out-Null
    }
    $destFile = Join-Path $ExamplesDir "FactoryLayoutExample.cs"
    Copy-Item $sourceFile $destFile -Force
    Write-Host "  Copied: Examples/FactoryLayoutExample.cs (optional)" -ForegroundColor Green
}

Write-Host "`n=== Installation Complete ===" -ForegroundColor Green
Write-Host "`nNext steps:" -ForegroundColor Cyan
Write-Host "1. Update FactoryLayoutView.xaml DataContext:"
Write-Host "   <UserControl.DataContext>"
Write-Host "       <vm:FactoryLayoutViewModel/>"
Write-Host "   </UserControl.DataContext>"
Write-Host ""
Write-Host "2. Add namespace to XAML:"
Write-Host "   xmlns:vm=`"clr-namespace:ManufacturingSimulation.ViewModels`""
Write-Host ""
Write-Host "3. Build solution: dotnet build"
Write-Host "4. Run application: dotnet run"