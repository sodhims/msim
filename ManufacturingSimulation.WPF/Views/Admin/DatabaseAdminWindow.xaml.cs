using System.Windows;
using System.Windows.Controls;
using ManufacturingSimulation.WPF.ViewModels.Admin;

namespace ManufacturingSimulation.WPF.Views.Admin
{
    public partial class DatabaseAdminWindow : Window
    {
        public DatabaseAdminWindow()
        {
            InitializeComponent();
            
            // ViewModel will be set via dependency injection or manually
            // DataContext = new DatabaseAdminViewModel(dbContext, adminService);
        }

        public DatabaseAdminWindow(DatabaseAdminViewModel viewModel) : this()
        {
            DataContext = viewModel;
            Loaded += async (s, e) => await viewModel.InitializeAsync();
        }

        private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is TreeViewItem item && 
                item.DataContext is TableMetadata tableMetadata)
            {
                var viewModel = DataContext as DatabaseAdminViewModel;
                viewModel?.SelectTableCommand.Execute(tableMetadata);
                e.Handled = true;
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            var viewModel = DataContext as DatabaseAdminViewModel;
            if (viewModel?.HasSelectedRecord == true)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Do you want to save before closing?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
            }

            base.OnClosing(e);
        }
    }
}
