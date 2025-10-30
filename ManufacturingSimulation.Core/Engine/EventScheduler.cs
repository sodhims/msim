namespace ManufacturingSimulation.Core.Engine
{
    public class EventScheduler
    {
        private readonly PriorityQueue<SimulationEvent, double> _eventQueue;
        private int _eventCounter;

        public EventScheduler()
        {
            _eventQueue = new PriorityQueue<SimulationEvent, double>();
            _eventCounter = 0;
        }

        public void ScheduleEvent(SimulationEvent evt)
        {
            _eventQueue.Enqueue(evt, evt.ScheduledTime);
            _eventCounter++;
        }

        public SimulationEvent? GetNextEvent()
        {
            if (_eventQueue.Count == 0)
                return null;
            return _eventQueue.Dequeue();
        }

        public bool HasEvents => _eventQueue.Count > 0;
        public int EventCount => _eventQueue.Count;
        public int TotalEventsProcessed => _eventCounter - _eventQueue.Count;

        public void Clear()
        {
            _eventQueue.Clear();
            _eventCounter = 0;
        }

        public SimulationEvent? PeekNextEvent()
        {
            if (_eventQueue.Count == 0)
                return null;
            _eventQueue.TryPeek(out var nextEvent, out _);
            return nextEvent;
        }
    }
}
