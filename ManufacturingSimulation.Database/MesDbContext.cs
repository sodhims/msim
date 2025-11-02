using Microsoft.EntityFrameworkCore;
using ManufacturingSimulation.Database.Models;

namespace ManufacturingSimulation.Database
{
    /// <summary>
    /// Entity Framework Core database context for MES Training Database
    /// Connects to SQLite (easily switchable to PostgreSQL later)
    /// </summary>
    public class MesDbContext : DbContext
    {
        public MesDbContext() { }
        
        public MesDbContext(DbContextOptions<MesDbContext> options) : base(options) { }

        // Student Management
        public DbSet<Student> Students { get; set; }
        
        // Products & BOM
        public DbSet<Product> Products { get; set; }
        public DbSet<Component> Components { get; set; }
        
        // Work Centers & Routing
        public DbSet<WorkCenter> WorkCenters { get; set; }
        public DbSet<Routing> Routings { get; set; }
        
        // Production Orders (you'll add this table to your schema)
        public DbSet<ProductionOrder> ProductionOrders { get; set; }
        
        // Simulation Integration
        public DbSet<SimulationScenario> SimulationScenarios { get; set; }
        public DbSet<SimulationEvent> SimulationEvents { get; set; }
        public DbSet<SimulationRun> SimulationRuns { get; set; }
        public DbSet<SimulationResult> SimulationResults { get; set; }
        public DbSet<SimulationWorkCenterResult> SimulationWorkCenterResults { get; set; }
        public DbSet<SimulationOrderPrediction> SimulationOrderPredictions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Default SQLite connection - change this to PostgreSQL when ready
optionsBuilder.UseSqlite(@"Data Source=C:\users\sodhi\source\repos\msim\ManufacturingSimulation.Database\mes_training.db");                
                // TO SWITCH TO POSTGRESQL (just uncomment and configure):
                // optionsBuilder.UseNpgsql("Host=localhost;Database=mes_training;Username=postgres;Password=yourpassword");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure table mappings (SQLite uses lowercase with underscores)
            modelBuilder.Entity<Student>().ToTable("students");
            modelBuilder.Entity<Product>().ToTable("products");
            modelBuilder.Entity<Component>().ToTable("components");
            modelBuilder.Entity<WorkCenter>().ToTable("work_centers");
            modelBuilder.Entity<Routing>().ToTable("routings");
            modelBuilder.Entity<ProductionOrder>().ToTable("production_orders");
            
            // Simulation tables
            modelBuilder.Entity<SimulationScenario>().ToTable("simulation_scenarios");
            modelBuilder.Entity<SimulationRun>().ToTable("simulation_runs");
            modelBuilder.Entity<SimulationResult>().ToTable("simulation_results");
            modelBuilder.Entity<SimulationWorkCenterResult>().ToTable("simulation_work_center_results");
            modelBuilder.Entity<SimulationOrderPrediction>().ToTable("simulation_order_predictions");

            // Configure relationships
            modelBuilder.Entity<Routing>()
                .HasOne(r => r.Product)
                .WithMany(p => p.Routings)
                .HasForeignKey(r => r.ProductId);

            modelBuilder.Entity<Routing>()
                .HasOne(r => r.WorkCenter)
                .WithMany()
                .HasForeignKey(r => r.WorkCenterId);

            modelBuilder.Entity<ProductionOrder>()
                .HasOne(po => po.Product)
                .WithMany()
                .HasForeignKey(po => po.ProductId);

            modelBuilder.Entity<SimulationRun>()
                .HasOne(sr => sr.Scenario)
                .WithMany(s => s.Runs)
                .HasForeignKey(sr => sr.ScenarioId);

            modelBuilder.Entity<SimulationResult>()
                .HasOne(sr => sr.Run)
                .WithOne(r => r.Result)
                .HasForeignKey<SimulationResult>(sr => sr.RunId);

            // Configure column mappings (snake_case for SQLite)
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entity.GetProperties())
                {
                    // Convert PascalCase to snake_case for SQLite
                    property.SetColumnName(ToSnakeCase(property.Name));
                }
            }
        }

        private string ToSnakeCase(string name)
        {
            return string.Concat(name.Select((x, i) => i > 0 && char.IsUpper(x) 
                ? "_" + x.ToString() 
                : x.ToString())).ToLower();
        }
    }
}
