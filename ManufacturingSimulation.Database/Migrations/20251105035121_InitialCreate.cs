using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManufacturingSimulation.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "machine_distances",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    from_machine_id = table.Column<int>(type: "INTEGER", nullable: false),
                    to_machine_id = table.Column<int>(type: "INTEGER", nullable: false),
                    distance_meters = table.Column<double>(type: "REAL", nullable: false),
                    travel_time_seconds = table.Column<double>(type: "REAL", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_machine_distances", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "machines",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    machine_type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    buffer_capacity = table.Column<int>(type: "INTEGER", nullable: false),
                    processing_rate = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_machines", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "simulation_scenario_orders",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    scenario_id = table.Column<int>(type: "INTEGER", nullable: false),
                    production_order_id = table.Column<int>(type: "INTEGER", nullable: false),
                    priority = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_simulation_scenario_orders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "students",
                columns: table => new
                {
                    student_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    username = table.Column<string>(type: "TEXT", nullable: false),
                    password_hash = table.Column<string>(type: "TEXT", nullable: false),
                    full_name = table.Column<string>(type: "TEXT", nullable: false),
                    email = table.Column<string>(type: "TEXT", nullable: true),
                    role = table.Column<string>(type: "TEXT", nullable: false),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_login = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_students", x => x.student_id);
                });

            migrationBuilder.CreateTable(
                name: "machine_locations",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    machine_id = table.Column<int>(type: "INTEGER", nullable: false),
                    x_coordinate = table.Column<double>(type: "REAL", nullable: false),
                    y_coordinate = table.Column<double>(type: "REAL", nullable: false),
                    rotation = table.Column<int>(type: "INTEGER", nullable: false),
                    floor_number = table.Column<int>(type: "INTEGER", nullable: false),
                    department = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_machine_locations", x => x.id);
                    table.ForeignKey(
                        name: "FK_machine_locations_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "components",
                columns: table => new
                {
                    component_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    student_id = table.Column<int>(type: "INTEGER", nullable: false),
                    component_number = table.Column<string>(type: "TEXT", nullable: false),
                    component_name = table.Column<string>(type: "TEXT", nullable: false),
                    type_id = table.Column<int>(type: "INTEGER", nullable: true),
                    description = table.Column<string>(type: "TEXT", nullable: true),
                    unit_cost = table.Column<double>(type: "REAL", nullable: true),
                    unit_of_measure = table.Column<string>(type: "TEXT", nullable: false),
                    lead_time_days = table.Column<int>(type: "INTEGER", nullable: true),
                    is_subcontracted = table.Column<bool>(type: "INTEGER", nullable: false),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_components", x => x.component_id);
                    table.ForeignKey(
                        name: "FK_components_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "student_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    product_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    student_id = table.Column<int>(type: "INTEGER", nullable: false),
                    product_number = table.Column<string>(type: "TEXT", nullable: false),
                    product_name = table.Column<string>(type: "TEXT", nullable: false),
                    category_id = table.Column<int>(type: "INTEGER", nullable: true),
                    description = table.Column<string>(type: "TEXT", nullable: true),
                    standard_cost = table.Column<double>(type: "REAL", nullable: true),
                    selling_price = table.Column<double>(type: "REAL", nullable: true),
                    bom_levels = table.Column<int>(type: "INTEGER", nullable: true),
                    assembly_time_minutes = table.Column<int>(type: "INTEGER", nullable: true),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.product_id);
                    table.ForeignKey(
                        name: "FK_products_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "student_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "simulation_scenarios",
                columns: table => new
                {
                    scenario_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    student_id = table.Column<int>(type: "INTEGER", nullable: false),
                    scenario_name = table.Column<string>(type: "TEXT", nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: true),
                    base_date = table.Column<DateTime>(type: "TEXT", nullable: true),
                    simulation_duration_hours = table.Column<double>(type: "REAL", nullable: false),
                    random_seed = table.Column<int>(type: "INTEGER", nullable: false),
                    created_date = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_simulation_scenarios", x => x.scenario_id);
                    table.ForeignKey(
                        name: "FK_simulation_scenarios_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "student_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work_centers",
                columns: table => new
                {
                    work_center_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    student_id = table.Column<int>(type: "INTEGER", nullable: false),
                    work_center_code = table.Column<string>(type: "TEXT", nullable: false),
                    work_center_name = table.Column<string>(type: "TEXT", nullable: false),
                    wc_type_id = table.Column<int>(type: "INTEGER", nullable: true),
                    capacity_units_per_hour = table.Column<double>(type: "REAL", nullable: true),
                    operating_cost_per_hour = table.Column<double>(type: "REAL", nullable: true),
                    setup_time_minutes = table.Column<int>(type: "INTEGER", nullable: false),
                    availability_percent = table.Column<double>(type: "REAL", nullable: false),
                    investment_cost = table.Column<double>(type: "REAL", nullable: true),
                    depreciation_years = table.Column<int>(type: "INTEGER", nullable: true),
                    requires_operator = table.Column<bool>(type: "INTEGER", nullable: false),
                    is_bottleneck = table.Column<bool>(type: "INTEGER", nullable: false),
                    quantity = table.Column<int>(type: "INTEGER", nullable: true),
                    buffer_capacity = table.Column<int>(type: "INTEGER", nullable: true),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: true),
                    updated_date = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_centers", x => x.work_center_id);
                    table.ForeignKey(
                        name: "FK_work_centers_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "student_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "production_orders",
                columns: table => new
                {
                    order_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    student_id = table.Column<int>(type: "INTEGER", nullable: false),
                    order_number = table.Column<string>(type: "TEXT", nullable: false),
                    product_id = table.Column<int>(type: "INTEGER", nullable: false),
                    quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    due_date = table.Column<DateTime>(type: "TEXT", nullable: true),
                    priority = table.Column<int>(type: "INTEGER", nullable: false),
                    status = table.Column<string>(type: "TEXT", nullable: false),
                    release_date = table.Column<DateTime>(type: "TEXT", nullable: true),
                    start_date = table.Column<DateTime>(type: "TEXT", nullable: true),
                    completion_date = table.Column<DateTime>(type: "TEXT", nullable: true),
                    created_date = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_orders", x => x.order_id);
                    table.ForeignKey(
                        name: "FK_production_orders_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_production_orders_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "student_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "simulation_runs",
                columns: table => new
                {
                    run_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    scenario_id = table.Column<int>(type: "INTEGER", nullable: false),
                    student_id = table.Column<int>(type: "INTEGER", nullable: false),
                    run_date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    status = table.Column<string>(type: "TEXT", nullable: false),
                    duration_seconds = table.Column<double>(type: "REAL", nullable: true),
                    config_json = table.Column<string>(type: "TEXT", nullable: true),
                    error_message = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_simulation_runs", x => x.run_id);
                    table.ForeignKey(
                        name: "FK_simulation_runs_simulation_scenarios_scenario_id",
                        column: x => x.scenario_id,
                        principalTable: "simulation_scenarios",
                        principalColumn: "scenario_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_simulation_runs_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "student_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "routings",
                columns: table => new
                {
                    routing_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    student_id = table.Column<int>(type: "INTEGER", nullable: false),
                    product_id = table.Column<int>(type: "INTEGER", nullable: false),
                    operation_seq = table.Column<int>(type: "INTEGER", nullable: false),
                    work_center_id = table.Column<int>(type: "INTEGER", nullable: false),
                    operation_name = table.Column<string>(type: "TEXT", nullable: true),
                    setup_time_minutes = table.Column<int>(type: "INTEGER", nullable: true),
                    cycle_time_minutes = table.Column<double>(type: "REAL", nullable: true),
                    labor_hours = table.Column<double>(type: "REAL", nullable: true),
                    setup_time_mean = table.Column<double>(type: "REAL", nullable: true),
                    setup_time_std_dev = table.Column<double>(type: "REAL", nullable: true),
                    setup_time_distribution = table.Column<string>(type: "TEXT", nullable: true),
                    cycle_time_std_dev = table.Column<double>(type: "REAL", nullable: true),
                    cycle_time_distribution = table.Column<string>(type: "TEXT", nullable: true),
                    batch_size = table.Column<int>(type: "INTEGER", nullable: true),
                    is_buffer_only = table.Column<bool>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_routings", x => x.routing_id);
                    table.ForeignKey(
                        name: "FK_routings_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_routings_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "student_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_routings_work_centers_work_center_id",
                        column: x => x.work_center_id,
                        principalTable: "work_centers",
                        principalColumn: "work_center_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "simulation_events",
                columns: table => new
                {
                    event_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    run_id = table.Column<int>(type: "INTEGER", nullable: false),
                    event_time = table.Column<double>(type: "REAL", nullable: false),
                    event_type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    part_id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    order_number = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    machine_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    queue_size = table.Column<int>(type: "INTEGER", nullable: false),
                    details = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_simulation_events", x => x.event_id);
                    table.ForeignKey(
                        name: "FK_simulation_events_simulation_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "simulation_runs",
                        principalColumn: "run_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "simulation_order_predictions",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    run_id = table.Column<int>(type: "INTEGER", nullable: false),
                    production_order_id = table.Column<int>(type: "INTEGER", nullable: false),
                    predicted_completion_time = table.Column<double>(type: "REAL", nullable: true),
                    predicted_flow_time_hours = table.Column<double>(type: "REAL", nullable: true),
                    completed = table.Column<bool>(type: "INTEGER", nullable: false),
                    parts_completed = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_simulation_order_predictions", x => x.id);
                    table.ForeignKey(
                        name: "FK_simulation_order_predictions_production_orders_production_order_id",
                        column: x => x.production_order_id,
                        principalTable: "production_orders",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_simulation_order_predictions_simulation_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "simulation_runs",
                        principalColumn: "run_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "simulation_results",
                columns: table => new
                {
                    result_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    run_id = table.Column<int>(type: "INTEGER", nullable: false),
                    simulated_time_hours = table.Column<double>(type: "REAL", nullable: false),
                    throughput = table.Column<double>(type: "REAL", nullable: false),
                    avg_flow_time_hours = table.Column<double>(type: "REAL", nullable: false),
                    avg_wip = table.Column<double>(type: "REAL", nullable: false),
                    total_parts_arrived = table.Column<int>(type: "INTEGER", nullable: false),
                    total_parts_completed = table.Column<int>(type: "INTEGER", nullable: false),
                    overall_utilization_percent = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_simulation_results", x => x.result_id);
                    table.ForeignKey(
                        name: "FK_simulation_results_simulation_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "simulation_runs",
                        principalColumn: "run_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "simulation_work_center_results",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    run_id = table.Column<int>(type: "INTEGER", nullable: false),
                    work_center_id = table.Column<int>(type: "INTEGER", nullable: false),
                    utilization_percent = table.Column<double>(type: "REAL", nullable: false),
                    parts_processed = table.Column<int>(type: "INTEGER", nullable: false),
                    avg_queue_size = table.Column<double>(type: "REAL", nullable: false),
                    max_queue_size = table.Column<int>(type: "INTEGER", nullable: false),
                    avg_wait_time_hours = table.Column<double>(type: "REAL", nullable: false),
                    is_bottleneck = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_simulation_work_center_results", x => x.id);
                    table.ForeignKey(
                        name: "FK_simulation_work_center_results_simulation_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "simulation_runs",
                        principalColumn: "run_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_simulation_work_center_results_work_centers_work_center_id",
                        column: x => x.work_center_id,
                        principalTable: "work_centers",
                        principalColumn: "work_center_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_components_student_id",
                table: "components",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_machine_locations_machine_id",
                table: "machine_locations",
                column: "machine_id");

            migrationBuilder.CreateIndex(
                name: "IX_production_orders_product_id",
                table: "production_orders",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_production_orders_student_id",
                table: "production_orders",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_products_student_id",
                table: "products",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_routings_product_id",
                table: "routings",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_routings_student_id",
                table: "routings",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_routings_work_center_id",
                table: "routings",
                column: "work_center_id");

            migrationBuilder.CreateIndex(
                name: "IX_simulation_events_run_id",
                table: "simulation_events",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "IX_simulation_order_predictions_production_order_id",
                table: "simulation_order_predictions",
                column: "production_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_simulation_order_predictions_run_id",
                table: "simulation_order_predictions",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "IX_simulation_results_run_id",
                table: "simulation_results",
                column: "run_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_simulation_runs_scenario_id",
                table: "simulation_runs",
                column: "scenario_id");

            migrationBuilder.CreateIndex(
                name: "IX_simulation_runs_student_id",
                table: "simulation_runs",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_simulation_scenarios_student_id",
                table: "simulation_scenarios",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_simulation_work_center_results_run_id",
                table: "simulation_work_center_results",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "IX_simulation_work_center_results_work_center_id",
                table: "simulation_work_center_results",
                column: "work_center_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_centers_student_id",
                table: "work_centers",
                column: "student_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "components");

            migrationBuilder.DropTable(
                name: "machine_distances");

            migrationBuilder.DropTable(
                name: "machine_locations");

            migrationBuilder.DropTable(
                name: "routings");

            migrationBuilder.DropTable(
                name: "simulation_events");

            migrationBuilder.DropTable(
                name: "simulation_order_predictions");

            migrationBuilder.DropTable(
                name: "simulation_results");

            migrationBuilder.DropTable(
                name: "simulation_scenario_orders");

            migrationBuilder.DropTable(
                name: "simulation_work_center_results");

            migrationBuilder.DropTable(
                name: "machines");

            migrationBuilder.DropTable(
                name: "production_orders");

            migrationBuilder.DropTable(
                name: "simulation_runs");

            migrationBuilder.DropTable(
                name: "work_centers");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "simulation_scenarios");

            migrationBuilder.DropTable(
                name: "students");
        }
    }
}
