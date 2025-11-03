// SimulationScenarioOrder.cs
using System.ComponentModel.DataAnnotations.Schema;

namespace ManufacturingSimulation.Database.Models
{
    [Table("simulation_scenario_orders")]
    public class SimulationScenarioOrder
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("scenario_id")]
        public int ScenarioId { get; set; }

        [Column("production_order_id")]
        public int ProductionOrderId { get; set; }

        [Column("priority")]
        public int Priority { get; set; }
    }
}