using System;
using System.Collections.Generic;
using ManufacturingSimulation.Core;
using ManufacturingSimulation.Database;
using ManufacturingSimulation.Database.Models;

namespace ManufacturingSimulation.Bridge
{
    /// <summary>
    /// Logs simulation events to database in real-time
    /// </summary>
    public class SimulationEventLogger : ISimulationEventLogger
    {
        private readonly int _runId;
        private readonly MesDbContext _db;
        private List<SimulationEvent> _eventBuffer;
        private const int BUFFER_SIZE = 100;

        public SimulationEventLogger(int runId, MesDbContext db)
        {
            _runId = runId;
            _db = db;
            _eventBuffer = new List<SimulationEvent>();
        }

        private void LogEvent(double time, string eventType, string partId, string orderNumber, 
            string machineName, int queueSize, string details)
        {
            var evt = new SimulationEvent
            {
                RunId = _runId,
                EventTime = time,
                EventType = eventType,
                PartId = partId,
                OrderNumber = orderNumber,
                MachineName = machineName,
                QueueSize = queueSize,
                Details = details
            };

            _eventBuffer.Add(evt);

            if (_eventBuffer.Count >= BUFFER_SIZE)
            {
                Flush();
            }
        }

        public void LogPartArrival(double time, string partId, string orderNumber)
        {
            LogEvent(time, "Arrival", partId, orderNumber, "System", 0, 
                $"Part {partId} arrived for order {orderNumber}");
        }

        public void LogQueueEntry(double time, string partId, string orderNumber, 
            string machineName, int queueSize)
        {
            LogEvent(time, "Queue Entry", partId, orderNumber, machineName, queueSize,
                $"Part entered queue at {machineName} (queue size: {queueSize})");
        }

        public void LogProcessingStart(double time, string partId, string orderNumber, 
            string machineName)
        {
            LogEvent(time, "Start Processing", partId, orderNumber, machineName, 0,
                $"Started processing on {machineName}");
        }

        public void LogProcessingEnd(double time, string partId, string orderNumber, 
            string machineName, double processingTime)
        {
            LogEvent(time, "End Processing", partId, orderNumber, machineName, 0,
                $"Finished processing on {machineName} (duration: {processingTime:F2} min)");
        }

        public void LogPartCompletion(double time, string partId, string orderNumber, 
            double flowTime)
        {
            LogEvent(time, "Completed", partId, orderNumber, "System", 0,
                $"Part {partId} completed all operations (total flow time: {flowTime:F2} min)");
        }

        public void Flush()
        {
            if (_eventBuffer.Count > 0)
            {
                _db.SimulationEvents.AddRange(_eventBuffer);
                _db.SaveChanges();
                _eventBuffer.Clear();
            }
        }
    }
}
