using ManufacturingSimulation.Core.Models;
using ManufacturingSimulation.Core.Engine.Rules;
using System.Collections.ObjectModel;
using System.Windows.Media;
using MachineBuffer = ManufacturingSimulation.Core.Models.Buffer;

namespace ManufacturingSimulation.WPF.ViewModels
{
    public class MachineViewModel : ViewModelBase
    {
        private readonly Machine _model;
        private MachineState _state;
        private string? _currentPartId;
        private double _progressPercent;
        private int _bufferCount;
        private double _utilization;

        public MachineViewModel(Machine model)
        {
            _model = model;
            _state = model.State;
            
            AvailableRules = new ObservableCollection<string>(DispatchingRuleManager.GetRuleNames());
            SelectedRule = model.DispatchingRule.Name;
            Parts = new ObservableCollection<string>();
        }

        public string MachineName => _model.Name;

        public MachineState State
        {
            get => _state;
            set => SetProperty(ref _state, value);
        }

        public string? CurrentPartId
        {
            get => _currentPartId;
            set => SetProperty(ref _currentPartId, value);
        }

        public double ProgressPercent
        {
            get => _progressPercent;
            set => SetProperty(ref _progressPercent, value);
        }

        public int BufferCount
        {
            get => _bufferCount;
            set => SetProperty(ref _bufferCount, value);
        }

        public double Utilization
        {
            get => _utilization;
            set => SetProperty(ref _utilization, value);
        }

        public ObservableCollection<string> AvailableRules { get; }

        public string SelectedRule
        {
            get => _model.DispatchingRule.Name;
            set
            {
                if (_model.DispatchingRule.Name != value)
                {
                    _model.DispatchingRule = DispatchingRuleManager.GetRuleByName(value);
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<string> Parts { get; }

        public SolidColorBrush StatusColor => State switch
        {
            MachineState.Idle => new SolidColorBrush(Colors.Gray),
            MachineState.Busy => new SolidColorBrush(Colors.Green),
            MachineState.Blocked => new SolidColorBrush(Colors.Red),
            MachineState.Starved => new SolidColorBrush(Colors.Orange),
            _ => new SolidColorBrush(Colors.Gray)
        };

        public string StateText => State switch
        {
            MachineState.Idle => "IDLE",
            MachineState.Busy => "BUSY",
            MachineState.Blocked => "BLOCKED",
            MachineState.Starved => "STARVED",
            _ => "UNKNOWN"
        };

        public int PartsCompleted => _model.PartsCompleted;

        public void UpdateFromModel(MachineBuffer buffer, double currentTime, MachineStatistics? machineStats)
        {
            State = _model.State;
            CurrentPartId = _model.CurrentPart?.Id;
            ProgressPercent = _model.State == MachineState.Busy ? _model.GetProcessingProgress(currentTime) * 100 : 0;
            BufferCount = buffer.Count;

            if (machineStats != null)
            {
                Utilization = machineStats.Utilization;
            }

            Parts.Clear();
            foreach (var part in buffer.Parts)
            {
                Parts.Add(part.Id);
            }

            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(StateText));
            OnPropertyChanged(nameof(PartsCompleted));
        }
    }
}
