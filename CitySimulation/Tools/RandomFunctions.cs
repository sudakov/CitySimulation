using System;
using System.Collections.Generic;
using System.Text;

namespace CitySimulation.Tools
{
    public static class RandomFunctions
    {
        private const double _sqrt12 = 3.46410161514;
        public static int RollPuassonInt(this Random random, double mean)
        {
            return MathNet.Numerics.Distributions.Poisson.Sample(random, mean);
        }

        public static double RollNormal(this Random random, double mean, double std)
        {
            return MathNet.Numerics.Distributions.Normal.Sample(random, mean, std);
        }
        public static int RollNormalInt(this Random random, double mean, double std)
        {
            return (int)Math.Round(RollNormal(random, mean, std));
        }

        public static double RollUniform(this Random random, double mean, double std)
        {
            double r = std * _sqrt12/2;
            return MathNet.Numerics.Distributions.ContinuousUniform.Sample(random, mean - r, mean + r);
        }

        public static int RollUniformInt(this Random random, double mean, double std)
        {
            double r = std * _sqrt12/2;

            return MathNet.Numerics.Distributions.DiscreteUniform.Sample(random, (int)Math.Round(mean - r), (int)Math.Round(mean + r));
        }


        public static bool RollBinary(this Random random, double p)
        {
            return random.NextDouble() < p;
        }
    }
}
