namespace ManufacturingSimulation.Core
{
    /// <summary>
    /// Interface for logging simulation events
    /// </summary>
    public interface ISimulationEventLogger
    {
        void LogPartArrival(double time, string partId, string orderNumber);
        void LogQueueEntry(double time, string partId, string orderNumber, string machineName, int queueSize);
        void LogProcessingStart(double time, string partId, string orderNumber, string machineName);
        void LogProcessingEnd(double time, string partId, string orderNumber, string machineName, double processingTime);
        void LogPartCompletion(double time, string partId, string orderNumber, double flowTime);

        // NEW: Setup tracking methods
        void LogSetupStart(double time, string partId, string orderNumber, string machineName, double setupTime);
        void LogSetupComplete(double time, string partId, string orderNumber, string machineName, double setupDuration);

        // NEW: Buffer and batch operations
        void LogBufferPass(double time, string partId, string orderNumber, string bufferName);
        void LogBatchProcessing(double time, string partId, string orderNumber, string machineName, int batchSize, double batchTime);

        void Flush();
    }
}