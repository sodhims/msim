using System.Windows;
using ManufacturingSimulation.Database;
using ManufacturingSimulation.Database.Models;
using ManufacturingSimulation.WPF.ViewModels;

namespace ManufacturingSimulation.WPF.Views
{
    public partial class RoutingManagementWindow : Window
    {
        public RoutingManagementWindow(MesDbContext context, Student currentStudent)
        {
            InitializeComponent();
            DataContext = new RoutingManagementViewModel(context, currentStudent);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as RoutingManagementViewModel;
            
            if (viewModel?.HasChanges == true)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Do you want to save before closing?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                switch (result)
                {
                    case MessageBoxResult.Yes:
                        viewModel.SaveChangesCommand.Execute(null);
                        DialogResult = true;
                        break;
                    case MessageBoxResult.No:
                        DialogResult = false;
                        break;
                    case MessageBoxResult.Cancel:
                        return; // Don't close
                }
            }

            Close();
        }
    }
}
