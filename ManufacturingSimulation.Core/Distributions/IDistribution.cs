using System;

namespace ManufacturingSimulation.Core.Distributions
{
    /// <summary>
    /// Base interface for all probability distributions
    /// </summary>
    public interface IDistribution
    {
        string Name { get; }
        string Description { get; }
        double Sample(Random random);
        double Mean { get; }
    }

    /// <summary>
    /// Exponential distribution - commonly used for arrival times
    /// </summary>
    public class ExponentialDistribution : IDistribution
    {
        public string Name => "Exponential";
        public string Description => "Exponential (λ)";
        public double Rate { get; set; }  // Lambda parameter
        public double Mean => 1.0 / Rate;

        public ExponentialDistribution(double rate = 1.0)
        {
            if (rate <= 0)
                throw new ArgumentException("Rate must be positive", nameof(rate));
            Rate = rate;
        }

        public double Sample(Random random)
        {
            return -Math.Log(1.0 - random.NextDouble()) / Rate;
        }

        public override string ToString() => $"Exponential(λ={Rate:F3})";
    }

    /// <summary>
    /// Normal (Gaussian) distribution - common for processing times
    /// </summary>
    public class NormalDistribution : IDistribution
    {
        public string Name => "Normal";
        public string Description => "Normal (μ, σ)";
        public double Mean { get; set; }
        public double StdDev { get; set; }

        public NormalDistribution(double mean = 0, double stdDev = 1)
        {
            if (stdDev <= 0)
                throw new ArgumentException("Standard deviation must be positive", nameof(stdDev));
            Mean = mean;
            StdDev = stdDev;
        }

        public double Sample(Random random)
        {
            // Box-Muller transform
            double u1 = 1.0 - random.NextDouble();
            double u2 = 1.0 - random.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return Mean + StdDev * randStdNormal;
        }

        public override string ToString() => $"Normal(μ={Mean:F2}, σ={StdDev:F2})";
    }

    /// <summary>
    /// Uniform distribution - constant probability between min and max
    /// </summary>
    public class UniformDistribution : IDistribution
    {
        public string Name => "Uniform";
        public string Description => "Uniform [a, b]";
        public double Min { get; set; }
        public double Max { get; set; }
        public double Mean => (Min + Max) / 2.0;

        public UniformDistribution(double min = 0, double max = 1)
        {
            if (min >= max)
                throw new ArgumentException("Min must be less than Max");
            Min = min;
            Max = max;
        }

        public double Sample(Random random)
        {
            return Min + (Max - Min) * random.NextDouble();
        }

        public override string ToString() => $"Uniform[{Min:F2}, {Max:F2}]";
    }

    /// <summary>
    /// Triangular distribution - useful for three-point estimates
    /// </summary>
    public class TriangularDistribution : IDistribution
    {
        public string Name => "Triangular";
        public string Description => "Triangular (a, b, c)";
        public double Min { get; set; }
        public double Mode { get; set; }
        public double Max { get; set; }
        public double Mean => (Min + Mode + Max) / 3.0;

        public TriangularDistribution(double min = 0, double mode = 0.5, double max = 1)
        {
            if (min >= max || mode < min || mode > max)
                throw new ArgumentException("Invalid triangular distribution parameters");
            Min = min;
            Mode = mode;
            Max = max;
        }

        public double Sample(Random random)
        {
            double u = random.NextDouble();
            double fc = (Mode - Min) / (Max - Min);
            
            if (u < fc)
                return Min + Math.Sqrt(u * (Max - Min) * (Mode - Min));
            else
                return Max - Math.Sqrt((1 - u) * (Max - Min) * (Max - Mode));
        }

        public override string ToString() => $"Triangular({Min:F2}, {Mode:F2}, {Max:F2})";
    }

    /// <summary>
    /// Constant (Deterministic) distribution - always returns the same value
    /// </summary>
    public class ConstantDistribution : IDistribution
    {
        public string Name => "Constant";
        public string Description => "Constant (c)";
        public double Value { get; set; }
        public double Mean => Value;

        public ConstantDistribution(double value = 1.0)
        {
            Value = value;
        }

        public double Sample(Random random)
        {
            return Value;
        }

        public override string ToString() => $"Constant({Value:F2})";
    }

    /// <summary>
    /// Lognormal distribution - right-skewed, useful for highly variable processes
    /// </summary>
    public class LognormalDistribution : IDistribution
    {
        public string Name => "Lognormal";
        public string Description => "Lognormal (μ, σ)";
        public double MuLog { get; set; }  // Mean of underlying normal
        public double SigmaLog { get; set; }  // StdDev of underlying normal
        public double Mean => Math.Exp(MuLog + SigmaLog * SigmaLog / 2.0);

        public LognormalDistribution(double muLog = 0, double sigmaLog = 1)
        {
            if (sigmaLog <= 0)
                throw new ArgumentException("Sigma must be positive", nameof(sigmaLog));
            MuLog = muLog;
            SigmaLog = sigmaLog;
        }

        public double Sample(Random random)
        {
            // Sample from normal distribution
            double u1 = 1.0 - random.NextDouble();
            double u2 = 1.0 - random.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            double normalSample = MuLog + SigmaLog * randStdNormal;
            
            return Math.Exp(normalSample);
        }

        public override string ToString() => $"Lognormal(μ={MuLog:F2}, σ={SigmaLog:F2})";
    }

    /// <summary>
    /// Gamma distribution - flexible shape, useful for service times
    /// </summary>
    public class GammaDistribution : IDistribution
    {
        public string Name => "Gamma";
        public string Description => "Gamma (α, β)";
        public double Shape { get; set; }  // Alpha
        public double Scale { get; set; }  // Beta
        public double Mean => Shape * Scale;

        public GammaDistribution(double shape = 2, double scale = 1)
        {
            if (shape <= 0 || scale <= 0)
                throw new ArgumentException("Shape and scale must be positive");
            Shape = shape;
            Scale = scale;
        }

        public double Sample(Random random)
        {
            // Marsaglia and Tsang method for shape >= 1
            if (Shape >= 1)
            {
                double d = Shape - 1.0 / 3.0;
                double c = 1.0 / Math.Sqrt(9.0 * d);

                while (true)
                {
                    double x, v;
                    do
                    {
                        x = SampleNormal(random);
                        v = 1.0 + c * x;
                    } while (v <= 0);

                    v = v * v * v;
                    double u = random.NextDouble();

                    if (u < 1 - 0.0331 * x * x * x * x)
                        return d * v * Scale;

                    if (Math.Log(u) < 0.5 * x * x + d * (1 - v + Math.Log(v)))
                        return d * v * Scale;
                }
            }
            else
            {
                // For shape < 1, use acceptance-rejection
                double u = random.NextDouble();
                return GammaDistribution.SampleGammaFraction(random, Shape) * Math.Pow(u, 1.0 / Shape) * Scale;
            }
        }

        private static double SampleNormal(Random random)
        {
            double u1 = 1.0 - random.NextDouble();
            double u2 = 1.0 - random.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }

        private static double SampleGammaFraction(Random random, double shape)
        {
            return new GammaDistribution(shape + 1, 1).Sample(random) * Math.Pow(random.NextDouble(), 1.0 / shape);
        }

        public override string ToString() => $"Gamma(α={Shape:F2}, β={Scale:F2})";
    }
}
