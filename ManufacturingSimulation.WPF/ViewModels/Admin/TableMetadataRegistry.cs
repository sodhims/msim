using System;
using System.Collections.Generic;
using System.Linq;
using ManufacturingSimulation.Database;
using ManufacturingSimulation.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace ManufacturingSimulation.WPF.ViewModels.Admin
{
    /// <summary>
    /// Registry of all database tables with their metadata for the admin interface
    /// </summary>
    public static class TableMetadataRegistry
    {
        public static List<TableMetadata> GetRegistry(MesDbContext context)
        {
            var tables = new List<TableMetadata>
            {
                // ===== MES DATA =====
                new TableMetadata
                {
                    TableName = "products",
                    DisplayName = "Products",
                    EntityType = typeof(Product),
                    Category = "MES Data",
                    PrimaryKey = "ProductId",
                    HasStudentId = true,
                    Description = "Products that can be manufactured",
                    SortOrder = 1
                },
                
                new TableMetadata
                {
                    TableName = "production_orders",
                    DisplayName = "Production Orders",
                    EntityType = typeof(ProductionOrder),
                    Category = "MES Data",
                    PrimaryKey = "OrderId",
                    HasStudentId = true,
                    Description = "Manufacturing orders to be executed",
                    SortOrder = 2,
                    ForeignKeys = new Dictionary<string, ForeignKeyInfo>
                    {
                        ["ProductId"] = new ForeignKeyInfo 
                        { 
                            ReferencedTable = "products", 
                            DisplayColumn = "ProductName",
                            ReferencedEntityType = typeof(Product)
                        }
                    }
                },

                new TableMetadata
                {
                    TableName = "work_centers",
                    DisplayName = "Work Centers",
                    EntityType = typeof(WorkCenter),
                    Category = "MES Data",
                    PrimaryKey = "WorkCenterId",
                    HasStudentId = true,
                    Description = "Manufacturing machines and workstations (⚠️ Contains BufferCapacity!)",
                    SortOrder = 3
                },

                new TableMetadata
                {
                    TableName = "routings",
                    DisplayName = "Routings",
                    EntityType = typeof(Routing),
                    Category = "MES Data",
                    PrimaryKey = "RoutingId",
                    HasStudentId = true,
                    Description = "Manufacturing process steps for products",
                    SortOrder = 4,
                    ForeignKeys = new Dictionary<string, ForeignKeyInfo>
                    {
                        ["ProductId"] = new ForeignKeyInfo 
                        { 
                            ReferencedTable = "products", 
                            DisplayColumn = "ProductName",
                            ReferencedEntityType = typeof(Product)
                        },
                        ["WorkCenterId"] = new ForeignKeyInfo 
                        { 
                            ReferencedTable = "work_centers", 
                            DisplayColumn = "WorkCenterName",
                            ReferencedEntityType = typeof(WorkCenter)
                        }
                    }
                },

                new TableMetadata
                {
                    TableName = "components",
                    DisplayName = "Components",
                    EntityType = typeof(Component),
                    Category = "MES Data",
                    PrimaryKey = "ComponentId",
                    HasStudentId = true,
                    Description = "Components and materials used in products",
                    SortOrder = 5
                },

                // ===== SIMULATION =====
                new TableMetadata
                {
                    TableName = "simulation_scenarios",
                    DisplayName = "Simulation Scenarios",
                    EntityType = typeof(SimulationScenario),
                    Category = "Simulation",
                    PrimaryKey = "ScenarioId",
                    HasStudentId = true,
                    Description = "Simulation configurations and parameters",
                    SortOrder = 1
                },

                new TableMetadata
                {
                    TableName = "simulation_runs",
                    DisplayName = "Simulation Runs",
                    EntityType = typeof(SimulationRun),
                    Category = "Simulation",
                    PrimaryKey = "RunId",
                    HasStudentId = true,
                    Description = "Executed simulation runs (⚠️ Check ConfigJson column!)",
                    SortOrder = 2,
                    ForeignKeys = new Dictionary<string, ForeignKeyInfo>
                    {
                        ["ScenarioId"] = new ForeignKeyInfo 
                        { 
                            ReferencedTable = "simulation_scenarios", 
                            DisplayColumn = "ScenarioName",
                            ReferencedEntityType = typeof(SimulationScenario)
                        }
                    }
                },

                new TableMetadata
                {
                    TableName = "simulation_results",
                    DisplayName = "Simulation Results",
                    EntityType = typeof(SimulationResult),
                    Category = "Simulation",
                    PrimaryKey = "ResultId",
                    HasStudentId = false,
                    Description = "Aggregated results from simulation runs",
                    SortOrder = 3,
                    ForeignKeys = new Dictionary<string, ForeignKeyInfo>
                    {
                        ["RunId"] = new ForeignKeyInfo 
                        { 
                            ReferencedTable = "simulation_runs", 
                            DisplayColumn = "RunId",
                            ReferencedEntityType = typeof(SimulationRun)
                        }
                    },
                    ReadOnlyColumns = new List<string> { "ResultId", "RunId" }
                },

                new TableMetadata
                {
                    TableName = "simulation_events",
                    DisplayName = "Simulation Events",
                    EntityType = typeof(SimulationEvent),
                    Category = "Simulation",
                    PrimaryKey = "EventId",
                    HasStudentId = false,
                    Description = "Detailed event log from simulations (large table!)",
                    SortOrder = 4,
                    ForeignKeys = new Dictionary<string, ForeignKeyInfo>
                    {
                        ["RunId"] = new ForeignKeyInfo 
                        { 
                            ReferencedTable = "simulation_runs", 
                            DisplayColumn = "RunId",
                            ReferencedEntityType = typeof(SimulationRun)
                        }
                    },
                    ReadOnlyColumns = new List<string> { "EventId", "RunId" }
                },

                new TableMetadata
                {
                    TableName = "simulation_work_center_results",
                    DisplayName = "Work Center Results",
                    EntityType = typeof(SimulationWorkCenterResult),
                    Category = "Simulation",
                    PrimaryKey = "Id",
                    HasStudentId = false,
                    Description = "Per-machine simulation results",
                    SortOrder = 5,
                    ForeignKeys = new Dictionary<string, ForeignKeyInfo>
                    {
                        ["RunId"] = new ForeignKeyInfo 
                        { 
                            ReferencedTable = "simulation_runs", 
                            DisplayColumn = "RunId",
                            ReferencedEntityType = typeof(SimulationRun)
                        },
                        ["WorkCenterId"] = new ForeignKeyInfo 
                        { 
                            ReferencedTable = "work_centers", 
                            DisplayColumn = "WorkCenterName",
                            ReferencedEntityType = typeof(WorkCenter)
                        }
                    }
                },

                new TableMetadata
                {
                    TableName = "simulation_order_predictions",
                    DisplayName = "Order Predictions",
                    EntityType = typeof(SimulationOrderPrediction),
                    Category = "Simulation",
                    PrimaryKey = "Id",
                    HasStudentId = false,
                    Description = "Predicted completion times for orders",
                    SortOrder = 6,
                    ForeignKeys = new Dictionary<string, ForeignKeyInfo>
                    {
                        ["RunId"] = new ForeignKeyInfo 
                        { 
                            ReferencedTable = "simulation_runs", 
                            DisplayColumn = "RunId",
                            ReferencedEntityType = typeof(SimulationRun)
                        },
                        ["ProductionOrderId"] = new ForeignKeyInfo 
                        { 
                            ReferencedTable = "production_orders", 
                            DisplayColumn = "OrderNumber",
                            ReferencedEntityType = typeof(ProductionOrder)
                        }
                    }
                },

                new TableMetadata
                {
                    TableName = "simulation_scenario_orders",
                    DisplayName = "Scenario Orders",
                    EntityType = typeof(SimulationScenarioOrder),
                    Category = "Simulation",
                    PrimaryKey = "Id",
                    HasStudentId = false,
                    Description = "Orders included in each scenario",
                    SortOrder = 7
                },

                // ===== FACTORY LAYOUT =====
                new TableMetadata
                {
                    TableName = "machine_locations",
                    DisplayName = "Machine Locations",
                    EntityType = typeof(MachineLocation),
                    Category = "Factory Layout",
                    PrimaryKey = "Id",
                    HasStudentId = false,
                    Description = "X/Y coordinates of machines on factory floor",
                    SortOrder = 1,
                    ForeignKeys = new Dictionary<string, ForeignKeyInfo>
                    {
                        ["MachineId"] = new ForeignKeyInfo 
                        { 
                            ReferencedTable = "machines", 
                            DisplayColumn = "Name",
                            ReferencedEntityType = typeof(Machine)
                        }
                    }
                },

                new TableMetadata
                {
                    TableName = "machine_distances",
                    DisplayName = "Machine Distances",
                    EntityType = typeof(MachineDistance),
                    Category = "Factory Layout",
                    PrimaryKey = "Id",
                    HasStudentId = false,
                    Description = "Calculated distances between machines",
                    SortOrder = 2
                },

                new TableMetadata
                {
                    TableName = "machines",
                    DisplayName = "Machines",
                    EntityType = typeof(Machine),
                    Category = "Factory Layout",
                    PrimaryKey = "Id",
                    HasStudentId = false,
                    Description = "Machine definitions (⚠️ Also has BufferCapacity!)",
                    SortOrder = 3
                },

                // ===== SYSTEM =====
                new TableMetadata
                {
                    TableName = "students",
                    DisplayName = "Students",
                    EntityType = typeof(Student),
                    Category = "System",
                    PrimaryKey = "StudentId",
                    HasStudentId = false,
                    Description = "User accounts",
                    SortOrder = 1,
                    ReadOnlyColumns = new List<string> { "PasswordHash", "CreatedDate" }
                }
            };

            // Load record counts
            foreach (var table in tables)
            {
                try
                {
                    var countMethod = typeof(Queryable)
                        .GetMethods()
                        .First(m => m.Name == "Count" && m.GetParameters().Length == 1)
                        .MakeGenericMethod(table.EntityType);

                    var dbSet = context.GetType().GetMethod("Set", Type.EmptyTypes)
                        ?.MakeGenericMethod(table.EntityType)
                        ?.Invoke(context, null);

                    if (dbSet != null)
                    {
                        table.RecordCount = (int)countMethod.Invoke(null, new[] { dbSet });
                    }
                }
                catch
                {
                    table.RecordCount = 0;
                }
            }

            return tables;
        }
    }
}
