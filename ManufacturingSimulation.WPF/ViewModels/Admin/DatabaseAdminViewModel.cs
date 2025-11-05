using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ManufacturingSimulation.Database;
using ManufacturingSimulation.Database.Models;
using ManufacturingSimulation.WPF.Services;
using Microsoft.EntityFrameworkCore;

namespace ManufacturingSimulation.WPF.ViewModels.Admin
{
    public class DatabaseAdminViewModel : INotifyPropertyChanged
    {
        private readonly MesDbContext _dbContext;
        private readonly AdminService _adminService;
        private readonly int? _currentStudentId;

        private TableMetadata _currentTable;
        private ObservableCollection<object> _currentTableData;
        private object _selectedRecord;
        private ObservableCollection<PropertyViewModel> _selectedRecordProperties;
        private ObservableCollection<TableCategory> _tableCategories;
        private string _statusBarMessage;
        private bool _showAllStudents;

        public DatabaseAdminViewModel(MesDbContext dbContext, AdminService adminService, int? studentId)
        {
            _dbContext = dbContext;
            _adminService = adminService;
            _currentStudentId = studentId;

            CurrentTableData = new ObservableCollection<object>();
            SelectedRecordProperties = new ObservableCollection<PropertyViewModel>();
            TableCategories = new ObservableCollection<TableCategory>();

            InitializeCommands();
        }

        #region Properties

        public ObservableCollection<TableCategory> TableCategories
        {
            get => _tableCategories;
            set => SetProperty(ref _tableCategories, value);
        }

        public ObservableCollection<object> CurrentTableData
        {
            get => _currentTableData;
            set => SetProperty(ref _currentTableData, value);
        }

        public ObservableCollection<PropertyViewModel> SelectedRecordProperties
        {
            get => _selectedRecordProperties;
            set => SetProperty(ref _selectedRecordProperties, value);
        }

        public object SelectedRecord
        {
            get => _selectedRecord;
            set
            {
                if (SetProperty(ref _selectedRecord, value))
                {
                    LoadRecordDetails(value);
                    OnPropertyChanged(nameof(HasSelectedRecord));
                }
            }
        }

        public string StatusBarMessage
        {
            get => _statusBarMessage;
            set => SetProperty(ref _statusBarMessage, value);
        }

        public string StatusMessage => _currentTable != null
            ? $"Viewing: {_currentTable.DisplayName}"
            : "Select a table to begin";

        public string CurrentTableDisplayName => _currentTable?.DisplayName ?? "No table selected";

        public int CurrentTableRecordCount => CurrentTableData?.Count ?? 0;

        public bool HasSelectedRecord => SelectedRecord != null;

        public bool HasRecords => CurrentTableData?.Count > 0;

        public bool ShowAllStudents
        {
            get => _showAllStudents;
            set
            {
                if (SetProperty(ref _showAllStudents, value))
                {
                    _ = RefreshCurrentTableAsync();
                }
            }
        }

        public string CurrentStudentName => _currentStudentId.HasValue
            ? $"Student {_currentStudentId}"
            : "Administrator";

        public bool IsAdminUser => !_currentStudentId.HasValue;

        public string DatabasePath => _dbContext.Database.GetConnectionString() ?? "Unknown";

        public bool IsConnected => _dbContext.Database.CanConnect();

        #endregion

        #region Commands

        public ICommand SelectTableCommand { get; private set; }
        public ICommand AddRecordCommand { get; private set; }
        public ICommand SaveRecordCommand { get; private set; }
        public ICommand DeleteRecordCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand ExportCsvCommand { get; private set; }
        public ICommand EditRecordCommand { get; private set; }
        public ICommand DuplicateRecordCommand { get; private set; }
        public ICommand SaveChangesCommand { get; private set; }
        public ICommand CancelEditCommand { get; private set; }

