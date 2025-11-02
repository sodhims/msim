# 📦 WPF Database Manager - Deployment Guide

## 🎯 What You're Installing

Complete database management UI:
- Order Management - Add/edit/delete orders
- Simulation Runner - Run simulations
- Results Viewer - View predictions

## 📁 Files Created (10 files)

**ViewModels/**
- OrderManagementViewModel.cs
- SimulationRunnerViewModel.cs
- SimulationResultsViewModel.cs

**Views/**
- DatabaseMenuWindow.xaml + .cs
- OrderManagementWindow.xaml + .cs
- OrderEditDialog.xaml + .cs
- SimulationRunnerWindow.xaml + .cs
- SimulationResultsDialog.xaml + .cs

## 🚀 Quick Install

### From msim/temp:

```powershell
# Copy ViewModels
Copy-Item "WPF_DB\ViewModels\*.cs" "..\ManufacturingSimulation.WPF\ViewModels\" -Force

# Copy Views
Copy-Item "WPF_DB\Views\*" "..\ManufacturingSimulation.WPF\Views\" -Force

# Add references
cd ..\ManufacturingSimulation.WPF
dotnet add reference ..\ManufacturingSimulation.Database
dotnet add reference ..\ManufacturingSimulation.Bridge

# Copy database
Copy-Item "..\ManufacturingSimulation.Database\mes_training.db" "."

# Build
cd ..
dotnet build
```

## 🎮 Launch

**Edit App.xaml StartupUri:**
```xml
<Application.StartupUri>Views/DatabaseMenuWindow.xaml</Application.StartupUri>
```

**Then run:**
```powershell
cd ManufacturingSimulation.WPF
dotnet run
```

## ✅ Features

- ✅ CRUD operations for orders
- ✅ Run simulations from database
- ✅ View bottleneck analysis
- ✅ Order completion predictions
- ✅ Past run history

## 📊 Usage

1. **Manage Orders** - Add/edit production orders
2. **Run Simulation** - Select orders, click run
3. **View Results** - See predictions and bottlenecks

Complete!
