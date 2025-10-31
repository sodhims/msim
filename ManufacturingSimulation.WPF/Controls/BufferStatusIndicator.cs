using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ManufacturingSimulation
{
    /// <summary>
    /// Visual indicator for buffer status with red/yellow/green color coding
    /// </summary>
    public class BufferStatusIndicator : UserControl
    {
        private readonly Ellipse _indicator;
        private readonly TextBlock _countText;
        private readonly TextBlock _labelText;

        private int _currentCount;
        private int _capacity;
        private double _redThreshold = 0.9;    // 90% = red
        private double _yellowThreshold = 0.7; // 70% = yellow

        public static readonly DependencyProperty CurrentCountProperty =
            DependencyProperty.Register(nameof(CurrentCount), typeof(int), typeof(BufferStatusIndicator),
                new PropertyMetadata(0, OnCountChanged));

        public static readonly DependencyProperty CapacityProperty =
            DependencyProperty.Register(nameof(Capacity), typeof(int), typeof(BufferStatusIndicator),
                new PropertyMetadata(10, OnCountChanged));

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(BufferStatusIndicator),
                new PropertyMetadata("Buffer", OnLabelChanged));

        public int CurrentCount
        {
            get => (int)GetValue(CurrentCountProperty);
            set => SetValue(CurrentCountProperty, value);
        }

        public int Capacity
        {
            get => (int)GetValue(CapacityProperty);
            set => SetValue(CapacityProperty, value);
        }

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public BufferStatusIndicator()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Create indicator ellipse
            _indicator = new Ellipse
            {
                Width = 20,
                Height = 20,
                Margin = new Thickness(5),
                StrokeThickness = 2,
                Stroke = Brushes.Gray
            };
            Grid.SetColumn(_indicator, 0);
            grid.Children.Add(_indicator);

            // Create text stack panel
            var textStack = new StackPanel
            {
                Margin = new Thickness(5, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            _labelText = new TextBlock
            {
                FontWeight = FontWeights.Bold,
                FontSize = 12
            };
            textStack.Children.Add(_labelText);

            _countText = new TextBlock
            {
                FontSize = 11,
                Foreground = Brushes.Gray
            };
            textStack.Children.Add(_countText);

            Grid.SetColumn(textStack, 1);
            grid.Children.Add(textStack);

            Content = grid;
            
            UpdateIndicator();
        }

        private static void OnCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BufferStatusIndicator indicator)
            {
                indicator.UpdateIndicator();
            }
        }

        private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BufferStatusIndicator indicator)
            {
                indicator._labelText.Text = indicator.Label;
            }
        }

        private void UpdateIndicator()
        {
            if (_indicator == null || _countText == null) return;

            double fillRatio = Capacity > 0 ? (double)CurrentCount / Capacity : 0;

            // Determine color based on fill ratio
            Brush fillBrush;
            string status;

            if (fillRatio >= _redThreshold)
            {
                fillBrush = new SolidColorBrush(Color.FromRgb(220, 53, 69)); // Red
                status = "Critical";
            }
            else if (fillRatio >= _yellowThreshold)
            {
                fillBrush = new SolidColorBrush(Color.FromRgb(255, 193, 7)); // Yellow
                status = "Warning";
            }
            else
            {
                fillBrush = new SolidColorBrush(Color.FromRgb(40, 167, 69)); // Green
                status = "Normal";
            }

            _indicator.Fill = fillBrush;
            _countText.Text = $"{CurrentCount}/{Capacity} ({status})";
            _labelText.Text = Label;

            // Add tooltip
            ToolTip = $"{Label}: {CurrentCount}/{Capacity} parts\n" +
                      $"Fill: {fillRatio:P0}\n" +
                      $"Status: {status}";
        }

        /// <summary>
        /// Set custom thresholds for yellow and red indicators
        /// </summary>
        public void SetThresholds(double yellowThreshold, double redThreshold)
        {
            if (yellowThreshold < 0 || yellowThreshold > 1 || 
                redThreshold < 0 || redThreshold > 1 ||
                yellowThreshold >= redThreshold)
            {
                throw new ArgumentException("Thresholds must be between 0 and 1, with yellow < red");
            }

            _yellowThreshold = yellowThreshold;
            _redThreshold = redThreshold;
            UpdateIndicator();
        }
    }
}
