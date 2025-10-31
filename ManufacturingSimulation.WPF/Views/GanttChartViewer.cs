using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ManufacturingSimulation
{
    /// <summary>
    /// Interactive Gantt chart viewer window with filtering and part tracing
    /// </summary>
    public partial class GanttChartViewer : Window
    {
        private GanttChartData _data;
        private Canvas _ganttCanvas;
        private ScrollViewer _scrollViewer;
        private double _timeScale = 50.0; // pixels per time unit
        private double _rowHeight = 40.0;
        private double _chartLeftMargin = 100.0;
        private double _chartTopMargin = 50.0;
        private int? _selectedPartId = null;
        private List<int> _filteredPartIds = new List<int>();

        // UI Controls
        private ComboBox _partFilterCombo;
        private TextBlock _statsText;
        private Slider _zoomSlider;
        private CheckBox _showLabelsCheck;
        private TextBlock _hoverInfoText;

        public GanttChartViewer()
        {
            InitializeComponent();
            SetupUI();
        }

        public GanttChartViewer(GanttChartData data) : this()
        {
            LoadData(data);
        }

        private void InitializeComponent()
        {
            Title = "Gantt Chart Viewer";
            Width = 1200;
            Height = 700;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        private void SetupUI()
        {
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Toolbar
            var toolbar = CreateToolbar();
            Grid.SetRow(toolbar, 0);
            mainGrid.Children.Add(toolbar);

            // Statistics panel
            var statsPanel = CreateStatsPanel();
            Grid.SetRow(statsPanel, 1);
            mainGrid.Children.Add(statsPanel);

            // Scrollable chart area
            _scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = Brushes.White
            };

            _ganttCanvas = new Canvas
            {
                Background = Brushes.White
            };
            _scrollViewer.Content = _ganttCanvas;

            Grid.SetRow(_scrollViewer, 2);
            mainGrid.Children.Add(_scrollViewer);

            // Status bar
            var statusBar = CreateStatusBar();
            Grid.SetRow(statusBar, 3);
            mainGrid.Children.Add(statusBar);

            Content = mainGrid;

            // Event handlers
            _ganttCanvas.MouseMove += GanttCanvas_MouseMove;
            _ganttCanvas.MouseLeave += GanttCanvas_MouseLeave;
        }

        private StackPanel CreateToolbar()
        {
            var toolbar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                Height = 50
            };

            // Load button
            var loadBtn = new Button
            {
                Content = "Load CSV...",
                Margin = new Thickness(5),
                Padding = new Thickness(10, 5, 10, 5),
                Width = 100
            };
            loadBtn.Click += LoadButton_Click;
            toolbar.Children.Add(loadBtn);

            // Separator
            toolbar.Children.Add(new Separator { Width = 20, Background = Brushes.Transparent });

            // Part filter
            toolbar.Children.Add(new TextBlock
            {
                Text = "Filter Part:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5)
            });

            _partFilterCombo = new ComboBox
            {
                Width = 120,
                Margin = new Thickness(5)
            };
            _partFilterCombo.SelectionChanged += PartFilter_SelectionChanged;
            toolbar.Children.Add(_partFilterCombo);

            var clearFilterBtn = new Button
            {
                Content = "Clear Filter",
                Margin = new Thickness(5),
                Padding = new Thickness(10, 5, 10, 5)
            };
            clearFilterBtn.Click += (s, e) => ClearPartFilter();
            toolbar.Children.Add(clearFilterBtn);

            // Separator
            toolbar.Children.Add(new Separator { Width = 20, Background = Brushes.Transparent });

            // Zoom control
            toolbar.Children.Add(new TextBlock
            {
                Text = "Zoom:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5)
            });

            _zoomSlider = new Slider
            {
                Minimum = 10,
                Maximum = 200,
                Value = 50,
                Width = 150,
                Margin = new Thickness(5),
                VerticalAlignment = VerticalAlignment.Center
            };
            _zoomSlider.ValueChanged += ZoomSlider_ValueChanged;
            toolbar.Children.Add(_zoomSlider);

            // Show labels checkbox
            _showLabelsCheck = new CheckBox
            {
                Content = "Show Labels",
                IsChecked = true,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 5, 5, 5)
            };
            _showLabelsCheck.Checked += (s, e) => RedrawChart();
            _showLabelsCheck.Unchecked += (s, e) => RedrawChart();
            toolbar.Children.Add(_showLabelsCheck);

            return toolbar;
        }

        private Border CreateStatsPanel()
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(250, 250, 250)),
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0, 1, 0, 1),
                Padding = new Thickness(10)
            };

            _statsText = new TextBlock
            {
                Text = "Load a CSV file to view Gantt chart",
                FontSize = 12
            };

            border.Child = _statsText;
            return border;
        }

        private Border CreateStatusBar()
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(10, 5, 10, 5)
            };

            _hoverInfoText = new TextBlock
            {
                Text = "Hover over tasks for details",
                FontSize = 11
            };

            border.Child = _hoverInfoText;
            return border;
        }

        public void LoadData(GanttChartData data)
        {
            _data = data;
            
            // Update part filter combo
            _partFilterCombo.Items.Clear();
            _partFilterCombo.Items.Add("All Parts");
            foreach (var partId in data.PartIds.OrderBy(id => id))
            {
                _partFilterCombo.Items.Add($"Part {partId}");
            }
            _partFilterCombo.SelectedIndex = 0;

            // Update statistics
            UpdateStatistics();

            // Draw chart
            RedrawChart();
        }

        private void UpdateStatistics()
        {
            if (_data == null) return;

            var stats = _data.GetStatistics();
            _statsText.Text = $"Parts: {stats.TotalParts} | " +
                            $"Machines: {stats.TotalMachines} | " +
                            $"Tasks: {stats.TotalTasks} | " +
                            $"Duration: {stats.SimulationDuration:F2} | " +
                            $"Avg Task Duration: {stats.AverageTaskDuration:F2} | " +
                            $"Avg Flow Time: {stats.AverageFlowTime:F2}";
        }

        private void RedrawChart()
        {
            if (_data == null) return;

            _ganttCanvas.Children.Clear();

            var tasksToShow = _selectedPartId.HasValue
                ? _data.GetTasksForPart(_selectedPartId.Value)
                : _data.Tasks;

            if (tasksToShow.Count == 0)
            {
                DrawNoDataMessage();
                return;
            }

            DrawTimeAxis();
            DrawMachineLabels();
            DrawGanttBars(tasksToShow);
            DrawGridLines();

            // Update canvas size
            double canvasWidth = _chartLeftMargin + (_data.MaxTime - _data.MinTime) * _timeScale + 50;
            double canvasHeight = _chartTopMargin + _data.MachineIds.Count * _rowHeight + 50;
            _ganttCanvas.Width = canvasWidth;
            _ganttCanvas.Height = canvasHeight;
        }

        private void DrawNoDataMessage()
        {
            var text = new TextBlock
            {
                Text = "No data to display for selected filter",
                FontSize = 16,
                Foreground = Brushes.Gray
            };
            Canvas.SetLeft(text, _chartLeftMargin + 50);
            Canvas.SetTop(text, _chartTopMargin + 50);
            _ganttCanvas.Children.Add(text);
        }

        private void DrawTimeAxis()
        {
            double timeRange = _data.MaxTime - _data.MinTime;
            int numTicks = Math.Min(20, (int)(timeRange / 10) + 1);
            double tickInterval = timeRange / numTicks;

            for (int i = 0; i <= numTicks; i++)
            {
                double time = _data.MinTime + i * tickInterval;
                double x = _chartLeftMargin + (time - _data.MinTime) * _timeScale;

                // Tick mark
                var tick = new Line
                {
                    X1 = x,
                    Y1 = _chartTopMargin - 5,
                    X2 = x,
                    Y2 = _chartTopMargin,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                _ganttCanvas.Children.Add(tick);

                // Label
                var label = new TextBlock
                {
                    Text = time.ToString("F1"),
                    FontSize = 10
                };
                Canvas.SetLeft(label, x - 15);
                Canvas.SetTop(label, _chartTopMargin - 25);
                _ganttCanvas.Children.Add(label);
            }

            // Axis line
            var axisLine = new Line
            {
                X1 = _chartLeftMargin,
                Y1 = _chartTopMargin,
                X2 = _chartLeftMargin + timeRange * _timeScale,
                Y2 = _chartTopMargin,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            _ganttCanvas.Children.Add(axisLine);
        }

        private void DrawMachineLabels()
        {
            for (int i = 0; i < _data.MachineIds.Count; i++)
            {
                int machineId = _data.MachineIds[i];
                double y = _chartTopMargin + i * _rowHeight + _rowHeight / 2;

                var label = new TextBlock
                {
                    Text = $"Machine {machineId}",
                    FontWeight = FontWeights.Bold,
                    FontSize = 12
                };
                Canvas.SetLeft(label, 10);
                Canvas.SetTop(label, y - 10);
                _ganttCanvas.Children.Add(label);

                // Calculate and show utilization
                var util = _data.CalculateMachineUtilization();
                if (util.ContainsKey(machineId))
                {
                    var utilLabel = new TextBlock
                    {
                        Text = $"({util[machineId]:P0})",
                        FontSize = 10,
                        Foreground = Brushes.Gray
                    };
                    Canvas.SetLeft(utilLabel, 10);
                    Canvas.SetTop(utilLabel, y + 5);
                    _ganttCanvas.Children.Add(utilLabel);
                }
            }
        }

        private void DrawGridLines()
        {
            double timeRange = _data.MaxTime - _data.MinTime;
            double chartHeight = _data.MachineIds.Count * _rowHeight;

            // Horizontal lines between machines
            for (int i = 0; i <= _data.MachineIds.Count; i++)
            {
                double y = _chartTopMargin + i * _rowHeight;
                var line = new Line
                {
                    X1 = _chartLeftMargin,
                    Y1 = y,
                    X2 = _chartLeftMargin + timeRange * _timeScale,
                    Y2 = y,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 2, 2 }
                };
                _ganttCanvas.Children.Add(line);
            }
        }

        private void DrawGanttBars(List<GanttTask> tasks)
        {
            var colorMap = GenerateColorMap(_data.PartIds);

            foreach (var task in tasks)
            {
                int machineIndex = _data.MachineIds.IndexOf(task.MachineId);
                if (machineIndex < 0) continue;

                double x = _chartLeftMargin + (task.StartTime - _data.MinTime) * _timeScale;
                double y = _chartTopMargin + machineIndex * _rowHeight + 5;
                double width = task.Duration * _timeScale;
                double height = _rowHeight - 10;

                // Create rectangle
                var rect = new Rectangle
                {
                    Width = Math.Max(width, 2), // Minimum width for visibility
                    Height = height,
                    Fill = colorMap[task.PartId],
                    Stroke = _selectedPartId == task.PartId ? Brushes.Red : Brushes.Black,
                    StrokeThickness = _selectedPartId == task.PartId ? 2 : 1,
                    Tag = task
                };

                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, y);
                _ganttCanvas.Children.Add(rect);

                // Add label if enabled and bar is wide enough
                if (_showLabelsCheck.IsChecked == true && width > 30)
                {
                    var label = new TextBlock
                    {
                        Text = $"P{task.PartId}",
                        FontSize = 9,
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.Bold
                    };
                    Canvas.SetLeft(label, x + 3);
                    Canvas.SetTop(label, y + height / 2 - 7);
                    _ganttCanvas.Children.Add(label);
                }

                // Add tooltip
                rect.ToolTip = $"Part {task.PartId}\n" +
                              $"Machine {task.MachineId}\n" +
                              $"Start: {task.StartTime:F2}\n" +
                              $"End: {task.EndTime:F2}\n" +
                              $"Duration: {task.Duration:F2}";

                // Click handler for highlighting
                rect.MouseLeftButtonDown += (s, e) => HighlightPart(task.PartId);
            }
        }

        private Dictionary<int, Brush> GenerateColorMap(List<int> partIds)
        {
            var colorMap = new Dictionary<int, Brush>();
            var random = new Random(42); // Fixed seed for consistency

            foreach (var partId in partIds)
            {
                byte r = (byte)random.Next(100, 230);
                byte g = (byte)random.Next(100, 230);
                byte b = (byte)random.Next(100, 230);
                colorMap[partId] = new SolidColorBrush(Color.FromRgb(r, g, b));
            }

            return colorMap;
        }

        private void HighlightPart(int partId)
        {
            _selectedPartId = partId;
            _partFilterCombo.SelectedItem = $"Part {partId}";
        }

        private void ClearPartFilter()
        {
            _selectedPartId = null;
            _partFilterCombo.SelectedIndex = 0;
            RedrawChart();
        }

        private void GanttCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            var element = e.OriginalSource as Rectangle;
            if (element?.Tag is GanttTask task)
            {
                _hoverInfoText.Text = $"Part {task.PartId} on Machine {task.MachineId}: " +
                                     $"{task.StartTime:F2} → {task.EndTime:F2} (Duration: {task.Duration:F2})";
            }
        }

        private void GanttCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            _hoverInfoText.Text = "Hover over tasks for details";
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Select Parts Log CSV"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var data = GanttChartData.LoadFromCsvLogs(dialog.FileName);
                    LoadData(data);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading CSV: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void PartFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_partFilterCombo.SelectedIndex == 0)
            {
                _selectedPartId = null;
            }
            else if (_partFilterCombo.SelectedItem is string selected && selected.StartsWith("Part "))
            {
                if (int.TryParse(selected.Substring(5), out int partId))
                {
                    _selectedPartId = partId;
                }
            }
            RedrawChart();
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _timeScale = e.NewValue;
            RedrawChart();
        }
    }
}
