using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ManufacturingSimulation.Bridge;
using ManufacturingSimulation.Database;

namespace ManufacturingSimulation.WPF.ViewModels
{
    public class GanttChartViewModel : INotifyPropertyChanged
    {
        private readonly int _runId;
        private readonly Canvas _canvas;
        private readonly SimulationService _simulationService;

        // Constants for drawing
        private const double ROW_HEIGHT = 50;
        private const double MACHINE_LABEL_WIDTH = 150;
        private const double TIME_LABEL_HEIGHT = 30;
        private const double MARGIN_TOP = 50;
        private const double MARGIN_LEFT = 10;

        // Events for updating UI
        public event Action<string> SimulationDateChanged;
        public event Action<string> MakespanChanged;
        public event Action<int> TaskCountChanged;
        public event Action<int> MachineCountChanged;
        public event Action<string> HoverInfoChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public GanttChartViewModel(int runId, Canvas canvas)
        {
            _runId = runId;
            _canvas = canvas;

            // Create MesDbContext and SimulationService
            var dbContext = new MesDbContext();
            _simulationService = new SimulationService(dbContext);
        }

        public void LoadGanttData()
        {
            // Get data from database
            var ganttData = _simulationService.GetGanttChartData(_runId);

            if (ganttData == null || ganttData.Tasks == null || !ganttData.Tasks.Any())
            {
                MessageBox.Show(
                    $"No task data found for run ID {_runId}.\n\nPlease ensure the simulation completed successfully and events were logged.",
                    "No Data",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            // Update header information
            SimulationDateChanged?.Invoke(ganttData.SimulationDate.ToString("yyyy-MM-dd HH:mm"));
            MakespanChanged?.Invoke($"{ganttData.Makespan:F2} min");
            TaskCountChanged?.Invoke(ganttData.Tasks.Count);
            MachineCountChanged?.Invoke(ganttData.UniqueMachines?.Count ?? 0);

            // Draw the chart
            DrawGanttChart(ganttData);
        }

        //private void DrawGanttChart(GanttViewData ganttData)
        //{
        //    _canvas.Children.Clear();

        //    if (ganttData.UniqueMachines == null || !ganttData.UniqueMachines.Any())
        //    {
        //        return;
        //    }

        //    // Calculate canvas size
        //    double canvasWidth = Math.Max(1000, ganttData.Makespan * 2); // 2 pixels per minute minimum
        //    double canvasHeight = (ganttData.UniqueMachines.Count * ROW_HEIGHT) + MARGIN_TOP + TIME_LABEL_HEIGHT + 50;

        //    _canvas.Width = canvasWidth + MACHINE_LABEL_WIDTH + 50;
        //    _canvas.Height = canvasHeight;

        //    // Calculate time scale
        //    double pixelsPerMinute = (canvasWidth - MACHINE_LABEL_WIDTH - 50) / ganttData.Makespan;

        //    // Draw grid and timeline
        //    DrawTimeline(ganttData.Makespan, pixelsPerMinute, canvasWidth);
        //    DrawMachineRows(ganttData.UniqueMachines, canvasHeight);

        //    // Draw tasks
        //    foreach (var task in ganttData.Tasks)
        //    {
        //        DrawTask(task, ganttData.UniqueMachines, pixelsPerMinute);
        //    }
        //}

        private void DrawTimeline(double makespan, double pixelsPerMinute, double canvasWidth)
        {
            double xStart = MACHINE_LABEL_WIDTH + MARGIN_LEFT;
            double y = MARGIN_TOP - 10;

            // Timeline axis
            var timelineAxis = new Line
            {
                X1 = xStart,
                Y1 = y,
                X2 = canvasWidth,
                Y2 = y,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            _canvas.Children.Add(timelineAxis);

            // Time markers - adjust interval based on makespan
            double interval = CalculateTimeInterval(makespan);

            for (double time = 0; time <= makespan; time += interval)
            {
                double x = xStart + (time * pixelsPerMinute);

                // Tick mark
                var tick = new Line
                {
                    X1 = x,
                    Y1 = y,
                    X2 = x,
                    Y2 = y + 5,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                _canvas.Children.Add(tick);

                // Time label
                var timeLabel = new TextBlock
                {
                    Text = $"{time:F0}",
                    FontSize = 10,
                    Foreground = Brushes.Black
                };
                Canvas.SetLeft(timeLabel, x - 10);
                Canvas.SetTop(timeLabel, y - 20);
                _canvas.Children.Add(timeLabel);

                // Vertical grid line
                var gridLine = new Line
                {
                    X1 = x,
                    Y1 = y + 10,
                    X2 = x,
                    Y2 = _canvas.Height - 20,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 2, 2 }
                };
                _canvas.Children.Add(gridLine);
            }

            // Timeline label
            var timelineLabel = new TextBlock
            {
                Text = "Time (minutes)",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black
            };
            Canvas.SetLeft(timelineLabel, xStart + ((canvasWidth - xStart) / 2) - 40);
            Canvas.SetTop(timelineLabel, y - 35);
            _canvas.Children.Add(timelineLabel);
        }

        private double CalculateTimeInterval(double makespan)
        {
            if (makespan <= 60) return 10;
            if (makespan <= 180) return 30;
            if (makespan <= 360) return 60;
            if (makespan <= 720) return 120;
            return 180;
        }

        private void DrawMachineRows(List<string> machines, double canvasHeight)
        {
            for (int i = 0; i < machines.Count; i++)
            {
                double y = MARGIN_TOP + (i * ROW_HEIGHT);

                // Row background (alternating colors)
                var rowBackground = new Rectangle
                {
                    Width = _canvas.Width,
                    Height = ROW_HEIGHT,
                    Fill = i % 2 == 0 ? Brushes.White : new SolidColorBrush(Color.FromRgb(245, 245, 245))
                };
                Canvas.SetLeft(rowBackground, 0);
                Canvas.SetTop(rowBackground, y);
                _canvas.Children.Add(rowBackground);

                // Machine label
                var machineLabel = new TextBlock
                {
                    Text = machines[i],
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80)),
                    Width = MACHINE_LABEL_WIDTH - 10,
                    TextAlignment = TextAlignment.Right
                };
                Canvas.SetLeft(machineLabel, MARGIN_LEFT);
                Canvas.SetTop(machineLabel, y + (ROW_HEIGHT / 2) - 8);
                _canvas.Children.Add(machineLabel);

                // Separator line
                var separator = new Line
                {
                    X1 = 0,
                    Y1 = y + ROW_HEIGHT,
                    X2 = _canvas.Width,
                    Y2 = y + ROW_HEIGHT,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 1
                };
                _canvas.Children.Add(separator);
            }
        }

        private void DrawTask(GanttViewTask task, List<string> machines, double pixelsPerMinute, Dictionary<string, Brush> colorMap)
        {
            int machineIndex = machines.IndexOf(task.MachineName);
            if (machineIndex == -1) return;

            double y = MARGIN_TOP + (machineIndex * ROW_HEIGHT) + 5;
            double x = MACHINE_LABEL_WIDTH + MARGIN_LEFT + (task.StartTime * pixelsPerMinute);
            double width = (task.EndTime - task.StartTime) * pixelsPerMinute;
            double height = ROW_HEIGHT - 10;

            if (width < 2) width = 2;

            // Get color based on order number prefix (WO-YYYYMMDD-XXXX)
            var partPrefix = string.Join("-", task.PartId.Split('-').Take(3)); // "WO-20251101-3917"
            var fillColor = colorMap.ContainsKey(partPrefix)
                ? colorMap[partPrefix]
                : Brushes.Gray;

            // Different opacity for Setup vs Processing
            var actualFill = task.TaskType == "Setup"
                ? new SolidColorBrush(((SolidColorBrush)fillColor).Color) { Opacity = 0.5 }
                : fillColor;

            var taskRect = new Rectangle
            {
                Width = width,
                Height = height,
                Fill = actualFill,
                Stroke = Brushes.DarkGray,
                StrokeThickness = 1,
                RadiusX = 3,
                RadiusY = 3
            };

            Canvas.SetLeft(taskRect, x);
            Canvas.SetTop(taskRect, y);
            _canvas.Children.Add(taskRect);

            if (width > 40)
            {
                var taskLabel = new TextBlock
                {
                    Text = task.PartId,
                    FontSize = 9,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold,
                    Width = width - 4,
                    TextAlignment = TextAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                Canvas.SetLeft(taskLabel, x + 2);
                Canvas.SetTop(taskLabel, y + (height / 2) - 6);
                _canvas.Children.Add(taskLabel);
            }

            string tooltip = $"Part: {task.PartId}\n" +
                           $"Type: {task.TaskType}\n" +
                           $"Machine: {task.MachineName}\n" +
                           $"Start: {task.StartTime:F2} min\n" +
                           $"Duration: {(task.EndTime - task.StartTime):F2} min\n" +
                           $"End: {task.EndTime:F2} min";

            taskRect.ToolTip = tooltip;
            taskRect.MouseEnter += (s, e) => HoverInfoChanged?.Invoke(
                $"Part: {task.PartId} ({task.TaskType}) on {task.MachineName} ({task.StartTime:F1}-{task.EndTime:F1} min)"
            );
            taskRect.MouseLeave += (s, e) => HoverInfoChanged?.Invoke("Hover over tasks for details");
        }

        private void DrawGanttChart(GanttViewData ganttData)
        {
            _canvas.Children.Clear();

            if (ganttData.UniqueMachines == null || !ganttData.UniqueMachines.Any())
                return;

            double canvasWidth = Math.Max(1000, ganttData.Makespan * 2);
            double canvasHeight = (ganttData.UniqueMachines.Count * ROW_HEIGHT) + MARGIN_TOP + TIME_LABEL_HEIGHT + 50;

            _canvas.Width = canvasWidth + MACHINE_LABEL_WIDTH + 50;
            _canvas.Height = canvasHeight;
            double pixelsPerMinute = (canvasWidth - MACHINE_LABEL_WIDTH - 50) / ganttData.Makespan;

            DrawTimeline(ganttData.Makespan, pixelsPerMinute, canvasWidth);
            DrawMachineRows(ganttData.UniqueMachines, canvasHeight);

            // Create color map for unique orders
            var orderPrefixes = ganttData.Tasks
                .Select(t => string.Join("-", t.PartId.Split('-').Take(3))) // "WO-20251101-3917"
                .Distinct()
                .ToList();

            var colorMap = new Dictionary<string, Brush>();
            var colors = new[] {
                Brushes.SteelBlue, Brushes.Coral, Brushes.MediumSeaGreen,
                Brushes.Orange, Brushes.Purple, Brushes.Teal, Brushes.Crimson
                };

            for (int i = 0; i < orderPrefixes.Count; i++)
                colorMap[orderPrefixes[i]] = colors[i % colors.Length];

            foreach (var task in ganttData.Tasks)
            {
                DrawTask(task, ganttData.UniqueMachines, pixelsPerMinute, colorMap);
            }
        }
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
