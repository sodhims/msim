using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using ManufacturingSimulation.Database;
using ManufacturingSimulation.Database.Models;

namespace ManufacturingSimulation.WPF.ViewModels
{
    public class RoutingManagementViewModel : INotifyPropertyChanged
    {
        private readonly MesDbContext _context;
        private Student _currentStudent;
        private Product _selectedProduct;
        private string _statusMessage;
        private bool _hasChanges;

        public RoutingManagementViewModel(MesDbContext context, Student currentStudent)
        {
            _context = context;
            _currentStudent = currentStudent;
            _statusMessage = "Ready";

            // Initialize commands
            AddOperationCommand = new RelayCommand(AddOperation, CanAddOperation);
            DeleteOperationCommand = new RelayCommand<RoutingOperationViewModel>(DeleteOperation, CanDeleteOperation);
            MoveOperationUpCommand = new RelayCommand<RoutingOperationViewModel>(MoveOperationUp, CanMoveOperationUp);
            MoveOperationDownCommand = new RelayCommand<RoutingOperationViewModel>(MoveOperationDown, CanMoveOperationDown);
            SaveChangesCommand = new RelayCommand(SaveChanges, () => HasChanges);

            // Load data
            LoadData();
        }

        #region Properties

        public Student CurrentStudent
        {
            get => _currentStudent;
            set => SetProperty(ref _currentStudent, value);
        }

        public ObservableCollection<Product> Products { get; set; } = new();
        public ObservableCollection<WorkCenter> WorkCenters { get; set; } = new();
        public ObservableCollection<RoutingOperationViewModel> Operations { get; set; } = new();

