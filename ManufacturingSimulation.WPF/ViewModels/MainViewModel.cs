using ManufacturingSimulation.Core.Models;
using ManufacturingSimulation.Core.SimulationEngine;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace ManufacturingSimulation.WPF.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly SimulationEngine _engine;
        private readonly DispatcherTimer _timer;
        private bool _isRunning;
        private double _currentTime;
        private int _simulationSpeed;

        public MainViewModel()
        {
            _engine = new SimulationEngine(randomSeed: 42);
            _simulationSpeed = 100; // milliseconds per step
            
            // Setup machines
            _engine.AddMachine(new Machine(1, "Drill Press"), bufferCapacity: 5);
            _engine.AddMachine(new Machine(2, "Lathe"), bufferCapacity: 5);
            _engine.AddMachine(new Machine(3, "Mill"), bufferCapacity: 5);
            _engine.AddMachine(new Machine(4, "Grinder"), bufferCapacity: 5);

            // Create ViewModels
            Machines = new ObservableCollection<MachineViewModel>();
            foreach (var machine in _engine.Machines)
            {
                Machines.Add(new MachineViewModel(machine));
            }

            // Commands
            StartCommand = new RelayCommand(_ => Start(), _ => !IsRunning);
            PauseCommand = new RelayCommand(_ => Pause(), _ => IsRunning);
            ResetCommand = new RelayCommand(_ => Reset());

            // Timer for UI updates
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _timer.Tick += Timer_Tick;

            // Subscribe to engine events
            _engine.EventProcessed += OnEventProcessed;

            // Initialize simulation with parts
            InitializeSimulation();
        }

        public ObservableCollection<MachineViewModel> Machines { get; }

        public bool IsRunning
        {
            get => _isRunning;
            set => SetProperty(ref _isRunning, value);
        }

        public double CurrentTime
        {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }

        public ICommand StartCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand ResetCommand { get; }

        private void InitializeSimulation()
        {
            // Create parts with different routes
            var parts = new List<Part>
            {
                new Part("P001", new List<int> { 1, 2, 3 }),
                new Part("P002", new List<int> { 2, 4, 1 }),
                new Part("P003", new List<int> { 1, 3, 2, 4 }),
                new Part("P004", new List<int> { 4, 3, 1 }),
                new Part("P005", new List<int> { 2, 1, 4 }),
                new Part("P006", new List<int> { 1, 2, 3, 4 }),
                new Part("P007", new List<int> { 3, 1, 4 }),
                new Part("P008", new List<int> { 4, 2, 3 })
            };

            // Schedule arrivals
            for (int i = 0; i < parts.Count; i++)
            {
                _engine.SchedulePartArrival(parts[i], arrivalTime: i * 2.0);
            }
        }

        private async void Start()
        {
            IsRunning = true;
            _timer.Start();

            await Task.Run(() =>
            {
                _engine.RunUntil(100.0);
            });

            _timer.Stop();
            IsRunning = false;
        }

        private void Pause()
        {
            IsRunning = false;
            _timer.Stop();
        }

        private void Reset()
        {
            _timer.Stop();
            _engine.Reset();
            CurrentTime = 0;
            IsRunning = false;
            
            InitializeSimulation();
            UpdateUI();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            UpdateUI();
        }

        private void OnEventProcessed(object? sender, SimulationEvent evt)
        {
            // Update UI on dispatcher thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateUI();
            });

            // Slow down for visualization
            Thread.Sleep(_simulationSpeed);
        }

        private void UpdateUI()
        {
            CurrentTime = _engine.CurrentTime;

            for (int i = 0; i < _engine.Machines.Count; i++)
            {
                var machine = _engine.Machines[i];
                var buffer = _engine.Buffers[machine.Id];
                Machines[i].UpdateFromModel(buffer, _engine.CurrentTime);
            }
        }
    }
}