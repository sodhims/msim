using System.Windows;

namespace ManufacturingSimulation.WPF.Views
{
    public partial class OrderEditDialog : Window
    {
        public OrderEditDialog()
        {
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
