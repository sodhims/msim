# 🚀 Factory Layout Installation Checklist

## ✅ Step-by-Step Installation

### Before You Start
- [ ] Manufacturing Simulation project open in Visual Studio
- [ ] Database schema already applied (machine_locations, machine_distances tables)
- [ ] FactoryLayoutView.xaml already created

---

## 📂 Step 1: Extract Files

**From the ZIP package, add these files to your project:**

```
ManufacturingSimulation/
├── Views/
│   └── FactoryLayoutView.xaml.cs       ← REPLACE existing file
│
├── ViewModels/                         ← CREATE folder if needed
│   └── FactoryLayoutViewModel.cs       ← ADD new file
│
├── Utilities/                          ← CREATE folder if needed
│   └── DistanceCalculator.cs           ← ADD new file
│
└── Examples/                           ← OPTIONAL folder
    └── FactoryLayoutExample.cs         ← OPTIONAL examples file
```

### File Locations in Visual Studio:

1. **FactoryLayoutView.xaml.cs**
   - Right-click `Views` folder
   - "Add" → "Existing Item"
   - Select `FactoryLayoutView.xaml.cs`
   - ⚠️ **IMPORTANT**: This will REPLACE the auto-generated code-behind

2. **FactoryLayoutViewModel.cs**
   - Right-click project root
   - "Add" → "New Folder" → Name it `ViewModels`
   - Right-click `ViewModels` folder
   - "Add" → "Existing Item"
   - Select `FactoryLayoutViewModel.cs`

3. **DistanceCalculator.cs**
   - Right-click project root
   - "Add" → "New Folder" → Name it `Utilities`
   - Right-click `Utilities` folder
   - "Add" → "Existing Item"
   - Select `DistanceCalculator.cs`

---

## 🔧 Step 2: Update XAML

Open `FactoryLayoutView.xaml` and add the ViewModel reference:

```xml
<UserControl x:Class="ManufacturingSimulation.Views.FactoryLayoutView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="clr-namespace:ManufacturingSimulation.ViewModels"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             mc:Ignorable="d"
             d:DesignHeight="600" d:DesignWidth="1200">
    
    <!-- ADD THIS SECTION -->
    <UserControl.DataContext>
        <vm:FactoryLayoutViewModel/>
    </UserControl.DataContext>
    
    <!-- Rest of your existing XAML -->
</UserControl>
```

**Key changes:**
- Add `xmlns:vm="clr-namespace:ManufacturingSimulation.ViewModels"` namespace
- Add `<UserControl.DataContext>` section

---

## 🏗️ Step 3: Verify Project Structure

Your Solution Explorer should now look like:

```
ManufacturingSimulation
├── 📁 Database
│   ├── MesDbContext.cs
│   └── 📁 Models
│       ├── Machine.cs
│       ├── MachineLocation.cs
│       └── MachineDistance.cs
├── 📁 Views
│   ├── FactoryLayoutView.xaml
│   └── FactoryLayoutView.xaml.cs        ✅ REPLACED
├── 📁 ViewModels
│   └── FactoryLayoutViewModel.cs        ✅ NEW
├── 📁 Utilities
│   └── DistanceCalculator.cs            ✅ NEW
└── 📁 Examples (optional)
    └── FactoryLayoutExample.cs
```

---

## 🔌 Step 4: Check Dependencies

