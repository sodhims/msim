using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ManufacturingSimulation.WPF.ViewModels;

namespace ManufacturingSimulation.WPF.Views
{
    /// <summary>
    /// Interaction logic for FactoryLayoutView.xaml
    /// Handles drag-and-drop for machine placement on factory floor
    /// </summary>
    public partial class FactoryLayoutView : UserControl
    {
        private Point _dragStartPoint;
        private bool _isDragging;
        private FrameworkElement _draggedElement;
        private Point _elementStartPosition;

        public FactoryLayoutView()
        {
            InitializeComponent();

            // Subscribe to ViewModel events for canvas updates
            this.Loaded += FactoryLayoutView_Loaded;
        }

        private void FactoryLayoutView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is FactoryLayoutViewModel viewModel)
            {
                // Subscribe to machine added event to enable drag-drop on new elements
                viewModel.MachineAddedToCanvas += OnMachineAddedToCanvas;
            }
        }

        #region Canvas Drag-Drop Events

        /// <summary>
        /// Start drag operation when mouse is pressed on canvas
        /// </summary>

        /// <summary>
        /// Handle mouse move to update element position during drag
        /// </summary>
        private void LayoutCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _draggedElement != null && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPoint = e.GetPosition(LayoutCanvas);

                // Calculate new position
                double newX = _elementStartPosition.X + (currentPoint.X - _dragStartPoint.X);
                double newY = _elementStartPosition.Y + (currentPoint.Y - _dragStartPoint.Y);

                // Constrain to canvas bounds
                double elementWidth = _draggedElement.ActualWidth;
                double elementHeight = _draggedElement.ActualHeight;

                newX = Math.Max(0, Math.Min(newX, LayoutCanvas.ActualWidth - elementWidth));
                newY = Math.Max(0, Math.Min(newY, LayoutCanvas.ActualHeight - elementHeight));

                // Update position
                Canvas.SetLeft(_draggedElement, newX);
                Canvas.SetTop(_draggedElement, newY);

                // Update ViewModel if applicable
                if (DataContext is FactoryLayoutViewModel viewModel &&
                    _draggedElement.DataContext is MachineLocationModel machine)
                {
                    // Convert canvas coordinates to factory coordinates
                    viewModel.UpdateMachinePosition(machine, newX, newY);
                }

                e.Handled = true;
            }
        }

        /// <summary>
        /// Complete drag operation and finalize position
        /// </summary>
        private void LayoutCanvas_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging && _draggedElement != null)
            {
                try
                {
                    _draggedElement.ReleaseMouseCapture();
                    Panel.SetZIndex(_draggedElement, 0);

                    // Finalize position in ViewModel
                    if (DataContext is FactoryLayoutViewModel viewModel &&
                        _draggedElement.DataContext is MachineLocationModel machine)
                    {
                        double finalX = Canvas.GetLeft(_draggedElement);
                        double finalY = Canvas.GetTop(_draggedElement);

                        viewModel.FinalizeMachinePosition(machine, finalX, finalY);
                    }
                }
                catch { }
                finally
                {
                    _isDragging = false;
                    _draggedElement = null;
                }

                e.Handled = true;
            }
        }
        private void LayoutCanvas_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject source = e.OriginalSource as DependencyObject;
            while (source != null && source != LayoutCanvas)
            {
                if (source is Grid grid && grid.DataContext is MachineLocationModel)
                {
                    _draggedElement = grid;
                    _dragStartPoint = e.GetPosition(LayoutCanvas);
                    _elementStartPosition = new Point(
                        Canvas.GetLeft(_draggedElement),
                        Canvas.GetTop(_draggedElement)
                    );

                    if (double.IsNaN(_elementStartPosition.X)) _elementStartPosition.X = 0;
                    if (double.IsNaN(_elementStartPosition.Y)) _elementStartPosition.Y = 0;

                    _isDragging = true;
                    _draggedElement.CaptureMouse();
                    Panel.SetZIndex(_draggedElement, 999);
                    e.Handled = true;
                    return;
                }
                source = VisualTreeHelper.GetParent(source);
            }
        }

        /// <summary>
        /// Cancel drag if mouse leaves canvas
        /// </summary>
        private void LayoutCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_isDragging && _draggedElement != null)
            {
                // Reset to original position
                Canvas.SetLeft(_draggedElement, _elementStartPosition.X);
                Canvas.SetTop(_draggedElement, _elementStartPosition.Y);

                _draggedElement.ReleaseMouseCapture();
                Panel.SetZIndex(_draggedElement, 0);
                _isDragging = false;
                _draggedElement = null;
            }
        }

        #endregion

        #region ListBox Drag-Drop Events

        /// <summary>
        /// Start drag from machine list to canvas
        /// </summary>
        private void MachineList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Find the ListBoxItem from any clicked element
            var listBoxItem = FindParent<ListBoxItem>((DependencyObject)e.OriginalSource);
            if (listBoxItem != null)
            {
                _draggedElement = listBoxItem;
                _dragStartPoint = e.GetPosition(null);
                e.Handled = true;
            }
        }

        // Helper method to find parent of specific type
        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;

            T parent = parentObject as T;
            return parent ?? FindParent<T>(parentObject);
        }
        /// <summary>
        /// Initiate drag-drop operation from list
        /// </summary>
        /// 
        private void MachineList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (MachineList.SelectedItem != null && DataContext is FactoryLayoutViewModel viewModel)
            {
                var machine = (ManufacturingSimulation.Database.Models.Machine)MachineList.SelectedItem;
                // Add to center of canvas
                viewModel.AddMachineToCanvas(machine, 400, 250);
            }
        }
        private void MachineList_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1 && MachineList.SelectedItem != null && DataContext is FactoryLayoutViewModel viewModel)
            {
                var machine = (ManufacturingSimulation.Database.Models.Machine)MachineList.SelectedItem;
                viewModel.AddMachineToCanvas(machine, 400, 250);
            }
        }
        private void MachineList_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _draggedElement is ListBoxItem item)
            {
                Point currentPosition = e.GetPosition(null);
                Vector diff = _dragStartPoint - currentPosition;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    // Start drag-drop operation
                    if (item.DataContext != null)
                    {
                        Console.WriteLine($"Dragging type: {item.DataContext.GetType().FullName}");

                        DragDrop.DoDragDrop(item, item.DataContext, DragDropEffects.Copy);
                    }
                    _draggedElement = null;
                }
            }
        }

        /// <summary>
        /// Allow drop on canvas
        /// </summary>
        private void LayoutCanvas_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ManufacturingSimulation.Database.Models.Machine)))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        /// <summary>
        /// Handle drop operation - add machine to canvas
        /// </summary>
        private void LayoutCanvas_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ManufacturingSimulation.Database.Models.Machine)))
            {
                var machine = e.Data.GetData(typeof(ManufacturingSimulation.Database.Models.Machine)) as ManufacturingSimulation.Database.Models.Machine;
                Point dropPosition = e.GetPosition(LayoutCanvas);

                if (DataContext is FactoryLayoutViewModel viewModel && machine != null)
                {
                    viewModel.AddMachineToCanvas(machine, dropPosition.X, dropPosition.Y);
                }
            }
            e.Handled = true;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Enable drag-drop on newly added machine elements
        /// </summary>
        private void OnMachineAddedToCanvas(object sender, FrameworkElement machineElement)
        {
            // Machine elements are already in canvas and handled by canvas events
            // This is just for any additional setup if needed
            if (machineElement != null)
            {
                machineElement.Cursor = Cursors.Hand;
            }
        }

        /// <summary>
        /// Handle grid toggle for visual guides
        /// </summary>
        private void ShowGridCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            // Grid visibility is bound to ViewModel, this is just for any additional logic
        }

        #endregion

        #region Context Menu Handlers

        /// <summary>
        /// Remove machine from canvas via context menu
        /// </summary>
        private void RemoveMachine_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem &&
                menuItem.DataContext is MachineLocationModel machine &&
                DataContext is FactoryLayoutViewModel viewModel)
            {
                viewModel.RemoveMachineFromCanvas(machine);
            }
        }

        /// <summary>
        /// Rotate machine 90 degrees
        /// </summary>
        private void RotateMachine_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem &&
                menuItem.DataContext is MachineLocationModel machine &&
                DataContext is FactoryLayoutViewModel viewModel)
            {
                viewModel.RotateMachine(machine);
            }
        }

        /// <summary>
        /// Edit machine properties
        /// </summary>
        private void EditMachine_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem &&
                menuItem.DataContext is MachineLocationModel machine &&
                DataContext is FactoryLayoutViewModel viewModel)
            {
                viewModel.EditMachineProperties(machine);
            }
        }

        #endregion
    }
}
