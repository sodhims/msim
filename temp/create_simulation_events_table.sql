-- Create simulation_events table for detailed event logging

CREATE TABLE IF NOT EXISTS simulation_events (
    event_id INTEGER PRIMARY KEY AUTOINCREMENT,
    run_id INTEGER NOT NULL,
    event_time REAL NOT NULL,
    event_type TEXT NOT NULL,
    part_id TEXT,
    order_number TEXT,
    machine_name TEXT,
    queue_size INTEGER DEFAULT 0,
    details TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (run_id) REFERENCES simulation_runs(run_id)
);

CREATE INDEX idx_events_run_id ON simulation_events(run_id);
CREATE INDEX idx_events_time ON simulation_events(event_time);
CREATE INDEX idx_events_type ON simulation_events(event_type);