Ensure these NuGet packages are installed:

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="7.0+" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="7.0+" />
```

**To check:**
- Right-click project → "Manage NuGet Packages"
- Look for EntityFrameworkCore packages
- Install if missing

---

## 🗄️ Step 5: Verify Database Schema

Your database should have these tables:

```sql
-- Check tables exist
SELECT name FROM sqlite_master 
WHERE type='table' 
AND name IN ('machines', 'machine_locations', 'machine_distances');
```

**Expected output:**
```
machines
machine_locations
machine_distances
```

If tables are missing, apply the schema:
```powershell
Get-Content enhanced_mes_schema.sql | sqlite3 mes_training.db
```

---

## 🏃 Step 6: Build and Run

1. **Clean Build**
   ```
   Build → Clean Solution
   Build → Build Solution
   ```

2. **Fix Any Errors**
   - Check namespace matches: `ManufacturingSimulation.ViewModels`
   - Verify all files are included in project
   - Ensure using statements are correct

3. **Run Application**
   ```
   Debug → Start Debugging (F5)
   ```

---

## 🧪 Step 7: Test Basic Functionality

### Test 1: Application Starts
- [ ] Application launches without errors
- [ ] Factory Layout tab is visible
- [ ] Canvas is displayed

### Test 2: Load Machines
- [ ] Machine list populates from database
- [ ] Machines are visible in left panel

### Test 3: Drag-Drop
- [ ] Drag machine from list to canvas
- [ ] Machine appears on canvas
- [ ] Can move machine on canvas

### Test 4: Distance Calculation
- [ ] Add 2+ machines to canvas
- [ ] Click "Calculate Distances" button
- [ ] Distance matrix populates
- [ ] Distances display in bottom panel

### Test 5: Save/Load
- [ ] Click "Save Layout" button
- [ ] Close application
- [ ] Reopen application
- [ ] Click "Load Layout" button
- [ ] Machines appear in saved positions

---

## 🐛 Troubleshooting

### Problem: Build Errors

**Error:** `The type or namespace 'ViewModels' could not be found`

**Solution:**
1. Check FactoryLayoutViewModel.cs namespace: `namespace ManufacturingSimulation.ViewModels`
2. Verify file is in ViewModels folder
3. Rebuild project

---

**Error:** `'MachineLocationModel' does not exist`

**Solution:**
- MachineLocationModel is defined in FactoryLayoutViewModel.cs
- Check file was added correctly
- Rebuild project

---

**Error:** `DbContext errors`

**Solution:**
1. Verify EntityFrameworkCore NuGet packages installed
2. Check connection string in MesDbContext.cs
3. Ensure database file exists

---

### Problem: Runtime Errors

**Error:** "Table not found"

**Solution:**
```powershell
# Check database
sqlite3 mes_training.db ".tables"

# If missing, apply schema
Get-Content enhanced_mes_schema.sql | sqlite3 mes_training.db
```

---

**Error:** "No machines loaded"

**Solution:**
```sql
-- Check machines table has data
SELECT * FROM machines;

-- If empty, insert sample machines
INSERT INTO machines (name, machine_type, status, buffer_capacity)
VALUES 
('CNC Mill', 'Milling', 'Available', 5),
('Lathe', 'Turning', 'Available', 3);
```

---

### Problem: UI Issues

**Issue:** Canvas not visible

**Solution:**
- Check XAML Canvas has Width/Height set
- Verify Canvas is not collapsed
- Check Grid layout is correct

---

**Issue:** Drag-drop not working

**Solution:**
- Verify AllowDrop="True" on Canvas
- Check PreviewMouseLeftButtonDown events are wired
- Ensure code-behind was replaced (not merged)

---

## 📋 Verification Checklist

Before moving to next phase, verify:

- [ ] All files added to project
- [ ] Project builds without errors
- [ ] Application starts successfully
- [ ] Machine list populates
- [ ] Drag-drop from list to canvas works
- [ ] Move machines on canvas works
- [ ] Distance calculation works
- [ ] Save layout works
- [ ] Load layout works
- [ ] Context menu works (Remove, Rotate)
- [ ] Grid toggle works
- [ ] Auto-arrange works

---

## 🎯 Success Criteria

✅ **Installation is successful when:**

1. Application runs without errors
2. You can drag a machine to the canvas
3. You can move machines around
4. Distance matrix calculates correctly
5. Layout saves and loads from database

**Time to complete: 15-30 minutes**

---

## 📞 Getting Help

If you encounter issues:

1. **Check this checklist** - Most issues covered above
2. **Review code comments** - Files are heavily documented
3. **Check FACTORY_LAYOUT_IMPLEMENTATION.md** - Detailed guide
4. **Review FactoryLayoutExample.cs** - Working examples

---

## 🎉 Next Steps

Once installation is complete:

1. **Explore Features**
   - Try all toolbar buttons
   - Test context menus
   - Experiment with different layouts

2. **Review Examples**
   - Run FactoryLayoutExample.cs
   - Understand distance calculations
   - Learn layout optimization

3. **Integrate with Simulation**
   - Use distance matrix in simulation engine
   - Add material handling delays
   - Visualize part movement

4. **Create Teaching Materials**
   - Lab exercises for students
   - Assignments on layout design
   - Real factory case studies

---

**🏆 Congratulations! Your Factory Layout Editor is ready to use! 🎊**

---

*Installation Guide v1.0*  
*Part of Manufacturing Simulation Teaching Project*
