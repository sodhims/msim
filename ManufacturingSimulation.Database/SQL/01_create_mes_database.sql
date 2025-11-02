-- ============================================================================
-- MES TRAINING DATABASE - COMPLETE SCHEMA
-- Electronics Manufacturing
-- ============================================================================

PRAGMA foreign_keys = ON;

-- ============================================================================
-- STUDENT MANAGEMENT
-- ============================================================================

CREATE TABLE students (
    student_id INTEGER PRIMARY KEY AUTOINCREMENT,
    username TEXT UNIQUE NOT NULL,
    password_hash TEXT NOT NULL,
    full_name TEXT NOT NULL,
    email TEXT,
    role TEXT DEFAULT 'Student' CHECK(role IN ('Student', 'Instructor', 'Admin')),
    is_active INTEGER DEFAULT 1,
    created_date TEXT DEFAULT CURRENT_TIMESTAMP,
    last_login TEXT
);

CREATE TABLE student_settings (
    setting_id INTEGER PRIMARY KEY AUTOINCREMENT,
    student_id INTEGER NOT NULL,
    setting_key TEXT NOT NULL,
    setting_value TEXT,
    FOREIGN KEY (student_id) REFERENCES students(student_id),
    UNIQUE(student_id, setting_key)
);

-- ============================================================================
-- PRODUCT & BOM
-- ============================================================================

CREATE TABLE product_categories (
    category_id INTEGER PRIMARY KEY AUTOINCREMENT,
    category_name TEXT UNIQUE NOT NULL,
    description TEXT
);

CREATE TABLE products (
    product_id INTEGER PRIMARY KEY AUTOINCREMENT,
    student_id INTEGER NOT NULL,
    product_number TEXT NOT NULL,
    product_name TEXT NOT NULL,
    category_id INTEGER,
    description TEXT,
    standard_cost REAL,
    selling_price REAL,
    bom_levels INTEGER,
    assembly_time_minutes INTEGER,
    is_active INTEGER DEFAULT 1,
    FOREIGN KEY (student_id) REFERENCES students(student_id),
    FOREIGN KEY (category_id) REFERENCES product_categories(category_id),
    UNIQUE(student_id, product_number)
);

CREATE TABLE component_types (
    type_id INTEGER PRIMARY KEY AUTOINCREMENT,
    type_name TEXT UNIQUE NOT NULL,
    description TEXT
);

CREATE TABLE components (
    component_id INTEGER PRIMARY KEY AUTOINCREMENT,
    student_id INTEGER NOT NULL,
    component_number TEXT NOT NULL,
    component_name TEXT NOT NULL,
    type_id INTEGER,
    description TEXT,
    unit_cost REAL,
    unit_of_measure TEXT DEFAULT 'EA',
    lead_time_days INTEGER,
    is_subcontracted INTEGER DEFAULT 0,
    is_active INTEGER DEFAULT 1,
    FOREIGN KEY (student_id) REFERENCES students(student_id),
    FOREIGN KEY (type_id) REFERENCES component_types(type_id),
    UNIQUE(student_id, component_number)
);

CREATE TABLE bom (
    bom_id INTEGER PRIMARY KEY AUTOINCREMENT,
    student_id INTEGER NOT NULL,
    parent_id INTEGER NOT NULL,
    component_id INTEGER NOT NULL,
    quantity REAL NOT NULL,
    bom_level INTEGER NOT NULL,
    scrap_factor REAL DEFAULT 0.02,
    FOREIGN KEY (student_id) REFERENCES students(student_id),
    FOREIGN KEY (parent_id) REFERENCES components(component_id),
    FOREIGN KEY (component_id) REFERENCES components(component_id),
    UNIQUE(student_id, parent_id, component_id)
);

-- ============================================================================
-- WORK CENTERS & ROUTING
-- ============================================================================

CREATE TABLE work_center_types (
    wc_type_id INTEGER PRIMARY KEY AUTOINCREMENT,
    type_name TEXT UNIQUE NOT NULL,
    is_automated INTEGER DEFAULT 0
);

