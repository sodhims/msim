using ManufacturingSimulation.Core;
using ManufacturingSimulation.Core.Models;
using ManufacturingSimulation.Core.Engine;
using ManufacturingSimulation.Core.Engine.Rules;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;

namespace ManufacturingSimulation.WPF.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly SimulationEngine _engine;
        private readonly DispatcherTimer _timer;
        private readonly StatisticsViewModel _statistics;
        private bool _isRunning;
        private double _currentTime;
        private int _simulationSpeed;

        public MainViewModel()
        {
            _engine = new SimulationEngine(randomSeed: 42);
            _simulationSpeed = 100;
            _statistics = new StatisticsViewModel();

            _engine.AddMachine(new Machine(1, "Drill Press"), bufferCapacity: 10);
            _engine.AddMachine(new Machine(2, "Lathe"), bufferCapacity: 10);
            _engine.AddMachine(new Machine(3, "Mill"), bufferCapacity: 10);
            _engine.AddMachine(new Machine(4, "Grinder"), bufferCapacity: 10);

            Machines = new ObservableCollection<MachineViewModel>();
            foreach (var machine in _engine.Machines)
            {
                Machines.Add(new MachineViewModel(machine));
            }

            StartCommand = new RelayCommand(_ => Start(), _ => !IsRunning);
            PauseCommand = new RelayCommand(_ => Pause(), _ => IsRunning);
            ResetCommand = new RelayCommand(_ => Reset());

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _timer.Tick += Timer_Tick;
            _engine.EventProcessed += OnEventProcessed;
            InitializeSimulation();
        }

        public ObservableCollection<MachineViewModel> Machines { get; }
        public StatisticsViewModel Statistics => _statistics;

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
            var parts = new List<Part>();
            var standardRoute = new List<int> { 1, 2, 3, 4 };

            for (int i = 0; i < 20; i++)
            {
                double arrivalTime = i * 2.0;
                int priority = i + 1;
                double dueDate = arrivalTime + 50;
                parts.Add(new Part($"P{i + 1:000}", new List<int>(standardRoute), arrivalTime, priority, dueDate));
            }

            foreach (var part in parts)
            {
                _engine.SchedulePartArrival(part, part.ArrivalTime);
            }
        }

        private async void Start()
        {
            IsRunning = true;
            _timer.Start();
            await Task.Run(() => { _engine.RunUntil(1000.0); });
            _timer.Stop();
            IsRunning = false;
        }

        private void Pause()
        {
            IsRunning = false;
            _timer.Stop();
            _engine.Logger.SaveToCSV(@"C:\msim\logs");  // ? Do you have this?
        }

        private void Reset()
        {
            _timer.Stop();
            _engine.Logger.SaveToCSV(@"C:\msim\logs");  // ? Do you have this?
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
            Thread.Sleep(_simulationSpeed);
        }

        private void UpdateUI()
        {
            CurrentTime = _engine.CurrentTime;
            var stats = _engine.GetStatistics();
            Statistics.UpdateFromModel(stats);

            for (int i = 0; i < _engine.Machines.Count; i++)
            {
                var machine = _engine.Machines[i];
                var buffer = _engine.Buffers[machine.Id];
                var machineStats = stats.MachineStats.ContainsKey(machine.Id) ? stats.MachineStats[machine.Id] : null;
                Machines[i].UpdateFromModel(buffer, _engine.CurrentTime, machineStats);
            }
        }
    }
}
