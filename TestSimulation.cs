using ManufacturingSimulation.Database;
using ManufacturingSimulation.Bridge;

using var db = new MesDbContext();
var simService = new SimulationService(db);

// Run simulation with orders 1, 2, 3
var result = simService.QuickRun(
    studentId: 1,
    orderIds: new List<int> { 1, 2, 3 },
    durationHours: 168  // 1 week
);

Console.WriteLine($"✓ Simulation Complete!");
Console.WriteLine($"Throughput: {result.Statistics.Throughput:F2} parts/hour");
Console.WriteLine($"Avg Flow Time: {result.Statistics.AverageFlowTime:F1} minutes");
Console.WriteLine($"Parts Completed: {result.Statistics.TotalPartsCompleted}");
Console.WriteLine($"Bottleneck: {result.BottleneckWorkCenter?.WorkCenterName}");

// View detailed results
var summary = simService.GetRunSummary(result.RunId);
Console.WriteLine("\nWork Center Performance:");
foreach (var wc in summary.WorkCenterResults)
{
    Console.WriteLine($"  {wc.WorkCenter.WorkCenterName}: {wc.UtilizationPercent:F1}% utilized");
}