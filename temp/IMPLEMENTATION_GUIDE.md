# Phase 1: Enhanced Simulation Engine - Implementation Guide

## Overview
This update adds support for:
- ✅ Setup time tracking (separate from operation time)
- ✅ Reading setup/cycle times from Routing table
- ✅ Distribution support (Normal, Uniform, Exponential, Constant)
- ✅ Batch processing
- ✅ Buffer-only operations (zero processing time)
- ✅ Detailed event logging for Gantt charts

---

## Files to Replace

### 1. **Part.cs** → Replace with **Part_Enhanced.cs**
**Location:** `ManufacturingSimulation.Core/Models/Part.cs`

**Key Changes:**
- Added `PartState.InSetup` state
- Added `OperationTiming` list to store setup/cycle times per operation
- Added current operation tracking (setup time, cycle time, batch size)
- Added `SetCurrentOperationTiming()` method

**Action:** 
1. Backup your current `Part.cs`
2. Replace with `Part_Enhanced.cs` (rename to `Part.cs`)

---

### 2. **Machine.cs** → Replace with **Machine_Enhanced.cs**
**Location:** `ManufacturingSimulation.Core/Models/Machine.cs`

**Key Changes:**
- Added `MachineState.Setup` state
- Added `StartSetup()` method
- Added `CompleteSetup()` method
- Modified `StartProcessing()` to handle transition from setup
- Added `GetCurrentPhase()` helper method

**Action:**
1. Backup your current `Machine.cs`
2. Replace with `Machine_Enhanced.cs` (rename to `Machine.cs`)

---

### 3. **SimulationEventLogger.cs** → Replace with **SimulationEventLogger_Enhanced.cs**
**Location:** `ManufacturingSimulation.Bridge/SimulationEventLogger.cs`

**Key Changes:**
- Added `LogSetupStart()` method
- Added `LogSetupComplete()` method
- Added `LogBufferPass()` method (for buffer-only operations)
- Added `LogBatchProcessing()` method

**Action:**
1. Backup your current `SimulationEventLogger.cs`
2. Replace with `SimulationEventLogger_Enhanced.cs` (rename to `SimulationEventLogger.cs`)

---

### 4. **MesToSimulationMapper.cs** → Replace with **MesToSimulationMapper_Enhanced.cs**
**Location:** `ManufacturingSimulation.Bridge/MesToSimulationMapper.cs`

**Key Changes:**
- Constructor now takes optional random seed
- `MapToParts()` now creates `OperationTiming` list from routing data
- Added `CreateOperationTimings()` method
- Added `SampleSetupTime()` and `SampleCycleTime()` methods
- Added `SampleFromDistribution()` with Normal, Uniform, Exponential support
- Added `SampleNormal()` for Box-Muller transform

**Action:**
1. Backup your current `MesToSimulationMapper.cs`
2. Replace with `MesToSimulationMapper_Enhanced.cs` (rename to `MesToSimulationMapper.cs`)

---

### 5. **SetupCompleteEvent.cs** → ADD NEW FILE
**Location:** `ManufacturingSimulation.Core/Engine/Events/SetupCompleteEvent.cs`

**This is a NEW event type!**

**Action:**
1. Create new file in `/Engine/Events/` folder
2. Copy contents from `SetupCompleteEvent.cs`

---

### 6. **SimulationEngine.cs** → MAJOR UPDATES REQUIRED

This is the most complex change. I'll create a detailed modification guide separately.

**Key additions needed:**
- `HandleSetupComplete()` method
- Modified `TryStartProcessing()` to handle setup phase
- Modified `HandleProcessingComplete()` to log both setup and operation

**I'll create this in the next file...**

---

## Step-by-Step Implementation

### Step 1: Update Core Models (Safest first)
1. Replace `Part.cs`
2. Replace `Machine.cs`
3. Add `SetupCompleteEvent.cs`
4. **Build the Core project** - it should compile successfully

### Step 2: Update Bridge Layer
1. Replace `SimulationEventLogger.cs`
2. Replace `MesToSimulationMapper.cs`
3. Update `SimulationService.cs` to pass seed to mapper:
```csharp
// In SimulationService.cs constructor or method
_mapper = new MesToSimulationMapper(randomSeed);
```
4. **Build the Bridge project** - check for errors

### Step 3: Update SimulationEngine (Most Complex)
See separate guide: `SIMULATION_ENGINE_UPDATES.md`

### Step 4: Test
1. Run a simple simulation
2. Check simulation_events table for new event types:
   - "Setup Start"
   - "Setup Complete"
   - "Buffer Pass"
3. Verify timing is reasonable

---

## Database Changes (Already Complete)

✅ Routing table has these columns:
- `setup_time_mean`
- `setup_time_std_dev`
- `setup_time_distribution`
- `cycle_time_std_dev`
- `cycle_time_distribution`
- `batch_size`
- `is_buffer_only`

---

## Testing Checklist

### Test 1: Simple Routing (No Distributions)
Create a routing with:
- setup_time_minutes = 5
- cycle_time_minutes = 10
- batch_size = 1
- All distribution fields = NULL

Expected: Works like before, but logs separate setup and operation events

### Test 2: Normal Distribution
- setup_time_mean = 5
- setup_time_std_dev = 1
- setup_time_distribution = "Normal"
- cycle_time_minutes = 10
- cycle_time_distribution = "Constant"

Expected: Setup times vary around 5 minutes, cycle time constant at 10

### Test 3: Buffer Operation
- is_buffer_only = 1
- setup_time_minutes = 0
- cycle_time_minutes = 0

Expected: Part passes through instantly, logs "Buffer Pass" event

### Test 4: Batch Operation
- batch_size = 5
- setup_time_minutes = 10
- cycle_time_minutes = 2

Expected: Setup once (10 min), then process 5 parts (10 min total)

---

## Common Issues and Solutions

### Issue 1: Compilation errors in SimulationEngine
**Solution:** Make sure you've added the SetupCompleteEvent.cs file

### Issue 2: Parts not processing
**Solution:** Check that OperationTimings are being created in MapToParts

### Issue 3: Negative times from Normal distribution
**Solution:** SampleNormal includes Math.Max(0.1, ...) to prevent negatives

### Issue 4: Setup events not logging
**Solution:** Check that _eventLogger?.LogSetupStart() is being called

---

## Next Steps After Implementation

Once this phase works:

**Phase 2: Build Gantt Chart**
- Query simulation_events table
- Filter for Setup Start/Complete and Processing Start/End events
- Create visual timeline showing:
  - Orange bars for setup
  - Blue bars for processing
  - One row per machine
  - X-axis = simulation time

**Phase 3: Enhanced Routing Management UI**
- Add distribution type dropdowns
- Add mean/std dev inputs
- Add batch size input
- Add "Is Buffer Only" checkbox

---

## Token Usage
You're at approximately 100,000 tokens used (52% of limit).

---

## Need Help?

If you encounter issues during implementation:
1. Check which file is causing the error
2. Look for missing using statements
3. Verify namespace matches your project structure
4. Check that SetupCompleteEvent.cs was added

Would you like me to create the detailed SimulationEngine.cs update guide next?
