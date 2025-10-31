using System;
using System.Collections.Generic;
using System.Linq;

namespace ManufacturingSimulation.Core.Distributions
{
    /// <summary>
    /// Central registry for all available probability distributions
    /// </summary>
    public static class DistributionManager
    {
        private static readonly Dictionary<string, Func<IDistribution>> _distributionFactories;

        static DistributionManager()
        {
            _distributionFactories = new Dictionary<string, Func<IDistribution>>
            {
                { "Constant", () => new ConstantDistribution(4.0) },
                { "Uniform", () => new UniformDistribution(2.0, 6.0) },
                { "Exponential", () => new ExponentialDistribution(0.25) },
                { "Normal", () => new NormalDistribution(4.0, 0.5) },
                { "Triangular", () => new TriangularDistribution(2.0, 4.0, 6.0) },
                { "Lognormal", () => new LognormalDistribution(1.2, 0.3) },
                { "Gamma", () => new GammaDistribution(4.0, 1.0) }
            };
        }

        public static IEnumerable<string> GetDistributionNames()
        {
            return _distributionFactories.Keys;
        }

        public static IDistribution GetDistributionByName(string name)
        {
            if (_distributionFactories.TryGetValue(name, out var factory))
            {
                return factory();
            }
            // Default to constant
            return new ConstantDistribution(4.0);
        }

        public static IDistribution CreateDistribution(string name, params double[] parameters)
        {
            switch (name)
            {
                case "Constant":
                    return new ConstantDistribution(parameters.Length > 0 ? parameters[0] : 4.0);
                
                case "Uniform":
                    return new UniformDistribution(
                        parameters.Length > 0 ? parameters[0] : 2.0,
                        parameters.Length > 1 ? parameters[1] : 6.0);
                
                case "Exponential":
                    return new ExponentialDistribution(parameters.Length > 0 ? parameters[0] : 0.25);
                
                case "Normal":
                    return new NormalDistribution(
                        parameters.Length > 0 ? parameters[0] : 4.0,
                        parameters.Length > 1 ? parameters[1] : 0.5);
                
                case "Triangular":
                    return new TriangularDistribution(
                        parameters.Length > 0 ? parameters[0] : 2.0,
                        parameters.Length > 1 ? parameters[1] : 4.0,
                        parameters.Length > 2 ? parameters[2] : 6.0);
                
                case "Lognormal":
                    return new LognormalDistribution(
                        parameters.Length > 0 ? parameters[0] : 1.2,
                        parameters.Length > 1 ? parameters[1] : 0.3);
                
                case "Gamma":
                    return new GammaDistribution(
                        parameters.Length > 0 ? parameters[0] : 4.0,
                        parameters.Length > 1 ? parameters[1] : 1.0);
                
                default:
                    return new ConstantDistribution(4.0);
            }
        }

        public static string GetDistributionDescription(string name)
        {
            var dist = GetDistributionByName(name);
            return dist.Description;
        }
    }
}