        public Product SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (SetProperty(ref _selectedProduct, value))
                {
                    LoadOperations();
                    ((RelayCommand)AddOperationCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool HasChanges
        {
            get => _hasChanges;
            set
            {
                if (SetProperty(ref _hasChanges, value))
                {
                    ((RelayCommand)SaveChangesCommand).RaiseCanExecuteChanged();
                }
            }
        }

        #endregion

        #region Commands

        public ICommand AddOperationCommand { get; }
        public ICommand DeleteOperationCommand { get; }
        public ICommand MoveOperationUpCommand { get; }
        public ICommand MoveOperationDownCommand { get; }
        public ICommand SaveChangesCommand { get; }

        #endregion

        #region Data Loading

        private void LoadData()
        {
            try
            {
                // Load products for current student
                var products = _context.Products
                    .Where(p => p.StudentId == _currentStudent.StudentId)
                    .OrderBy(p => p.ProductName)
                    .ToList();

                Products.Clear();
                foreach (var product in products)
                {
                    Products.Add(product);
                }

                // Load work centers for current student
                var workCenters = _context.WorkCenters
                    .Where(wc => wc.StudentId == _currentStudent.StudentId)
                    .OrderBy(wc => wc.WorkCenterName)
                    .ToList();

                WorkCenters.Clear();
                foreach (var wc in workCenters)
                {
                    WorkCenters.Add(wc);
                }

                StatusMessage = $"Loaded {Products.Count} products and {WorkCenters.Count} work centers";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading data: {ex.Message}";
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadOperations()
        {
            Operations.Clear();

            if (SelectedProduct == null)
                return;

            try
            {
                var routings = _context.Routings
                    .Where(r => r.ProductId == SelectedProduct.ProductId)
                    .OrderBy(r => r.OperationSeq)
                    .ToList();

                foreach (var routing in routings)
                {
                    var workCenter = WorkCenters.FirstOrDefault(wc => wc.WorkCenterId == routing.WorkCenterId);
                    var opVm = new RoutingOperationViewModel(routing, workCenter);
                    opVm.PropertyChanged += Operation_PropertyChanged;
                    Operations.Add(opVm);
                }

                StatusMessage = $"Loaded {Operations.Count} operations for {SelectedProduct.ProductName}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading operations: {ex.Message}";
                MessageBox.Show($"Error loading operations: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Command Implementations

        private bool CanAddOperation()
        {
            return SelectedProduct != null && WorkCenters.Count > 0;
        }

        private void AddOperation()
        {
            try
            {
                int nextSeq = Operations.Any() ? Operations.Max(o => o.OperationSeq) + 1 : 1;

                var newRouting = new Routing
                {
                    StudentId = _currentStudent.StudentId,
                    ProductId = SelectedProduct.ProductId,
                    WorkCenterId = WorkCenters.First().WorkCenterId,
                    OperationSeq = nextSeq,
                    CycleTimeMinutes = 10.0,
                    SetupTimeMinutes = 5
                };

                _context.Routings.Add(newRouting);

                var opVm = new RoutingOperationViewModel(newRouting, WorkCenters.First());
                opVm.PropertyChanged += Operation_PropertyChanged;
                Operations.Add(opVm);

                HasChanges = true;
                StatusMessage = $"Added operation {nextSeq}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error adding operation: {ex.Message}";
                MessageBox.Show($"Error adding operation: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanDeleteOperation(RoutingOperationViewModel operation)
        {
            return operation != null;
        }

        private void DeleteOperation(RoutingOperationViewModel operation)
        {
            try
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete operation {operation.OperationSeq}?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                operation.PropertyChanged -= Operation_PropertyChanged;
                Operations.Remove(operation);

                // Mark for deletion in context
                if (operation.Routing.RoutingId > 0)
                {
                    _context.Routings.Remove(operation.Routing);
                }

                // Renumber remaining operations
                RenumberOperations();

                HasChanges = true;
                StatusMessage = $"Deleted operation";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error deleting operation: {ex.Message}";
                MessageBox.Show($"Error deleting operation: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanMoveOperationUp(RoutingOperationViewModel operation)
        {
            return operation != null && Operations.IndexOf(operation) > 0;
        }

        private void MoveOperationUp(RoutingOperationViewModel operation)
        {
            int index = Operations.IndexOf(operation);
            if (index > 0)
            {
                Operations.Move(index, index - 1);
                RenumberOperations();
                HasChanges = true;
                StatusMessage = "Moved operation up";
            }
        }

        private bool CanMoveOperationDown(RoutingOperationViewModel operation)
        {
            return operation != null && Operations.IndexOf(operation) < Operations.Count - 1;
        }

        private void MoveOperationDown(RoutingOperationViewModel operation)
        {
            int index = Operations.IndexOf(operation);
            if (index < Operations.Count - 1)
            {
                Operations.Move(index, index + 1);
                RenumberOperations();
                HasChanges = true;
                StatusMessage = "Moved operation down";
            }
        }

        private void SaveChanges()
        {
            try
            {
                _context.SaveChanges();
                HasChanges = false;
                StatusMessage = "Changes saved successfully";
                MessageBox.Show("All changes have been saved.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving changes: {ex.Message}";
                MessageBox.Show($"Error saving changes: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Helper Methods

        private void RenumberOperations()
        {
            int seq = 1;
            foreach (var op in Operations)
            {
                op.OperationSeq = seq++;
            }
        }

        private void Operation_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            HasChanges = true;
            StatusMessage = "Unsaved changes";
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// ViewModel wrapper for Routing entity to support UI binding
    /// </summary>
    public class RoutingOperationViewModel : INotifyPropertyChanged
    {
        private WorkCenter _selectedWorkCenter;

        public RoutingOperationViewModel(Routing routing, WorkCenter workCenter)
        {
            Routing = routing;
            _selectedWorkCenter = workCenter;
        }

        public Routing Routing { get; }

        public int OperationSeq
        {
            get => Routing.OperationSeq;
            set
            {
                if (Routing.OperationSeq != value)
                {
                    Routing.OperationSeq = value;
                    OnPropertyChanged();
                }
            }
        }

        public WorkCenter SelectedWorkCenter
        {
            get => _selectedWorkCenter;
            set
            {
                if (_selectedWorkCenter != value)
                {
                    _selectedWorkCenter = value;
                    if (value != null)
                    {
                        Routing.WorkCenterId = value.WorkCenterId;
                    }
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(WorkCenterName));
                }
            }
        }

        public string WorkCenterName => SelectedWorkCenter?.WorkCenterName ?? "Not Set";

        public double? CycleTimeMinutes
        {
            get => Routing.CycleTimeMinutes;
            set
            {
                if (Routing.CycleTimeMinutes != value)
                {
                    Routing.CycleTimeMinutes = value;
                    OnPropertyChanged();
                }
            }
        }

        public int? SetupTimeMinutes
        {
            get => Routing.SetupTimeMinutes;
            set
            {
                if (Routing.SetupTimeMinutes != value)
                {
                    Routing.SetupTimeMinutes = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
