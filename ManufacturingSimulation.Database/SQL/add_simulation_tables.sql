-- ============================================================================
-- SIMULATION INTEGRATION TABLES
-- Add these to your existing MES Training Database
-- ============================================================================

-- Production Orders table (if you don't have it already)
CREATE TABLE IF NOT EXISTS production_orders (
    order_id INTEGER PRIMARY KEY AUTOINCREMENT,
    student_id INTEGER NOT NULL,
    order_number TEXT NOT NULL,
    product_id INTEGER NOT NULL,
    quantity INTEGER NOT NULL,
    due_date TEXT,
    priority INTEGER DEFAULT 1,
    status TEXT DEFAULT 'Planned' CHECK(status IN ('Planned', 'Released', 'InProgress', 'Completed')),
    release_date TEXT,
    start_date TEXT,
    completion_date TEXT,
    created_date TEXT DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (student_id) REFERENCES students(student_id),
    FOREIGN KEY (product_id) REFERENCES products(product_id),
    UNIQUE(student_id, order_number)
);

-- Simulation scenarios (what-if analysis)
CREATE TABLE simulation_scenarios (
    scenario_id INTEGER PRIMARY KEY AUTOINCREMENT,
    student_id INTEGER NOT NULL,
    scenario_name TEXT NOT NULL,
    description TEXT,
    base_date TEXT,
    simulation_duration_hours REAL DEFAULT 168.0,
    random_seed INTEGER DEFAULT 42,
    created_date TEXT DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (student_id) REFERENCES students(student_id)
);

-- Orders included in simulation scenarios
CREATE TABLE simulation_scenario_orders (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    scenario_id INTEGER NOT NULL,
    production_order_id INTEGER NOT NULL,
    priority INTEGER DEFAULT 1,
    FOREIGN KEY (scenario_id) REFERENCES simulation_scenarios(scenario_id) ON DELETE CASCADE,
    FOREIGN KEY (production_order_id) REFERENCES production_orders(order_id),
    UNIQUE(scenario_id, production_order_id)
);

-- Simulation runs (actual executions)
CREATE TABLE simulation_runs (
    run_id INTEGER PRIMARY KEY AUTOINCREMENT,
    scenario_id INTEGER NOT NULL,
    student_id INTEGER NOT NULL,
    run_date TEXT DEFAULT CURRENT_TIMESTAMP,
    status TEXT DEFAULT 'Pending' CHECK(status IN ('Pending', 'Running', 'Completed', 'Failed')),
    duration_seconds REAL,
    config_json TEXT,
    error_message TEXT,
    FOREIGN KEY (scenario_id) REFERENCES simulation_scenarios(scenario_id) ON DELETE CASCADE,
    FOREIGN KEY (student_id) REFERENCES students(student_id)
);

-- Overall results per run
CREATE TABLE simulation_results (
    result_id INTEGER PRIMARY KEY AUTOINCREMENT,
    run_id INTEGER NOT NULL,
    simulated_time_hours REAL,
    throughput REAL,
    avg_flow_time_hours REAL,
    avg_wip REAL,
    total_parts_arrived INTEGER,
    total_parts_completed INTEGER,
    overall_utilization_percent REAL,
    FOREIGN KEY (run_id) REFERENCES simulation_runs(run_id) ON DELETE CASCADE,
    UNIQUE(run_id)
);

-- Work center performance predictions
CREATE TABLE simulation_work_center_results (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    run_id INTEGER NOT NULL,
    work_center_id INTEGER NOT NULL,
    utilization_percent REAL,
    parts_processed INTEGER,
    avg_queue_size REAL,
    max_queue_size INTEGER,
    avg_wait_time_hours REAL,
    is_bottleneck INTEGER DEFAULT 0,
    FOREIGN KEY (run_id) REFERENCES simulation_runs(run_id) ON DELETE CASCADE,
    FOREIGN KEY (work_center_id) REFERENCES work_centers(work_center_id)
);

-- Order completion predictions
CREATE TABLE simulation_order_predictions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    run_id INTEGER NOT NULL,
    production_order_id INTEGER NOT NULL,
    predicted_completion_time REAL,
    predicted_flow_time_hours REAL,
    completed INTEGER DEFAULT 0,
    parts_completed INTEGER DEFAULT 0,
    FOREIGN KEY (run_id) REFERENCES simulation_runs(run_id) ON DELETE CASCADE,
    FOREIGN KEY (production_order_id) REFERENCES production_orders(order_id)
);

-- Event log (optional - for detailed analysis)
CREATE TABLE IF NOT EXISTS simulation_events (
    event_id INTEGER PRIMARY KEY AUTOINCREMENT,
    run_id INTEGER NOT NULL,
    event_time REAL,
    event_type TEXT,
    work_center_id INTEGER,
    order_id INTEGER,
    description TEXT,
    FOREIGN KEY (run_id) REFERENCES simulation_runs(run_id) ON DELETE CASCADE
);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS idx_sim_runs_scenario ON simulation_runs(scenario_id);
CREATE INDEX IF NOT EXISTS idx_sim_runs_student ON simulation_runs(student_id);
CREATE INDEX IF NOT EXISTS idx_sim_results_run ON simulation_results(run_id);
CREATE INDEX IF NOT EXISTS idx_sim_wc_results_run ON simulation_work_center_results(run_id);
CREATE INDEX IF NOT EXISTS idx_sim_wc_results_wc ON simulation_work_center_results(work_center_id);
CREATE INDEX IF NOT EXISTS idx_sim_order_pred_run ON simulation_order_predictions(run_id);
CREATE INDEX IF NOT EXISTS idx_sim_order_pred_order ON simulation_order_predictions(production_order_id);
CREATE INDEX IF NOT EXISTS idx_sim_events_run ON simulation_events(run_id);
CREATE INDEX IF NOT EXISTS idx_sim_scenario_orders_scenario ON simulation_scenario_orders(scenario_id);

-- ============================================================================
-- Sample Data for Testing
-- ============================================================================

-- Insert sample student if needed
INSERT OR IGNORE INTO students (student_id, username, password_hash, full_name, email, role)
VALUES (1, 'demo', 'demo_hash', 'Demo Student', 'demo@example.com', 'Student');

-- You'll need to add work centers, products, routings, and orders
-- based on your specific manufacturing system setup
