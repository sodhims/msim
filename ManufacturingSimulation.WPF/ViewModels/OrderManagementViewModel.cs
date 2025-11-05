using ManufacturingSimulation.Database;
using ManufacturingSimulation.Database.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace ManufacturingSimulation.WPF.ViewModels
{
    public class OrderManagementViewModel : INotifyPropertyChanged
    {
        private readonly MesDbContext _context;
        private ObservableCollection<ProductionOrder> _orders;
        private ProductionOrder _selectedOrder;
        private ObservableCollection<Product> _availableProducts;
        private string _statusMessage;
        private bool _showCompletedOrders = false;
        private List<ProductionOrder> _currentlySelectedOrders = new List<ProductionOrder>(); // ← ADD THIS


        public OrderManagementViewModel(MesDbContext context)
        {
            _context = context;
            Orders = new ObservableCollection<ProductionOrder>();
            AvailableProducts = new ObservableCollection<Product>();
            SelectedOrders = new ObservableCollection<ProductionOrder>();
            
            InitializeCommands();
            _ = LoadDataAsync();
        }

        #region Properties

        public ObservableCollection<ProductionOrder> Orders
        {
            get => _orders;
            set => SetProperty(ref _orders, value);
        }

        public ObservableCollection<ProductionOrder> SelectedOrders { get; set; }

        public ProductionOrder SelectedOrder
        {
            get => _selectedOrder;
            set => SetProperty(ref _selectedOrder, value);
        }

        public ObservableCollection<Product> AvailableProducts
        {
            get => _availableProducts;
            set => SetProperty(ref _availableProducts, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool ShowCompletedOrders
        {
            get => _showCompletedOrders;
            set
            {
                if (SetProperty(ref _showCompletedOrders, value))
                {
                    _ = LoadOrdersAsync();
                }
            }
        }

        public int TotalOrders => Orders.Count;
        public int SelectedOrderCount => _currentlySelectedOrders?.Count ?? 0;  
        public int PendingOrderCount => Orders.Count(o => o.Status == "Pending");

        #endregion

        #region Commands
        public void SetSelectedOrders(List<ProductionOrder> selectedOrders)
        {
            _currentlySelectedOrders = selectedOrders ?? new List<ProductionOrder>();
        }
        public ICommand AddOrderCommand { get; private set; }
        public ICommand EditOrderCommand { get; private set; }
        public ICommand DeleteOrderCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand SelectAllCommand { get; private set; }
        public ICommand ClearSelectionCommand { get; private set; }
        public ICommand RunSimulationCommand { get; private set; }

        private void InitializeCommands()
        {
            AddOrderCommand = new RelayCommand(async () => await AddOrderAsync());
            EditOrderCommand = new RelayCommand(async () => await EditOrderAsync());
            DeleteOrderCommand = new RelayCommand(async () => await DeleteOrderAsync());
            RefreshCommand = new RelayCommand(async () => await LoadOrdersAsync());
//            SelectAllCommand = new RelayCommand(SelectAllOrders);
//            ClearSelectionCommand = new RelayCommand(ClearSelection);
            RunSimulationCommand = new RelayCommand(RunSimulation);
        }

        #endregion

        #region Data Loading

        private async Task LoadDataAsync()
        {
            await LoadProductsAsync();
            await LoadOrdersAsync();
        }

        private async Task LoadProductsAsync()
        {
            try
            {
                var products = await _context.Products
                    .Where(p => p.IsActive == true)
                    .OrderBy(p => p.ProductName)
                    .ToListAsync();

                AvailableProducts.Clear();
                foreach (var product in products)
                {
                    AvailableProducts.Add(product);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading products: {ex.Message}";
            }
        }

        private async Task LoadOrdersAsync()
        {
            try
            {
                StatusMessage = "Loading orders...";

                var query = _context.ProductionOrders
                    .Include(o => o.Product)
                    .AsQueryable();

                if (!ShowCompletedOrders)
                {
                    query = query.Where(o => o.Status != "Completed");
                }

                var orders = await query
                    .OrderBy(o => o.DueDate)
                    .ThenBy(o => o.Priority)
                    .ToListAsync();

                Orders.Clear();
                foreach (var order in orders)
                {
                    Orders.Add(order);
                }

                StatusMessage = $"Loaded {Orders.Count} orders";
                OnPropertyChanged(nameof(TotalOrders));
                OnPropertyChanged(nameof(PendingOrderCount));
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading orders: {ex.Message}";
                MessageBox.Show($"Failed to load orders: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Order Operations

        private async Task AddOrderAsync()
        {
            MessageBox.Show("Use Database Admin → production_orders → Add New to create orders",
                "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            // Or just skip this for now
        }

        private async Task EditOrderAsync()
        {
            if (_currentlySelectedOrders.Count == 0)
            {
                MessageBox.Show("Please select an order to edit.",
                    "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_currentlySelectedOrders.Count > 1)
            {
                MessageBox.Show("Please select only one order to edit.",
                    "Multiple Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var orderToEdit = _currentlySelectedOrders[0];

            // Simple edit dialog
            var editWindow = new Window
            {
                Title = $"Edit Order {orderToEdit.OrderNumber}",
                Width = 500,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var grid = new System.Windows.Controls.Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Auto) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Auto) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Auto) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Auto) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Auto) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Auto) });

            // Order Number (read-only)
            var lblOrderNumber = new System.Windows.Controls.TextBlock { Text = "Order Number:", Margin = new Thickness(0, 10, 0, 5) };
            var txtOrderNumber = new System.Windows.Controls.TextBox { Text = orderToEdit.OrderNumber, IsReadOnly = true, Background = System.Windows.Media.Brushes.LightGray };
            System.Windows.Controls.Grid.SetRow(lblOrderNumber, 0);
            System.Windows.Controls.Grid.SetRow(txtOrderNumber, 1);

            // Quantity
            var lblQuantity = new System.Windows.Controls.TextBlock { Text = "Quantity:", Margin = new Thickness(0, 10, 0, 5) };
            var txtQuantity = new System.Windows.Controls.TextBox { Text = orderToEdit.Quantity.ToString() };
            System.Windows.Controls.Grid.SetRow(lblQuantity, 2);
            System.Windows.Controls.Grid.SetRow(txtQuantity, 3);

            // Status
            var lblStatus = new System.Windows.Controls.TextBlock { Text = "Status:", Margin = new Thickness(0, 10, 0, 5) };
            var cmbStatus = new System.Windows.Controls.ComboBox();
            cmbStatus.Items.Add("Pending");
            cmbStatus.Items.Add("Released");
            cmbStatus.Items.Add("Planned");
            cmbStatus.Items.Add("In Progress");
            cmbStatus.Items.Add("Completed");
            cmbStatus.Items.Add("Cancelled");
            cmbStatus.SelectedItem = orderToEdit.Status;
            System.Windows.Controls.Grid.SetRow(lblStatus, 4);
            System.Windows.Controls.Grid.SetRow(cmbStatus, 5);

            // Buttons
            var btnPanel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };
            var btnSave = new System.Windows.Controls.Button { Content = "Save", Width = 80, Margin = new Thickness(5) };
            var btnCancel = new System.Windows.Controls.Button { Content = "Cancel", Width = 80, Margin = new Thickness(5) };

            btnSave.Click += async (s, e) =>
            {
                try
                {
                    if (int.TryParse(txtQuantity.Text, out int newQuantity))
                    {
                        orderToEdit.Quantity = newQuantity;
                        orderToEdit.Status = cmbStatus.SelectedItem?.ToString();

                        await _context.SaveChangesAsync();
                        await LoadOrdersAsync();

                        editWindow.DialogResult = true;
                        editWindow.Close();
                    }
                    else
                    {
                        MessageBox.Show("Invalid quantity value", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            btnCancel.Click += (s, e) => { editWindow.Close(); };

            btnPanel.Children.Add(btnSave);
            btnPanel.Children.Add(btnCancel);
            System.Windows.Controls.Grid.SetRow(btnPanel, 6);

            grid.Children.Add(lblOrderNumber);
            grid.Children.Add(txtOrderNumber);
            grid.Children.Add(lblQuantity);
            grid.Children.Add(txtQuantity);
            grid.Children.Add(lblStatus);
            grid.Children.Add(cmbStatus);
            grid.Children.Add(btnPanel);

            editWindow.Content = grid;
            editWindow.ShowDialog();
        }

        private async Task DeleteOrderAsync()
        {
            if (_currentlySelectedOrders.Count == 0)
            {
                MessageBox.Show("Please select at least one order.",
                    "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Mark {_currentlySelectedOrders.Count} order(s) as 'Cancelled'?",
                "Cancel Orders",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    StatusMessage = $"Cancelling {_currentlySelectedOrders.Count} orders...";

                    foreach (var order in _currentlySelectedOrders)
                    {
                        var orderToUpdate = await _context.ProductionOrders
                            .FirstOrDefaultAsync(o => o.OrderId == order.OrderId);

                        if (orderToUpdate != null)
                        {
                            orderToUpdate.Status = "Cancelled";
                        }
                    }

                    await _context.SaveChangesAsync();
                    await LoadOrdersAsync();

                    StatusMessage = $"{_currentlySelectedOrders.Count} order(s) cancelled";
                    MessageBox.Show($"Cancelled {_currentlySelectedOrders.Count} order(s)!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        #endregion

        #region Selection

        //private void SelectAllOrders()
        //{
        //    SelectedOrders.Clear();
        //    foreach (var order in Orders.Where(o => o.Status == "Pending"))
        //    {
        //        SelectedOrders.Add(order);
        //    }
        //    StatusMessage = $"Selected {SelectedOrders.Count} orders";
        //    OnPropertyChanged(nameof(SelectedOrderCount));
        //}

        //private void ClearSelection()
        //{
        //    SelectedOrders.Clear();
        //    StatusMessage = "Selection cleared";
        //    OnPropertyChanged(nameof(SelectedOrderCount));
        //}

        #endregion

        #region Simulation

        private void RunSimulation()
        {
            if (_currentlySelectedOrders.Count == 0)
            {
                MessageBox.Show("Please select at least one order to simulate.", "No Orders Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Filter out cancelled/completed orders
            var validOrders = _currentlySelectedOrders
                .Where(o => o.Status != "Cancelled" && o.Status != "Completed")
                .ToList();

            if (validOrders.Count == 0)
            {
                MessageBox.Show(
                    "None of the selected orders can be simulated.\n\n" +
                    "Only orders with status 'Pending', 'Released', or 'Planned' can be simulated.",
                    "Invalid Selection",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (validOrders.Count < _currentlySelectedOrders.Count)
            {
                var skipped = _currentlySelectedOrders.Count - validOrders.Count;
                var result = MessageBox.Show(
                    $"{skipped} order(s) will be skipped (Cancelled/Completed).\n\n" +
                    $"Run simulation with {validOrders.Count} valid order(s)?",
                    "Confirm Simulation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;
            }
            else
            {
                var result = MessageBox.Show(
                    $"Run simulation with {validOrders.Count} selected order(s)?",
                    "Confirm Simulation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            // Return only valid orders to caller
            SimulationRequested?.Invoke(this, validOrders);
        }
        public event EventHandler<System.Collections.Generic.List<ProductionOrder>> SimulationRequested;

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }

    // Simple RelayCommand implementation

}