CREATE TABLE work_centers (
    work_center_id INTEGER PRIMARY KEY AUTOINCREMENT,
    student_id INTEGER NOT NULL,
    work_center_code TEXT NOT NULL,
    work_center_name TEXT NOT NULL,
    wc_type_id INTEGER,
    capacity_units_per_hour REAL,
    operating_cost_per_hour REAL,
    setup_time_minutes INTEGER DEFAULT 15,
    availability_percent REAL DEFAULT 90.0,
    investment_cost REAL,
    depreciation_years INTEGER,
    requires_operator INTEGER DEFAULT 0,
    is_bottleneck INTEGER DEFAULT 0,
    FOREIGN KEY (student_id) REFERENCES students(student_id),
    FOREIGN KEY (wc_type_id) REFERENCES work_center_types(wc_type_id),
    UNIQUE(student_id, work_center_code)
);

CREATE TABLE routings (
    routing_id INTEGER PRIMARY KEY AUTOINCREMENT,
    student_id INTEGER NOT NULL,
    product_id INTEGER NOT NULL,
    operation_seq INTEGER NOT NULL,
    work_center_id INTEGER NOT NULL,
    operation_name TEXT,
    setup_time_minutes INTEGER,
    cycle_time_minutes REAL,
    labor_hours REAL,
    FOREIGN KEY (student_id) REFERENCES students(student_id),
    FOREIGN KEY (product_id) REFERENCES products(product_id),
    FOREIGN KEY (work_center_id) REFERENCES work_centers(work_center_id),
    UNIQUE(student_id, product_id, operation_seq)
);

-- ============================================================================
-- LABOR RESOURCES
-- ============================================================================

CREATE TABLE operator_roles (
    role_id INTEGER PRIMARY KEY AUTOINCREMENT,
    role_name TEXT UNIQUE NOT NULL,
    base_rate_per_hour REAL,
    loaded_rate_per_hour REAL
);

CREATE TABLE operators (
    operator_id INTEGER PRIMARY KEY AUTOINCREMENT,
    student_id INTEGER NOT NULL,
    operator_number TEXT NOT NULL,
    operator_name TEXT NOT NULL,
    role_id INTEGER,
    skill_level INTEGER DEFAULT 1,
    hourly_rate REAL,
    is_active INTEGER DEFAULT 1,
    FOREIGN KEY (student_id) REFERENCES students(student_id),
    FOREIGN KEY (role_id) REFERENCES operator_roles(role_id),
    UNIQUE(student_id, operator_number)
);

-- ============================================================================
-- PRODUCTION ORDERS
-- ============================================================================

CREATE TABLE production_orders (
    order_id INTEGER PRIMARY KEY AUTOINCREMENT,
    student_id INTEGER NOT NULL,
    order_number TEXT NOT NULL,
    product_id INTEGER NOT NULL,
    quantity INTEGER NOT NULL,
    due_date TEXT,
    priority INTEGER DEFAULT 1,
    status TEXT DEFAULT 'Planned' CHECK(status IN ('Planned', 'Released', 'InProgress', 'Completed', 'Cancelled')),
    release_date TEXT,
    start_date TEXT,
    completion_date TEXT,
    created_date TEXT DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (student_id) REFERENCES students(student_id),
    FOREIGN KEY (product_id) REFERENCES products(product_id),
    UNIQUE(student_id, order_number)
);

-- ============================================================================
-- INDEXES
-- ============================================================================

CREATE INDEX idx_products_student ON products(student_id);
CREATE INDEX idx_products_category ON products(category_id);
CREATE INDEX idx_components_student ON components(student_id);
CREATE INDEX idx_work_centers_student ON work_centers(student_id);
CREATE INDEX idx_routings_student ON routings(student_id);
CREATE INDEX idx_routings_product ON routings(product_id);
CREATE INDEX idx_routings_wc ON routings(work_center_id);
CREATE INDEX idx_prod_orders_student ON production_orders(student_id);
CREATE INDEX idx_prod_orders_product ON production_orders(product_id);
CREATE INDEX idx_prod_orders_status ON production_orders(status);