        private void InitializeCommands()
        {
            SelectTableCommand = new RelayCommand<TableMetadata>(async table => await SelectTableAsync(table));
            AddRecordCommand = new RelayCommand(async () => await AddRecordAsync(), () => _currentTable != null);
            SaveRecordCommand = new RelayCommand(async () => await SaveRecordAsync(), () => SelectedRecord != null);
            DeleteRecordCommand = new RelayCommand(async () => await DeleteRecordAsync(), () => SelectedRecord != null);
            RefreshCommand = new RelayCommand(async () => await RefreshCurrentTableAsync());
            ExportCsvCommand = new RelayCommand(async () => await ExportToCsvAsync(), () => HasRecords);
            EditRecordCommand = new RelayCommand(() => { /* Record is already loaded for edit */ }, () => SelectedRecord != null);
            DuplicateRecordCommand = new RelayCommand(async () => await DuplicateRecordAsync(), () => SelectedRecord != null);
            SaveChangesCommand = new RelayCommand(async () => await SaveRecordAsync());
            CancelEditCommand = new RelayCommand(() => SelectedRecord = null);
        }

        #endregion

        #region Initialization

        public async Task InitializeAsync()
        {
            try
            {
                StatusBarMessage = "Loading table metadata...";
                await LoadTableMetadataAsync();
                StatusBarMessage = "Ready";
            }
            catch (Exception ex)
            {
                StatusBarMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Failed to initialize: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadTableMetadataAsync()
        {
            await Task.Run(() =>
            {
                var tables = TableMetadataRegistry.GetRegistry(_dbContext);
                var categories = tables.GroupBy(t => t.Category)
                    .OrderBy(g => GetCategoryOrder(g.Key))
                    .Select(g => new TableCategory
                    {
                        CategoryName = g.Key,
                        Tables = new ObservableCollection<TableMetadata>(g.OrderBy(t => t.DisplayName))
                    });

                Application.Current.Dispatcher.Invoke(() =>
                {
                    TableCategories.Clear();
                    foreach (var category in categories)
                    {
                        TableCategories.Add(category);
                    }
                });
            });
        }

        private int GetCategoryOrder(string category)
        {
            return category switch
            {
                "MES Data" => 1,
                "Factory Layout" => 2,
                "Simulation" => 3,
                "System" => 4,
                _ => 99
            };
        }

        #endregion

        #region Table Operations

        private async Task SelectTableAsync(TableMetadata table)
        {
            if (table == null) return;

            try
            {
                _currentTable = table;
                StatusBarMessage = $"Loading {table.DisplayName}...";

                await LoadTableDataAsync(table);

                StatusBarMessage = $"{table.DisplayName}: {CurrentTableData.Count} records";
                OnPropertyChanged(nameof(StatusMessage));
                OnPropertyChanged(nameof(CurrentTableDisplayName));
                OnPropertyChanged(nameof(CurrentTableRecordCount));
            }
            catch (Exception ex)
            {
                StatusBarMessage = $"Error loading table: {ex.Message}";
                MessageBox.Show($"Failed to load table data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadTableDataAsync(TableMetadata table)
        {
            var studentId = ShowAllStudents ? null : _currentStudentId;
            var data = await _adminService.GetAllAsync(table.EntityType, studentId);

            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentTableData.Clear();
                foreach (var item in data)
                {
                    CurrentTableData.Add(item);
                }
            });
        }

        private async Task RefreshCurrentTableAsync()
        {
            if (_currentTable != null)
            {
                await SelectTableAsync(_currentTable);
            }
        }

        #endregion

        #region Record Operations

        private void LoadRecordDetails(object record)
        {
            if (record == null)
            {
                SelectedRecordProperties = new ObservableCollection<PropertyViewModel>();
                return;
            }

            var properties = new ObservableCollection<PropertyViewModel>();
            var type = record.GetType();

            foreach (var prop in type.GetProperties())
            {
                // Skip navigation properties and collections
                if (prop.PropertyType.Namespace?.StartsWith("ManufacturingSimulation") == true)
                    continue;
                if (typeof(System.Collections.IEnumerable).IsAssignableFrom(prop.PropertyType) && prop.PropertyType != typeof(string))
                    continue;

                var propVM = new PropertyViewModel
                {
                    PropertyInfo = prop,
                    Entity = record,
                    DisplayName = FormatPropertyName(prop.Name),
                    EditValue = prop.GetValue(record),
                    IsReadOnly = prop.Name.EndsWith("Id") && prop.Name != "StudentId"
                };

                properties.Add(propVM);
            }

            SelectedRecordProperties = properties;
        }

        private string FormatPropertyName(string propertyName)
        {
            // Convert "WorkCenterId" to "Work Center Id"
            return System.Text.RegularExpressions.Regex.Replace(
                propertyName,
                "([a-z])([A-Z])",
                "$1 $2"
            );
        }

        private async Task AddRecordAsync()
        {
            try
            {
                if (_currentTable == null) return;

                var newRecord = Activator.CreateInstance(_currentTable.EntityType);

                // Set student_id if applicable
                if (_currentStudentId.HasValue && _currentTable.HasStudentId)
                {
                    var studentIdProp = _currentTable.EntityType.GetProperty("StudentId")
                        ?? _currentTable.EntityType.GetProperty("student_id");
                    studentIdProp?.SetValue(newRecord, _currentStudentId.Value);
                }

                // Set default values
                SetDefaultValues(newRecord);

                // Just set as selected - let user fill it in
                SelectedRecord = newRecord;

                StatusBarMessage = "New record created. Fill in fields and click Save Record.";
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? "No inner exception";
                MessageBox.Show(
                    $"Failed to add record:\n\nError: {ex.Message}\n\nInner: {innerMsg}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task SaveRecordAsync()
        {
            try
            {
                if (SelectedRecord == null) return;

                // Apply changes from property view models
                foreach (var propVM in SelectedRecordProperties)
                {
                    if (propVM.IsReadOnly || !propVM.PropertyInfo.CanWrite)
                        continue; // Skip read-only properties

                    try
                    {
                        propVM.ApplyChanges(); // Use the built-in method
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error setting {propVM.DisplayName}: {ex.Message}",
                            "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Check if it's a new record (not yet in database)
                var entry = _dbContext.Entry(SelectedRecord);
                if (entry.State == EntityState.Detached)
                {
                    _dbContext.Add(SelectedRecord);
                }

                await _dbContext.SaveChangesAsync();

                // Add to grid if not already there
                if (!CurrentTableData.Contains(SelectedRecord))
                {
                    CurrentTableData.Add(SelectedRecord);
                }

                StatusBarMessage = "Record saved successfully";
                OnPropertyChanged(nameof(CurrentTableRecordCount));

                // Refresh to show updated data
                await RefreshCurrentTableAsync();
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? "No inner exception";
                MessageBox.Show(
                    $"Failed to save:\n\nError: {ex.Message}\n\nInner: {innerMsg}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        private async Task DeleteRecordAsync()
        {
            try
            {
                if (SelectedRecord == null) return;

                var result = MessageBox.Show(
                    "Are you sure you want to delete this record?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await _adminService.DeleteAsync(SelectedRecord);
                    CurrentTableData.Remove(SelectedRecord);
                    SelectedRecord = null;

                    StatusBarMessage = "Record deleted successfully";
                    OnPropertyChanged(nameof(CurrentTableRecordCount));
                }
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? "No inner exception";
                MessageBox.Show(
                    $"Failed to delete:\n\nError: {ex.Message}\n\nInner: {innerMsg}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task DuplicateRecordAsync()
        {
            try
            {
                if (SelectedRecord == null) return;

                var duplicate = Activator.CreateInstance(SelectedRecord.GetType());

                foreach (var prop in SelectedRecord.GetType().GetProperties())
                {
                    if (prop.Name.EndsWith("Id") || !prop.CanWrite)
                        continue;

                    var value = prop.GetValue(SelectedRecord);
                    prop.SetValue(duplicate, value);
                }

                SelectedRecord = duplicate;
                StatusBarMessage = "Record duplicated. Modify and save.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to duplicate: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetDefaultValues(object entity)
        {
            var type = entity.GetType();

            // Set default dates
            foreach (var prop in type.GetProperties())
            {
                if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?))
                {
                    if (prop.Name.Contains("Created"))
                    {
                        prop.SetValue(entity, DateTime.UtcNow);
                    }
                }
                else if (prop.PropertyType == typeof(bool) && prop.Name == "IsActive")
                {
                    prop.SetValue(entity, true);
                }
            }
        }

        #endregion

        #region Export

        private async Task ExportToCsvAsync()
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    FileName = $"{_currentTable?.TableName}_{DateTime.Now:yyyyMMdd}.csv"
                };

                if (dialog.ShowDialog() == true)
                {
                    await _adminService.ExportToCsvAsync(CurrentTableData.ToList(), dialog.FileName);
                    StatusBarMessage = $"Exported {CurrentTableData.Count} records to {dialog.FileName}";
                    MessageBox.Show("Export completed successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
}
