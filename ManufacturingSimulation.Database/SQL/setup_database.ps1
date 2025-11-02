# MES Training Database Setup Script
# Creates SQLite database with all tables and sample data

param(
    [string]$DatabasePath = "mes_training.db",
    [switch]$Force
)

Write-Host "==================================" -ForegroundColor Cyan
Write-Host "MES Training Database Setup" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

# Check if database exists
if (Test-Path $DatabasePath) {
    if ($Force) {
        Write-Host "Removing existing database..." -ForegroundColor Yellow
        Remove-Item $DatabasePath -Force
    } else {
        Write-Host "Database already exists: $DatabasePath" -ForegroundColor Red
        Write-Host "Use -Force to overwrite" -ForegroundColor Yellow
        exit 1
    }
}

# Check for sqlite3
$sqlite3 = Get-Command sqlite3 -ErrorAction SilentlyContinue

if (-not $sqlite3) {
    Write-Host "ERROR: sqlite3 not found in PATH" -ForegroundColor Red
    Write-Host ""
    Write-Host "Options:" -ForegroundColor Yellow
    Write-Host "1. Install SQLite: https://www.sqlite.org/download.html" -ForegroundColor White
    Write-Host "2. Use DB Browser for SQLite: https://sqlitebrowser.org/" -ForegroundColor White
    Write-Host "3. Let Entity Framework create it automatically" -ForegroundColor White
    exit 1
}

# Run SQL scripts
Write-Host "Creating MES database schema..." -ForegroundColor Green
& sqlite3 $DatabasePath ".read SQL/01_create_mes_database.sql"

Write-Host "Adding simulation tables..." -ForegroundColor Green
& sqlite3 $DatabasePath ".read SQL/02_create_simulation_tables.sql"

Write-Host "Inserting sample data..." -ForegroundColor Green
& sqlite3 $DatabasePath ".read SQL/03_insert_sample_data.sql"

# Verify
Write-Host ""
Write-Host "Verifying database..." -ForegroundColor Cyan
$tableCount = & sqlite3 $DatabasePath "SELECT COUNT(*) FROM sqlite_master WHERE type='table';"
Write-Host "Tables created: $tableCount" -ForegroundColor White

$studentCount = & sqlite3 $DatabasePath "SELECT COUNT(*) FROM students;"
Write-Host "Students: $studentCount" -ForegroundColor White

$wcCount = & sqlite3 $DatabasePath "SELECT COUNT(*) FROM work_centers;"
Write-Host "Work Centers: $wcCount" -ForegroundColor White

$orderCount = & sqlite3 $DatabasePath "SELECT COUNT(*) FROM production_orders;"
Write-Host "Production Orders: $orderCount" -ForegroundColor White

Write-Host ""
Write-Host "==================================" -ForegroundColor Green
Write-Host "Database created successfully!" -ForegroundColor Green
Write-Host "==================================" -ForegroundColor Green
Write-Host ""
Write-Host "Database file: $DatabasePath" -ForegroundColor Cyan
Write-Host ""
Write-Host "Test login:" -ForegroundColor Yellow
Write-Host "  Username: demo" -ForegroundColor White
Write-Host "  Password: demo_hash_12345" -ForegroundColor White
