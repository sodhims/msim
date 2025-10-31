using System;
using System.Windows;
using System.Windows.Data;

namespace ManufacturingSimulation
{
    /// <summary>
    /// Code-behind for ConfigurationWindow
    /// </summary>
    public partial class ConfigurationWindow : Window
    {
        public ConfigurationViewModel ViewModel { get; }

        public ConfigurationWindow()
        {
            InitializeComponent();

            ViewModel = new ConfigurationViewModel();
            DataContext = ViewModel;
        }

        public ConfigurationWindow(ConfigurationViewModel viewModel) : this()
        {
            ViewModel = viewModel;
            DataContext = ViewModel;
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}