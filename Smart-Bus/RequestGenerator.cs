using System;
using Microsoft.SPOT;

namespace Smart_Bus
{
    class RequestGenerator
    {

        private static Random random = new Random();

        // Instance variables

        private const int timeBuckets = 12;
        private double[] arrivalDistribution;
        private double workingDuration;
        private double standbyBucketTimeInterval;

        // Private methods

        private double standbyDistributionFunction(double t)
        {
            // assert 0 <= t && t < this.closingTime;

            int i = (int)(t / this.standbyBucketTimeInterval);
            double baseTime = i * this.standbyBucketTimeInterval;
            double baseFraction = arrivalDistribution[i];
            double dTime = this.standbyBucketTimeInterval;
            double dFraction = arrivalDistribution[i + 1] - baseFraction;
            double extraTime = t - baseTime;
            double extraFraction = extraTime * dFraction / dTime;
            return baseFraction + extraFraction;
        }

        private double standbyDistributionFunctionInverse(double f)
        {
            // assert 0.0 <= f && f < 1.0;

            int i = 0;
            while (arrivalDistribution[i + 1] <= f)
            {
                i++;
            }
            double baseFraction = arrivalDistribution[i];
            double baseTime = i * this.standbyBucketTimeInterval;
            double dFraction = arrivalDistribution[i + 1] - baseFraction;
            double dTime = this.standbyBucketTimeInterval;
            double extraFraction = f - baseFraction;
            double extraTime = extraFraction * dTime / dFraction;
            return baseTime + extraTime;
        }

        // Public methods

        public RequestGenerator(double workingDuration)
        {
            /*
             * Probability distribution function must run from 0.0 to 1.0 exactly,
             * so scale accordingly
             */

            arrivalDistribution = new double[timeBuckets + 1];
            arrivalDistribution[0] = 0.0;
            
            // Initialize arrivalDistribution to be uniform
            for (int i = 1; i < arrivalDistribution.Length - 1; i++)
            {
                double fractionInBucket = 1.0 / timeBuckets;
                arrivalDistribution[i] = arrivalDistribution[i - 1] + fractionInBucket;
            }
            arrivalDistribution[arrivalDistribution.Length - 1] = 1.0;

            // Other variables

            this.workingDuration = workingDuration;
            this.standbyBucketTimeInterval = this.workingDuration / timeBuckets;
        }

        public double generatedRequestTime(double currentTime)
        {
            //assert 0.0 <= currentTime && currentTime < this.closingTime;

            double currentFraction = standbyDistributionFunction(currentTime);
            double f = currentFraction + random.NextDouble() * (1.0 - currentFraction);
            return standbyDistributionFunctionInverse(f);
        }

    }
}