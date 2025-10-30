using ManufacturingSimulation.Core.Models;
using System.Collections.Generic;

namespace ManufacturingSimulation.Core.Engine.Rules
{
    /// <summary>
    /// Interface for dispatching rules that select which part to process next
    /// </summary>
    public interface IDispatchingRule
    {
        string Name { get; }
        Part? SelectNextPart(IEnumerable<Part> availableParts, double currentTime);
    }

    /// <summary>
    /// First-In-First-Out: Process parts in order of arrival
    /// </summary>
    public class FIFORule : IDispatchingRule
    {
        public string Name => "FIFO";

        public Part? SelectNextPart(IEnumerable<Part> availableParts, double currentTime)
        {
            return availableParts.FirstOrDefault();
        }
    }

    /// <summary>
    /// Priority: Process highest priority parts first
    /// </summary>
    public class PriorityRule : IDispatchingRule
    {
        public string Name => "Priority";

        public Part? SelectNextPart(IEnumerable<Part> availableParts, double currentTime)
        {
            return availableParts.OrderByDescending(p => p.Priority).FirstOrDefault();
        }
    }

    /// <summary>
    /// Shortest Processing Time: Process parts with shortest estimated time first
    /// </summary>
    public class ShortestProcessingTimeRule : IDispatchingRule
    {
        public string Name => "SPT";

        public Part? SelectNextPart(IEnumerable<Part> availableParts, double currentTime)
        {
            return availableParts.OrderBy(p => p.EstimatedProcessingTime).FirstOrDefault();
        }
    }

    /// <summary>
    /// Earliest Due Date: Process parts with earliest due date first
    /// </summary>
    public class EarliestDueDateRule : IDispatchingRule
    {
        public string Name => "EDD";

        public Part? SelectNextPart(IEnumerable<Part> availableParts, double currentTime)
        {
            return availableParts.OrderBy(p => p.DueDate).FirstOrDefault();
        }
    }

    /// <summary>
    /// Critical Ratio: CR = (Due Date - Current Time) / Remaining Processing Time
    /// Lower CR = more critical
    /// </summary>
    public class CriticalRatioRule : IDispatchingRule
    {
        public string Name => "CR";

        public Part? SelectNextPart(IEnumerable<Part> availableParts, double currentTime)
        {
            return availableParts.OrderBy(p => p.GetCriticalRatio(currentTime)).FirstOrDefault();
        }
    }

    /// <summary>
    /// Slack: Process parts with least slack time first
    /// Slack = Due Date - Current Time - Remaining Processing Time
    /// </summary>
    public class SlackRule : IDispatchingRule
    {
        public string Name => "Slack";

        public Part? SelectNextPart(IEnumerable<Part> availableParts, double currentTime)
        {
            return availableParts.OrderBy(p => p.GetSlack(currentTime)).FirstOrDefault();
        }
    }
}
