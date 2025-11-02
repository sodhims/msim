# MES Database Quick Start

## Create Database (Choose One Method)

### Method 1: PowerShell Script (Easiest)
```powershell
cd ManufacturingSimulation.Database\SQL
.\setup_database.ps1
```

### Method 2: Manual Commands
```bash
cd ManufacturingSimulation.Database\SQL
sqlite3 mes_training.db < 01_create_mes_database.sql
sqlite3 mes_training.db < 02_create_simulation_tables.sql
sqlite3 mes_training.db < 03_insert_sample_data.sql
```

### Method 3: DB Browser (GUI)
1. Download: https://sqlitebrowser.org/
2. New Database → `mes_training.db`
3. Execute SQL → Run each .sql file in order

### Method 4: Let EF Core Create It
```csharp
using var db = new MesDbContext();
db.Database.EnsureCreated();
// Then run SQL scripts for sample data
```

## Test Database

```csharp
using var db = new MesDbContext();

// Check work centers
var wcs = db.WorkCenters.ToList();
Console.WriteLine($"Work Centers: {wcs.Count}"); // Should be 4

// Check orders
var orders = db.ProductionOrders.ToList();
Console.WriteLine($"Orders: {orders.Count}"); // Should be 5

// Run simulation
var simService = new SimulationService(db);
var result = simService.QuickRun(1, new List<int> {1,2,3}, 168);
Console.WriteLine($"Throughput: {result.Statistics.Throughput:F2}");
```

## Sample Data Included

**Students:** 2 (demo, instructor)
**Work Centers:** 4 (PCB Assembly, Testing, Packaging, Inspection)
**Products:** 3 (IoT Sensor, Controller, Display)
**Orders:** 5 production orders
**Routings:** Complete process flows

## Database Location

Copy `mes_training.db` to:
- Your project root, OR
- `bin/Debug/net8.0/`, OR
- Update connection string in `MesDbContext.cs`
