using ManufacturingSimulation.Core.Models;
using System.Collections.ObjectModel;
using System.Windows.Media;
using MachineBuffer = ManufacturingSimulation.Core.Models.Buffer;  // Add this line

namespace ManufacturingSimulation.WPF.ViewModels
{
    public class MachineViewModel : ViewModelBase
    {
        private readonly Machine _model;
        private MachineState _state;
        private string? _currentPartId;
        private double _progressPercent;
        private int _bufferCount;

        public MachineViewModel(Machine model)
        {
            _model = model;
            _state = model.State;
            Parts = new ObservableCollection<string>();
        }

        public string MachineName => _model.Name;

        public MachineState State
        {
            get => _state;
            set
            {
                if (SetProperty(ref _state, value))
                {
                    OnPropertyChanged(nameof(StatusColor));
                    OnPropertyChanged(nameof(StateText));
                }
            }
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

        public ObservableCollection<string> Parts { get; }

        public SolidColorBrush StatusColor => State switch
        {
            MachineState.Busy => Brushes.LimeGreen,
            MachineState.Blocked => Brushes.Red,
            MachineState.Starved => Brushes.Orange,
            MachineState.Idle => Brushes.Gray,
            _ => Brushes.Gray
        };

        public string StateText => State switch
        {
            MachineState.Busy => "BUSY",
            MachineState.Blocked => "BLOCKED",
            MachineState.Starved => "STARVED",
            MachineState.Idle => "IDLE",
            _ => "UNKNOWN"
        };

        public int PartsCompleted => _model.PartsCompleted;

        public void UpdateFromModel(MachineBuffer buffer, double currentTime)
        {
            State = _model.State;
            CurrentPartId = _model.CurrentPart?.Id;
            BufferCount = buffer.Count;
            
            if (_model.State == MachineState.Busy)
            {
                ProgressPercent = _model.GetProcessingProgress(currentTime);
            }
            else
            {
                ProgressPercent = 0;
            }

            // Update parts in buffer
            Parts.Clear();
            foreach (var part in buffer.Parts)
            {
                Parts.Add(part.Id);
            }

            OnPropertyChanged(nameof(PartsCompleted));
        }
    }
}