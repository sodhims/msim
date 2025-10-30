using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ManufacturingSimulation.Core.Models;

namespace ManufacturingSimulation.Core.Logging
{
    public class SimulationLogger
    {
        private readonly List<PartLogEntry> _partLogs;
        private readonly List<EventLogEntry> _eventLogs;
        private readonly string _logDirectory;

        public SimulationLogger(string logDirectory = "logs")
        {
            _partLogs = new List<PartLogEntry>();
            _eventLogs = new List<EventLogEntry>();
            _logDirectory = logDirectory;

            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public void LogPartCreated(Part part)
        {
            var entry = new PartLogEntry
            {
                PartId = part.Id,
                Route = string.Join("->", part.Route),
                Priority = part.Priority,
                ArrivalTime = part.ArrivalTime,
                DueDate = part.DueDate
            };
            _partLogs.Add(entry);
        }

        public void LogEvent(double time, string eventType, string machineId, string partId, string details = "")
        {
            _eventLogs.Add(new EventLogEntry
            {
                Time = time,
                EventType = eventType,
                MachineId = machineId,
                PartId = partId,
                Details = details
            });
        }

        public void LogPartProcessingStart(double time, Machine machine, Part part, double processingTime)
        {
            LogEvent(time, "ProcessingStart", machine.Name, part.Id,
                $"Operation {part.CurrentOperationIndex + 1}/{part.Route.Count}, Duration: {processingTime:F2}");
        }

        public void LogPartProcessingComplete(double time, Machine machine, Part part)
        {
            LogEvent(time, "ProcessingComplete", machine.Name, part.Id,
                $"Operation {part.CurrentOperationIndex}/{part.Route.Count} complete");
        }

        public void LogPartArrival(double time, Part part, int machineId)
        {
            LogEvent(time, "PartArrival", $"Machine{machineId}", part.Id,
                $"Arrived at buffer for Machine {machineId}");
        }

        public void LogPartTransfer(double time, Part part, int fromMachine, int toMachine)
        {
            LogEvent(time, "PartTransfer", $"M{fromMachine}->M{toMachine}", part.Id,
                $"Transferred from Machine {fromMachine} to Machine {toMachine} buffer");
        }

        public void LogPartCompleted(double time, Part part, double flowTime)
        {
            LogEvent(time, "PartComplete", "System", part.Id,
                $"All operations complete. Flow time: {flowTime:F2}");
        }

        public void LogMachineBlocked(double time, Machine machine, Part part, int targetMachineId)
        {
            LogEvent(time, "MachineBlocked", machine.Name, part.Id,
                $"Blocked trying to send to Machine {targetMachineId} (buffer full)");
        }

        public void SaveToCSV(string customDirectory = null)
        {
            string logDir = customDirectory ?? _logDirectory;

            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            string partsPath = Path.Combine(logDir, $"parts_log_{timestamp}.csv");
            SavePartsCSV(partsPath);

            string eventsPath = Path.Combine(logDir, $"events_log_{timestamp}.csv");
            SaveEventsCSV(eventsPath);

            string routingPath = Path.Combine(logDir, $"routing_log_{timestamp}.csv");  // ← NEW
            SaveRoutingCSV(routingPath);  // ← NEW

            Console.WriteLine($"Logs saved to: {Path.GetFullPath(logDir)}");
        }

        private void SaveRoutingCSV(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("PartId,OperationNumber,MachineId,MachineName,StartTime,EndTime,ProcessingTime,BufferWaitTime");

            // Group events by part and build routing history
            var partEvents = _eventLogs
                .Where(e => e.EventType == "ProcessingStart" || e.EventType == "ProcessingComplete" || e.EventType == "PartArrival")
                .OrderBy(e => e.Time)
                .GroupBy(e => e.PartId);

            foreach (var partGroup in partEvents)
            {
                string partId = partGroup.Key;
                var events = partGroup.ToList();

                int opNumber = 0;
                double? startTime = null;
                double? arrivalTime = null;
                string currentMachine = "";

                for (int i = 0; i < events.Count; i++)
                {
                    var evt = events[i];

                    if (evt.EventType == "PartArrival")
                    {
                        arrivalTime = evt.Time;
                    }
                    else if (evt.EventType == "ProcessingStart")
                    {
                        opNumber++;
                        startTime = evt.Time;
                        currentMachine = evt.MachineId;
                    }
                    else if (evt.EventType == "ProcessingComplete" && startTime.HasValue)
                    {
                        double endTime = evt.Time;
                        double processingTime = endTime - startTime.Value;
                        double bufferWait = arrivalTime.HasValue ? (startTime.Value - arrivalTime.Value) : 0;

                        sb.AppendLine($"{partId},{opNumber},{currentMachine},{evt.MachineId},{startTime.Value:F2},{endTime:F2},{processingTime:F2},{bufferWait:F2}");

                        arrivalTime = endTime; // For next operation's wait time
                        startTime = null;
                    }
                }
            }

            File.WriteAllText(path, sb.ToString());
        }
        private void SavePartsCSV(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("PartId,Route,Priority,ArrivalTime,DueDate");

            foreach (var entry in _partLogs)
            {
                sb.AppendLine($"{entry.PartId},{entry.Route},{entry.Priority},{entry.ArrivalTime:F2},{entry.DueDate:F2}");
            }

            File.WriteAllText(path, sb.ToString());
        }

        private void SaveEventsCSV(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Time,EventType,Machine,PartId,Details");

            foreach (var entry in _eventLogs.OrderBy(e => e.Time))
            {
                sb.AppendLine($"{entry.Time:F2},{entry.EventType},{entry.MachineId},{entry.PartId},\"{entry.Details}\"");
            }

            File.WriteAllText(path, sb.ToString());
        }

        public void Clear()
        {
            _partLogs.Clear();
            _eventLogs.Clear();
        }
    }

    public class PartLogEntry
    {
        public string PartId { get; set; }
        public string Route { get; set; }
        public int Priority { get; set; }
        public double ArrivalTime { get; set; }
        public double DueDate { get; set; }
    }

    public class EventLogEntry
    {
        public double Time { get; set; }
        public string EventType { get; set; }
        public string MachineId { get; set; }
        public string PartId { get; set; }
        public string Details { get; set; }
    }
}