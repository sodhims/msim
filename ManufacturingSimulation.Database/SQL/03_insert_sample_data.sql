-- ============================================================================
-- SAMPLE DATA FOR MES TRAINING DATABASE
-- ============================================================================

-- Students
INSERT INTO students (username, password_hash, full_name, email, role) VALUES
('demo', 'demo_hash_12345', 'Demo Student', 'demo@example.com', 'Student'),
('instructor', 'instructor_hash_12345', 'John Instructor', 'instructor@example.com', 'Instructor');

-- Product Categories
INSERT INTO product_categories (category_name, description) VALUES
('IoT Devices', 'Internet of Things sensors and controllers'),
('Displays', 'LCD and LED display modules'),
('Controllers', 'Microcontroller-based systems');

-- Component Types
INSERT INTO component_types (type_name, description) VALUES
('PCB', 'Printed Circuit Board'),
('IC', 'Integrated Circuit'),
('Passive', 'Resistors, Capacitors'),
('Connector', 'Electrical Connectors'),
('Housing', 'Plastic enclosures');

-- Work Center Types
INSERT INTO work_center_types (type_name, is_automated) VALUES
('Manual Assembly', 0),
('Automated Assembly', 1),
('Testing', 0),
('Packaging', 0);

-- Work Centers for student_id = 1
INSERT INTO work_centers (student_id, work_center_code, work_center_name, wc_type_id, capacity_units_per_hour, operating_cost_per_hour, setup_time_minutes, availability_percent) VALUES
(1, 'PCB-ASSY', 'PCB Assembly Station', 1, 12.0, 45.0, 15, 85.0),
(1, 'TEST-01', 'Functional Testing', 3, 8.0, 35.0, 10, 90.0),
(1, 'PKG-01', 'Packaging Station', 4, 15.0, 25.0, 5, 95.0),
(1, 'INSP-01', 'Final Inspection', 3, 10.0, 30.0, 5, 92.0);

-- Products for student_id = 1
INSERT INTO products (student_id, product_number, product_name, category_id, description, standard_cost, selling_price, assembly_time_minutes, is_active) VALUES
(1, 'IOT-TEMP-001', 'IoT Temperature Sensor', 1, 'Wireless temperature monitoring sensor', 12.50, 29.99, 45, 1),
(1, 'CTRL-HOME-001', 'Smart Home Controller', 3, 'Central control unit for home automation', 24.00, 59.99, 65, 1),
(1, 'DISP-LCD-001', 'LCD Display Module', 2, '2.4 inch color LCD display', 8.75, 19.99, 35, 1);

-- Routings for IoT Temperature Sensor (product_id = 1)
INSERT INTO routings (student_id, product_id, operation_seq, work_center_id, operation_name, setup_time_minutes, cycle_time_minutes, labor_hours) VALUES
(1, 1, 10, 1, 'PCB Assembly', 15, 5.0, 0.10),
(1, 1, 20, 2, 'Functional Test', 10, 3.0, 0.05),
(1, 1, 30, 4, 'Final Inspection', 5, 2.0, 0.03),
(1, 1, 40, 3, 'Package', 5, 1.5, 0.02);

-- Routings for Smart Home Controller (product_id = 2)
INSERT INTO routings (student_id, product_id, operation_seq, work_center_id, operation_name, setup_time_minutes, cycle_time_minutes, labor_hours) VALUES
(1, 2, 10, 1, 'PCB Assembly', 20, 8.0, 0.15),
(1, 2, 20, 2, 'Programming & Test', 15, 5.0, 0.08),
(1, 2, 30, 4, 'Quality Check', 5, 3.0, 0.05),
(1, 2, 40, 3, 'Package', 5, 2.0, 0.03);

-- Routings for LCD Display Module (product_id = 3)
INSERT INTO routings (student_id, product_id, operation_seq, work_center_id, operation_name, setup_time_minutes, cycle_time_minutes, labor_hours) VALUES
(1, 3, 10, 1, 'Assembly', 10, 4.0, 0.08),
(1, 3, 20, 2, 'Display Test', 10, 3.5, 0.06),
(1, 3, 30, 3, 'Package', 5, 1.5, 0.02);

-- Production Orders for student_id = 1
INSERT INTO production_orders (student_id, order_number, product_id, quantity, due_date, priority, status, release_date) VALUES
(1, 'WO-2024-001', 1, 50, datetime('now', '+7 days'), 1, 'Released', datetime('now')),
(1, 'WO-2024-002', 2, 30, datetime('now', '+10 days'), 2, 'Released', datetime('now')),
(1, 'WO-2024-003', 1, 40, datetime('now', '+5 days'), 1, 'Released', datetime('now')),
(1, 'WO-2024-004', 3, 60, datetime('now', '+14 days'), 3, 'Planned', NULL),
(1, 'WO-2024-005', 2, 25, datetime('now', '+12 days'), 2, 'Planned', NULL);

-- Operator Roles
INSERT INTO operator_roles (role_name, base_rate_per_hour, loaded_rate_per_hour) VALUES
('Assembly Technician', 18.50, 27.75),
('Test Technician', 20.00, 30.00),
('Quality Inspector', 22.00, 33.00),
('Packaging Operator', 16.00, 24.00);

-- Operators for student_id = 1
INSERT INTO operators (student_id, operator_number, operator_name, role_id, skill_level, hourly_rate, is_active) VALUES
(1, 'OP-001', 'John Smith', 1, 3, 22.00, 1),
(1, 'OP-002', 'Jane Doe', 2, 2, 20.00, 1),
(1, 'OP-003', 'Mike Johnson', 3, 3, 24.00, 1),
(1, 'OP-004', 'Sarah Williams', 4, 2, 18.00, 1);
