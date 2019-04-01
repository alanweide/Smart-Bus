using System;
using Microsoft.SPOT;

namespace Smart_Bus
{
    public class Utilities
    {

        private static Random random = new Random();

        public static double RandomExponential(double average)
        {
            //assert average > 0.0;
            return -System.Math.Log(1 - random.NextDouble()) * average;
        }

        public static double RandomErlang(double average, int shape)
        {
            //assert average > 0.0;
            //assert shape >= 1;
            double lambda = shape / average;
            double product = 1.0;
            for (int i = 0; i < shape; i++)
            {
                product *= random.NextDouble();
            }
            return -System.Math.Log(product) / lambda;
        }

        public static double RandomUniform(double min, double range)
        {
            return min + range * random.NextDouble();
        }

    }
}
