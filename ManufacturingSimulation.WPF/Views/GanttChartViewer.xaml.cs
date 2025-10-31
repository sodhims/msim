using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace ManufacturingSimulation.WPF.Views
{
    public partial class GanttChartViewer : UserControl
    {
        private double _zoomLevel = 1.0;
        private Dictionary<string, Rectangle> _taskBars = new Dictionary<string, Rectangle>();

        public GanttChartViewer()
        {
            InitializeComponent();
            InitializeChart();
        }

        private void InitializeChart()
        {
            // Initialize the Gantt chart canvas
            DrawGridLines();
        }

        private void DrawGridLines()
        {
            // TODO: Implement grid line drawing
            // This would draw vertical lines for time intervals
            // and horizontal lines for resource rows
        }

        #region Public Methods

        public void Clear()
        {
            tasksCanvas.Children.Clear();
            gridLinesCanvas.Children.Clear();
            resourceListPanel.Children.Clear();
            timelineCanvas.Children.Clear();
            _taskBars.Clear();

            txtStatus.Text = "Chart cleared";
        }

        public void Refresh()
        {
            // Redraw everything
            DrawGridLines();
            txtStatus.Text = "Chart refreshed";
        }

        public void AddTask(string taskId, string resourceId, TimeSpan startTime, TimeSpan? endTime)
        {
            try
            {
                // TODO: Implement task bar creation and positioning
                // Create a Rectangle for the task
                // Position it based on startTime and resourceId
                // Add to tasksCanvas

                txtStatus.Text = $"Task {taskId} added";
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"Error adding task: {ex.Message}";
            }
        }

        public void UpdateTaskEnd(string taskId, TimeSpan endTime)
        {
            try
            {
                if (_taskBars.ContainsKey(taskId))
                {
                    // TODO: Update the width of the task bar
                    txtStatus.Text = $"Task {taskId} updated";
                }
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"Error updating task: {ex.Message}";
            }
        }

        #endregion

        #region Event Handlers

        private void BtnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            _zoomLevel *= 1.2;
            UpdateZoomLevel();
        }

        private void BtnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            _zoomLevel /= 1.2;
            UpdateZoomLevel();
        }

        private void BtnResetZoom_Click(object sender, RoutedEventArgs e)
        {
            _zoomLevel = 1.0;
            UpdateZoomLevel();
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            ExportChart();
        }

        private void ChartScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Synchronize scrolling between timeline and chart
            if (timelineScrollViewer != null)
            {
                timelineScrollViewer.ScrollToHorizontalOffset(e.HorizontalOffset);
            }

            if (resourceScrollViewer != null)
            {
                resourceScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
            }
        }

        #endregion

        #region Private Methods

        private void UpdateZoomLevel()
        {
            // TODO: Apply zoom transformation to canvas
            txtZoomLevel.Text = $"Zoom: {_zoomLevel * 100:F0}%";
            Refresh();
        }

        private void ExportChart()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg",
                FileName = $"gantt_chart_{DateTime.Now:yyyyMMdd_HHmmss}.png"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // TODO: Implement chart export to image
                    MessageBox.Show("Export functionality not yet implemented.",
                        "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting chart: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion
    }
}
