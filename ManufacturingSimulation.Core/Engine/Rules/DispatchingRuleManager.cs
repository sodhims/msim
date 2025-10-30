using ManufacturingSimulation.Core.Engine.Rules;
using System.Collections.Generic;
using System.Linq;

namespace ManufacturingSimulation.Core.Engine.Rules
{
    /// <summary>
    /// Central registry for all available dispatching rules
    /// </summary>
    public static class DispatchingRuleManager
    {
        private static readonly Dictionary<string, IDispatchingRule> _rules;

        static DispatchingRuleManager()
        {
            _rules = new Dictionary<string, IDispatchingRule>
            {
                { "FIFO", new FIFORule() },
                { "Priority", new PriorityRule() },
                { "SPT", new ShortestProcessingTimeRule() },
                { "EDD", new EarliestDueDateRule() },
                { "CR", new CriticalRatioRule() },
                { "Slack", new SlackRule() }
            };
        }

        public static IEnumerable<IDispatchingRule> GetAllRules()
        {
            return _rules.Values;
        }

        public static IDispatchingRule GetRuleByName(string name)
        {
            if (_rules.TryGetValue(name, out var rule))
            {
                return rule;
            }
            // Default to FIFO if rule not found
            return _rules["FIFO"];
        }

        public static IEnumerable<string> GetRuleNames()
        {
            return _rules.Keys;
        }
    }
}
