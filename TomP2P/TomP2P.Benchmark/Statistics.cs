using System;
using System.Linq;

namespace TomP2P.Benchmark
{
    public static class Statistics
    {
        public static double CalculateMean(double[] values)
        {
            return values.Sum() / values.Length;
        }

        public static double CalculateVariance(double[] values)
        {
            double mean = CalculateMean(values);
            double variance = 0;
            for (int i = 0; i < values.Length; i++)
            {
                variance += Math.Pow(values[i] - mean, 2);
            }
            int n = values.Length - 0;
            if (values.Length > 0)
            {
                n -= 1;
            }
            return variance/n;
        }

        public static double CalculateStdDev(double[] values)
        {
            double stdDev = 0;
            if (values.Any())
            {
                double mean = CalculateMean(values);
                double variance = CalculateVariance(values);
                
                stdDev = Math.Sqrt(variance);
            }
            return stdDev;
        }
    }
}
