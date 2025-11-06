# Quick Implementation Checklist

## Phase 1: Enhanced Simulation Engine - Ready to Install!

### Files Ready to Download:
✅ [Part_Enhanced.cs](computer:///mnt/user-data/outputs/Part_Enhanced.cs)
✅ [Machine_Enhanced.cs](computer:///mnt/user-data/outputs/Machine_Enhanced.cs)
✅ [SimulationEngine_Enhanced.cs](computer:///mnt/user-data/outputs/SimulationEngine_Enhanced.cs)
✅ [SimulationEventLogger_Enhanced.cs](computer:///mnt/user-data/outputs/SimulationEventLogger_Enhanced.cs)
✅ [MesToSimulationMapper_Enhanced.cs](computer:///mnt/user-data/outputs/MesToSimulationMapper_Enhanced.cs)
✅ [SetupCompleteEvent.cs](computer:///mnt/user-data/outputs/SetupCompleteEvent.cs)
✅ [IMPLEMENTATION_GUIDE.md](computer:///mnt/user-data/outputs/IMPLEMENTATION_GUIDE.md)

---

## Installation Steps:

### 1. Backup Current Files
Create backups of:
- `ManufacturingSimulation.Core/Models/Part.cs`
- `ManufacturingSimulation.Core/Models/Machine.cs`
- `ManufacturingSimulation.Core/SimulationEngine.cs`
- `ManufacturingSimulation.Bridge/SimulationEventLogger.cs`
- `ManufacturingSimulation.Bridge/MesToSimulationMapper.cs`

### 2. Replace Core Files
1. Replace `Part.cs` with `Part_Enhanced.cs` (rename to `Part.cs`)
2. Replace `Machine.cs` with `Machine_Enhanced.cs` (rename to `Machine.cs`)
3. Replace `SimulationEngine.cs` with `SimulationEngine_Enhanced.cs` (rename to `SimulationEngine.cs`)

### 3. Add New Event
1. Create folder: `ManufacturingSimulation.Core/Engine/Events/` (if doesn't exist)
2. Add `SetupCompleteEvent.cs` to this folder

### 4. Replace Bridge Files
1. Replace `SimulationEventLogger.cs` with `SimulationEventLogger_Enhanced.cs` (rename)
2. Replace `MesToSimulationMapper.cs` with `MesToSimulationMapper_Enhanced.cs` (rename)

### 5. Update SimulationService.cs
In the constructor or where mapper is created, pass the random seed:

```csharp
// OLD:
_mapper = new MesToSimulationMapper();

// NEW:
_mapper = new MesToSimulationMapper(scenario.RandomSeed);
```

### 6. Build Solution
Press **Ctrl+Shift+B**

Check for errors. Most common:
- Missing `using` statements
- Namespace mismatches

### 7. Test Run
1. Run your WPF app
2. Open Simulation Runner
3. Select a production order
4. Run simulation
5. Check for errors

### 8. Verify Events Logged
1. Open Database Admin or DBeaver
2. Look at `simulation_events` table
3. Filter by your recent RunId
4. Look for new event types:
   - **"Setup Start"**
   - **"Setup Complete"**
   - **"Start Processing"**
   - **"End Processing"**
   - **"Buffer Pass"** (if you have buffer-only operations)

---

## What You'll See:

### Old Behavior:
```
[0.0] Part-1 arrived
[0.0] Start Processing on Drill Press
[15.0] End Processing on Drill Press
```

### New Behavior:
```
[0.0] Part-1 arrived
[0.0] Setup Start on Drill Press (5.0 min)
[5.0] Setup Complete on Drill Press
[5.0] Start Processing on Drill Press
[15.0] End Processing on Drill Press (10.0 min)
```

---

## Testing Scenarios:

### Test 1: Basic Routing
Use Database Admin to create a routing:
- Product: Any
- Work Center: Drill Press
- `setup_time_minutes`: 5
- `cycle_time_minutes`: 10
- Leave all distribution fields NULL
- `batch_size`: 1

**Expected:** Part has 5 min setup, 10 min processing

### Test 2: Distribution
Edit the routing:
- `setup_time_mean`: 5
- `setup_time_std_dev`: 1
- `setup_time_distribution`: "Normal"

**Expected:** Setup time varies around 5 minutes

### Test 3: Buffer Operation
Edit the routing:
- `is_buffer_only`: 1 (true)
- `setup_time_minutes`: 0
- `cycle_time_minutes`: 0

**Expected:** Part passes through instantly with "Buffer Pass" event

### Test 4: Batch
Edit the routing:
- `batch_size`: 5
- `setup_time_minutes`: 10
- `cycle_time_minutes`: 2

**Expected:** 10 min setup, then 10 min processing (2 min × 5 parts)

---

## Troubleshooting:

### Build Errors
**"SetupCompleteEvent not found"**
→ Make sure you added `SetupCompleteEvent.cs` to Engine/Events folder

**"OperationTiming not found"**
→ Make sure you're using the new `Part_Enhanced.cs`

### Runtime Errors
**"Method not found: HandleSetupComplete"**
→ Make sure you replaced `SimulationEngine.cs`

**Parts not processing**
→ Check debug log at `C:\msim\logs\debug.txt`

### No New Events in Database
**Check:**
1. Is `_eventLogger` set? (should be called in SimulationService)
2. Are the new logging methods being called?
3. Check debug log for "Setup Start" messages

---

## Next Phase Preview:

Once this works, we'll build:

### Phase 2: Gantt Chart Window
- Query simulation_events
- Show timeline with:
  - Orange bars = Setup
  - Blue bars = Processing
  - One row per machine
  - X-axis = time

### Phase 3: Enhanced Routing Management
- Add distribution type dropdowns
- Add mean/std dev inputs
- Add batch size controls
- Add buffer-only checkbox

---

## Current Status:
✅ Database schema updated (routing table has new columns)
✅ Core models enhanced (Part, Machine)
✅ Engine updated (SimulationEngine with setup/operation phases)
✅ Logging enhanced (separate setup events)
✅ Mapper updated (reads routing data with distributions)

**READY TO IMPLEMENT!**

---

**Questions or issues? Just let me know which step you're on!**
