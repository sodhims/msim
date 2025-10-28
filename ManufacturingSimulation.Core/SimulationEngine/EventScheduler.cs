namespace ManufacturingSimulation.Core.SimulationEngine
{
    public class EventScheduler
    {
        private readonly SortedSet<SimulationEvent> _eventQueue;
        private int _eventCounter = 0;

        public EventScheduler()
        {
            // Use custom comparer to handle events at same time
            _eventQueue = new SortedSet<SimulationEvent>(
                Comparer<SimulationEvent>.Create((e1, e2) =>
                {
                    int timeCompare = e1.ScheduledTime.CompareTo(e2.ScheduledTime);
                    if (timeCompare != 0) return timeCompare;
                    
                    // If same time, maintain insertion order
                    return e1.GetHashCode().CompareTo(e2.GetHashCode());
                })
            );
        }

        public void ScheduleEvent(SimulationEvent evt)
        {
            _eventQueue.Add(evt);
            _eventCounter++;
        }

        public SimulationEvent? GetNextEvent()
        {
            if (_eventQueue.Count == 0)
                return null;

            var nextEvent = _eventQueue.Min;
            if (nextEvent != null)
            {
                _eventQueue.Remove(nextEvent);
            }
            return nextEvent;
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
            return _eventQueue.Min;
        }
    }
}