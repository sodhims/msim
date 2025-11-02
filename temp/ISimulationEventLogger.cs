namespace ManufacturingSimulation.Core
{
    /// <summary>
    /// Interface for logging simulation events
    /// Implemented in Bridge project to avoid circular dependency
    /// </summary>
    public interface ISimulationEventLogger
    {
        void LogPartArrival(double time, string partId, string orderNumber);
        void LogQueueEntry(double time, string partId, string orderNumber, string machineName, int queueSize);
        void LogProcessingStart(double time, string partId, string orderNumber, string machineName);
        void LogProcessingEnd(double time, string partId, string orderNumber, string machineName, double processingTime);
        void LogPartCompletion(double time, string partId, string orderNumber, double flowTime);
        void Flush();
    }
}
