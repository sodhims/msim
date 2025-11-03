using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using ManufacturingSimulation.Database;
using ManufacturingSimulation.Database.Models;
using ManufacturingSimulation.WPF.Utilities;

namespace ManufacturingSimulation.WPF.ViewModels
{
    /// <summary>
    /// ViewModel for Factory Layout Editor
    /// Manages machine placement, distance calculations, and layout persistence
    /// </summary>
    public class FactoryLayoutViewModel : INotifyPropertyChanged
    {
        #region Fields

        private readonly MesDbContext _dbContext;
        private ObservableCollection<ManufacturingSimulation.Database.Models.Machine> _availableMachines;
        private ObservableCollection<MachineLocationModel> _placedMachines;
        private ObservableCollection<MachineDistance> _distances;
        private bool _showGrid;
        private bool _showDistances;
        private bool _showConnections;
        private double _canvasWidth;
        private double _canvasHeight;
        private double _gridSize;
        private string _statusMessage;
        private MachineLocationModel _selectedMachine;

        #endregion

        #region Properties

        /// <summary>
        /// Machines available to place on factory floor
        /// </summary>
        public ObservableCollection<ManufacturingSimulation.Database.Models.Machine> AvailableMachines
        {
            get => _availableMachines;
            set
            {
                _availableMachines = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Machines currently placed on factory floor
        /// </summary>
        public ObservableCollection<MachineLocationModel> PlacedMachines
        {
            get => _placedMachines;
            set
            {
                _placedMachines = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Distance matrix between all placed machines
        /// </summary>
        public ObservableCollection<MachineDistance> Distances
        {
            get => _distances;
            set
            {
                _distances = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Show grid overlay for alignment
        /// </summary>
        public bool ShowGrid
        {
            get => _showGrid;
            set
            {
                _showGrid = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Show distance labels between machines
        /// </summary>
        public bool ShowDistances
        {
            get => _showDistances;
            set
            {
                _showDistances = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Show connection lines between machines
        /// </summary>
        public bool ShowConnections
        {
            get => _showConnections;
            set
            {
                _showConnections = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Canvas width in pixels
        /// </summary>
        public double CanvasWidth
        {
            get => _canvasWidth;
            set
            {
                _canvasWidth = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Canvas height in pixels
        /// </summary>
        public double CanvasHeight
        {
            get => _canvasHeight;
            set
            {
                _canvasHeight = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Grid cell size in pixels
        /// </summary>
        public double GridSize
        {
            get => _gridSize;
            set
            {
                _gridSize = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Status message for user feedback
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Currently selected machine
        /// </summary>
        public MachineLocationModel SelectedMachine
        {
            get => _selectedMachine;
            set
            {
                _selectedMachine = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Commands

        public ICommand SaveLayoutCommand { get; }
        public ICommand LoadLayoutCommand { get; }
        public ICommand ClearLayoutCommand { get; }
        public ICommand CalculateDistancesCommand { get; }
        public ICommand ExportLayoutCommand { get; }
        public ICommand ImportLayoutCommand { get; }
        public ICommand SnapToGridCommand { get; }
        public ICommand AutoArrangeCommand { get; }

        #endregion

        #region Events

        public event EventHandler<FrameworkElement> MachineAddedToCanvas;

        #endregion

        #region Constructor

        public FactoryLayoutViewModel()
        {
            _dbContext = new MesDbContext();

            // Initialize collections
            AvailableMachines = new ObservableCollection<ManufacturingSimulation.Database.Models.Machine>();
            PlacedMachines = new ObservableCollection<MachineLocationModel>();
            Distances = new ObservableCollection<MachineDistance>();

            // Default settings
            CanvasWidth = 1000;
            CanvasHeight = 600;
            GridSize = 20;
            ShowGrid = true;
            ShowDistances = false;
            ShowConnections = true;

            // Initialize commands
            SaveLayoutCommand = new RelayCommand(SaveLayout);
            LoadLayoutCommand = new RelayCommand(LoadLayout);
            ClearLayoutCommand = new RelayCommand(ClearLayout);
            CalculateDistancesCommand = new RelayCommand(CalculateDistances);
            ExportLayoutCommand = new RelayCommand(ExportLayout);
            ImportLayoutCommand = new RelayCommand(ImportLayout);
            SnapToGridCommand = new RelayCommand(SnapToGrid);
            AutoArrangeCommand = new RelayCommand(AutoArrange);

            // Load data
            LoadMachines();
            LoadLayout();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Add machine to canvas at specified position
        /// </summary>
        public void AddMachineToCanvas(ManufacturingSimulation.Database.Models.Machine machine, double x, double y)
        {
            // Check if machine already placed
            if (PlacedMachines.Any(m => m.MachineId == machine.Id))
            {
                StatusMessage = $"Machine {machine.Name} is already placed on the floor";
                return;
            }

            // Snap to grid if enabled
            if (ShowGrid)
            {
                x = Math.Round(x / GridSize) * GridSize;
                y = Math.Round(y / GridSize) * GridSize;
            }

            var machineLocation = new MachineLocationModel
            {
                MachineId = machine.Id,
                MachineName = machine.Name,
                XCoordinate = x,
                YCoordinate = y,
                Width = 80,  // Default width
                Height = 60, // Default height
                Rotation = 0,
                IsPlaced = true
            };

            PlacedMachines.Add(machineLocation);
            StatusMessage = $"Added {machine.Name} to factory floor";

            // Recalculate distances
            if (PlacedMachines.Count > 1)
            {
                CalculateDistances();
            }
        }

        /// <summary>
        /// Update machine position during drag
        /// </summary>
        public void UpdateMachinePosition(MachineLocationModel machine, double x, double y)
        {
            machine.XCoordinate = x;
            machine.YCoordinate = y;
        }

        /// <summary>
        /// Finalize machine position after drag completes
        /// </summary>
        public void FinalizeMachinePosition(MachineLocationModel machine, double x, double y)
        {
            // Snap to grid if enabled
            if (ShowGrid)
            {
                x = Math.Round(x / GridSize) * GridSize;
                y = Math.Round(y / GridSize) * GridSize;
            }

            machine.XCoordinate = x;
            machine.YCoordinate = y;

            StatusMessage = $"Moved {machine.MachineName} to ({x:F0}, {y:F0})";

            // Recalculate distances
            CalculateDistances();
        }

        /// <summary>
        /// Remove machine from canvas
        /// </summary>
        public void RemoveMachineFromCanvas(MachineLocationModel machine)
        {
            PlacedMachines.Remove(machine);
            StatusMessage = $"Removed {machine.MachineName} from factory floor";
            CalculateDistances();
        }

        /// <summary>
        /// Rotate machine 90 degrees
        /// </summary>
        public void RotateMachine(MachineLocationModel machine)
        {
            machine.Rotation = (machine.Rotation + 90) % 360;

            // Swap width and height for 90/270 degree rotations
            if (machine.Rotation == 90 || machine.Rotation == 270)
            {
                var temp = machine.Width;
                machine.Width = machine.Height;
                machine.Height = temp;
            }

            StatusMessage = $"Rotated {machine.MachineName} to {machine.Rotation}°";
        }

        /// <summary>
        /// Edit machine properties (placeholder for future dialog)
        /// </summary>
        public void EditMachineProperties(MachineLocationModel machine)
        {
            SelectedMachine = machine;
            StatusMessage = $"Editing {machine.MachineName} properties";
            // TODO: Open properties dialog
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Load available machines from database
        /// </summary>
        private void LoadMachines()
        {
            try
            {
                var workCenters = _dbContext.WorkCenters.ToList();
                AvailableMachines.Clear();

                foreach (var wc in workCenters)
                {
                    var machine = new ManufacturingSimulation.Database.Models.Machine
                    {
                        Id = wc.WorkCenterId,
                        Name = wc.WorkCenterName,
                        MachineType = wc.WorkCenterType?.ToString() ?? "Unknown",
                        Status = "Available",
                        BufferCapacity = (int)wc.BufferCapacity,
                        ProcessingRate = wc.SetupTimeMinutes
                    };
                    AvailableMachines.Add(machine);
                }
                StatusMessage = $"Loaded {workCenters.Count} work centers";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading work centers: {ex.Message}";
            }
        }
        /// <summary>
        /// Save layout to database
        /// </summary>
        private void SaveLayout()
        {
            try
            {
                // Clear existing locations
                var existingLocations = _dbContext.MachineLocations.ToList();
                _dbContext.MachineLocations.RemoveRange(existingLocations);

                // Save new locations
                foreach (var placedMachine in PlacedMachines)
                {
                    var location = new MachineLocation
                    {
                        MachineId = placedMachine.MachineId,
                        XCoordinate = placedMachine.XCoordinate,
                        YCoordinate = placedMachine.YCoordinate,
                        Rotation = placedMachine.Rotation,
                        FloorNumber = 1,
                        Department = "Production",
                        UpdatedAt = DateTime.Now
                    };
                    _dbContext.MachineLocations.Add(location);
                }

                // Save distances
                var existingDistances = _dbContext.MachineDistances.ToList();
                _dbContext.MachineDistances.RemoveRange(existingDistances);

                foreach (var distance in Distances)
                {
                    _dbContext.MachineDistances.Add(distance);
                }

                _dbContext.SaveChanges();
                StatusMessage = $"Layout saved: {PlacedMachines.Count} machines, {Distances.Count} distances";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving layout: {ex.Message}";
            }
        }

        /// <summary>
        /// Load layout from database
        /// </summary>
        private void LoadLayout()
        {
            try
            {
                var locations = _dbContext.MachineLocations
                    .Include(ml => ml.Machine)
                    .ToList();

                PlacedMachines.Clear();
                foreach (var location in locations)
                {
                    var machineLocation = new MachineLocationModel
                    {
                        MachineId = location.MachineId,
                        MachineName = location.Machine?.Name ?? "Unknown",
                        XCoordinate = location.XCoordinate,
                        YCoordinate = location.YCoordinate,
                        Width = 80,
                        Height = 60,
                        Rotation = location.Rotation,
                        IsPlaced = true
                    };
                    PlacedMachines.Add(machineLocation);
                }

                // Load distances
                var distances = _dbContext.MachineDistances.ToList();
                Distances.Clear();
                foreach (var distance in distances)
                {
                    Distances.Add(distance);
                }

                StatusMessage = $"Layout loaded: {PlacedMachines.Count} machines";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading layout: {ex.Message}";
            }
        }

        /// <summary>
        /// Clear all machines from canvas
        /// </summary>
        private void ClearLayout()
        {
            if (MessageBox.Show("Clear all machines from factory floor?",
                "Confirm Clear",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                PlacedMachines.Clear();
                Distances.Clear();
                StatusMessage = "Factory floor cleared";
            }
        }

        /// <summary>
        /// Calculate distances between all placed machines
        /// Uses Euclidean distance in factory units (assuming 1 pixel = 0.1 meters)
        /// </summary>
        private void CalculateDistances()
        {
            Distances.Clear();

            if (PlacedMachines.Count < 2)
            {
                StatusMessage = "Need at least 2 machines to calculate distances";
                return;
            }

            int calculatedCount = 0;

            // Calculate distance between every pair of machines
            for (int i = 0; i < PlacedMachines.Count; i++)
            {
                for (int j = i + 1; j < PlacedMachines.Count; j++)
                {
                    var machine1 = PlacedMachines[i];
                    var machine2 = PlacedMachines[j];

                    // Calculate center points
                    double x1 = machine1.XCoordinate + machine1.Width / 2;
                    double y1 = machine1.YCoordinate + machine1.Height / 2;
                    double x2 = machine2.XCoordinate + machine2.Width / 2;
                    double y2 = machine2.YCoordinate + machine2.Height / 2;

                    // Euclidean distance in pixels
                    double distancePixels = Math.Sqrt(
                        Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2)
                    );

                    // Convert to meters (1 pixel = 0.1 meters)
                    double distanceMeters = distancePixels * 0.1;

                    // Estimate travel time (assuming 1 m/s for material handling)
                    double travelTimeSeconds = distanceMeters / 1.0;

                    var distance = new MachineDistance
                    {
                        FromMachineId = machine1.MachineId,
                        ToMachineId = machine2.MachineId,
                        DistanceMeters = distanceMeters,
                        TravelTimeSeconds = travelTimeSeconds,
                        UpdatedAt = DateTime.Now
                    };

                    Distances.Add(distance);

                    // Add reverse direction
                    var reverseDistance = new MachineDistance
                    {
                        FromMachineId = machine2.MachineId,
                        ToMachineId = machine1.MachineId,
                        DistanceMeters = distanceMeters,
                        TravelTimeSeconds = travelTimeSeconds,
                        UpdatedAt = DateTime.Now
                    };

                    Distances.Add(reverseDistance);
                    calculatedCount += 2;
                }
            }

            StatusMessage = $"Calculated {calculatedCount} distances between {PlacedMachines.Count} machines";
        }

        /// <summary>
        /// Export layout to CSV file
        /// </summary>
        private void ExportLayout()
        {
            // TODO: Implement CSV export
            StatusMessage = "Export not yet implemented";
        }

        /// <summary>
        /// Import layout from CSV file
        /// </summary>
        private void ImportLayout()
        {
            // TODO: Implement CSV import
            StatusMessage = "Import not yet implemented";
        }

        /// <summary>
        /// Snap all machines to grid
        /// </summary>
        private void SnapToGrid()
        {
            foreach (var machine in PlacedMachines)
            {
                machine.XCoordinate = Math.Round(machine.XCoordinate / GridSize) * GridSize;
                machine.YCoordinate = Math.Round(machine.YCoordinate / GridSize) * GridSize;
            }
            CalculateDistances();
            StatusMessage = "All machines snapped to grid";
        }

        /// <summary>
        /// Auto-arrange machines in optimal layout
        /// Uses simple row-based arrangement
        /// </summary>
        private void AutoArrange()
        {
            if (PlacedMachines.Count == 0)
            {
                StatusMessage = "No machines to arrange";
                return;
            }

            double startX = 50;
            double startY = 50;
            double spacingX = 120;
            double spacingY = 100;
            int machinesPerRow = (int)((CanvasWidth - 100) / spacingX);

            for (int i = 0; i < PlacedMachines.Count; i++)
            {
                int row = i / machinesPerRow;
                int col = i % machinesPerRow;

                PlacedMachines[i].XCoordinate = startX + col * spacingX;
                PlacedMachines[i].YCoordinate = startY + row * spacingY;
                PlacedMachines[i].Rotation = 0;
            }

            CalculateDistances();
            StatusMessage = $"Auto-arranged {PlacedMachines.Count} machines";
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Model for machine location on factory floor (UI representation)
    /// </summary>
    public class MachineLocationModel : INotifyPropertyChanged
    {
        private int _machineId;
        private string _machineName;
        private double _xCoordinate;
        private double _yCoordinate;
        private double _width;
        private double _height;
        private int _rotation;
        private bool _isPlaced;

        public int MachineId
        {
            get => _machineId;
            set { _machineId = value; OnPropertyChanged(); }
        }

        public string MachineName
        {
            get => _machineName;
            set { _machineName = value; OnPropertyChanged(); }
        }

        public double XCoordinate
        {
            get => _xCoordinate;
            set { _xCoordinate = value; OnPropertyChanged(); }
        }

        public double YCoordinate
        {
            get => _yCoordinate;
            set { _yCoordinate = value; OnPropertyChanged(); }
        }

        public double Width
        {
            get => _width;
            set { _width = value; OnPropertyChanged(); }
        }

        public double Height
        {
            get => _height;
            set { _height = value; OnPropertyChanged(); }
        }

        public int Rotation
        {
            get => _rotation;
            set { _rotation = value; OnPropertyChanged(); }
        }

        public bool IsPlaced
        {
            get => _isPlaced;
            set { _isPlaced = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    #endregion
}
