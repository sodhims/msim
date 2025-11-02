using ManufacturingSimulation.WPF.Views;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using ManufacturingSimulation.Database;
using ManufacturingSimulation.Database.Models;

namespace ManufacturingSimulation.WPF.ViewModels
{
    public class OrderManagementViewModel : INotifyPropertyChanged
    {
        private readonly MesDbContext _db;
        private ProductionOrder _selectedOrder;
        private string _searchText;
        private string _statusFilter;

        public ObservableCollection<ProductionOrder> Orders { get; set; }
        public ObservableCollection<Product> Products { get; set; }
        public ObservableCollection<string> StatusOptions { get; set; }

        public ProductionOrder SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                _selectedOrder = value;
                OnPropertyChanged(nameof(SelectedOrder));
                OnPropertyChanged(nameof(CanEdit));
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                FilterOrders();
            }
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set
            {
                _statusFilter = value;
                OnPropertyChanged(nameof(StatusFilter));
                FilterOrders();
            }
        }

        public bool CanEdit => SelectedOrder != null;

        // Commands
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RefreshCommand { get; }

        public OrderManagementViewModel()
        {
            _db = new MesDbContext();
            
            Orders = new ObservableCollection<ProductionOrder>();
            Products = new ObservableCollection<Product>();
            StatusOptions = new ObservableCollection<string>
            {
                "All",
                "Planned",
                "Released",
                "InProgress",
                "Completed",
                "Cancelled"
            };

            StatusFilter = "All";

            AddCommand = new RelayCommand(Add);
            EditCommand = new RelayCommand(() => Edit());
            DeleteCommand = new RelayCommand(() => Delete());
            RefreshCommand = new RelayCommand(Refresh);

            LoadData();
        }

        private void LoadData()
        {
            // Load products
            Products.Clear();
            var products = _db.Products
                .Where(p => p.StudentId == 1 && p.IsActive)
                .ToList();
            foreach (var product in products)
            {
                Products.Add(product);
            }

            // Load orders
            Refresh();
        }

        private void Refresh()
        {
            Orders.Clear();
            var orders = _db.ProductionOrders
                .Include(o => o.Product)
                .Where(o => o.StudentId == 1)
                .OrderByDescending(o => o.CreatedDate)
                .ToList();

            foreach (var order in orders)
            {
                Orders.Add(order);
            }

            FilterOrders();
        }

        private void FilterOrders()
        {
            var query = _db.ProductionOrders
                .Include(o => o.Product)
                .Where(o => o.StudentId == 1)
                .AsQueryable();

            // Filter by status
            if (!string.IsNullOrEmpty(StatusFilter) && StatusFilter != "All")
            {
                query = query.Where(o => o.Status == StatusFilter);
            }

            // Filter by search text
            if (!string.IsNullOrEmpty(SearchText))
            {
                query = query.Where(o => 
                    o.OrderNumber.Contains(SearchText) ||
                    o.Product.ProductName.Contains(SearchText));
            }

            Orders.Clear();
            foreach (var order in query.OrderByDescending(o => o.CreatedDate).ToList())
            {
                Orders.Add(order);
            }
        }

        private void Add()
        {
            var dialog = new OrderEditDialog
            {
                DataContext = new OrderEditViewModel(_db, Products.ToList())
            };

            if (dialog.ShowDialog() == true)
            {
                Refresh();
            }
        }

        private void Edit()
        {
            if (SelectedOrder == null) return;

            var dialog = new OrderEditDialog
            {
                DataContext = new OrderEditViewModel(_db, Products.ToList(), SelectedOrder)
            };

            if (dialog.ShowDialog() == true)
            {
                Refresh();
            }
        }

        private void Delete()
        {
            if (SelectedOrder == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Delete order {SelectedOrder.OrderNumber}?",
                "Confirm Delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    _db.ProductionOrders.Remove(SelectedOrder);
                    _db.SaveChanges();
                    Refresh();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"Error deleting order: {ex.Message}",
                        "Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // Edit dialog ViewModel
    public class OrderEditViewModel : INotifyPropertyChanged
    {
        private readonly MesDbContext _db;
        private readonly ProductionOrder _order;
        private readonly bool _isNew;

        public string OrderNumber { get; set; }
        public int SelectedProductId { get; set; }
        public int Quantity { get; set; }
        public DateTime? DueDate { get; set; }
        public int Priority { get; set; }
        public string Status { get; set; }

        public ObservableCollection<Product> Products { get; set; }
        public ObservableCollection<string> StatusOptions { get; set; }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public OrderEditViewModel(MesDbContext db, System.Collections.Generic.List<Product> products, ProductionOrder order = null)
        {
            _db = db;
            _order = order;
            _isNew = order == null;

            Products = new ObservableCollection<Product>(products);
            StatusOptions = new ObservableCollection<string>
            {
                "Planned",
                "Released",
                "InProgress",
                "Completed",
                "Cancelled"
            };

            if (_isNew)
            {
                OrderNumber = $"WO-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
                Quantity = 10;
                Priority = 1;
                Status = "Planned";
                DueDate = DateTime.Now.AddDays(7);
                SelectedProductId = products.FirstOrDefault()?.ProductId ?? 0;
            }
            else
            {
                OrderNumber = order.OrderNumber;
                SelectedProductId = order.ProductId;
                Quantity = order.Quantity;
                DueDate = order.DueDate;
                Priority = order.Priority;
                Status = order.Status;
            }

            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(() => { });
        }

        private void Save()
        {
            if (_isNew)
            {
                var newOrder = new ProductionOrder
                {
                    StudentId = 1,
                    OrderNumber = OrderNumber,
                    ProductId = SelectedProductId,
                    Quantity = Quantity,
                    DueDate = DueDate,
                    Priority = Priority,
                    Status = Status,
                    CreatedDate = DateTime.Now
                };

                if (Status == "Released")
                {
                    newOrder.ReleaseDate = DateTime.Now;
                }

                _db.ProductionOrders.Add(newOrder);
            }
            else
            {
                _order.ProductId = SelectedProductId;
                _order.Quantity = Quantity;
                _order.DueDate = DueDate;
                _order.Priority = Priority;
                _order.Status = Status;

                if (Status == "Released" && _order.ReleaseDate == null)
                {
                    _order.ReleaseDate = DateTime.Now;
                }
            }

            _db.SaveChanges();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
