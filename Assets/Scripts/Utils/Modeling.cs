using UnityEngine;

namespace Utils
{
    // Probability functions that attempt to model reality for the simulation
    public static class Modeling
    {
        // Returns the height of a building given the normalized distance from the center of the city (0, 1)
        public static float BuildingHeightFromDistance(float distance, float minHeight=1f, float maxHeight=10f, float decay=3f)
        {
            return minHeight + (maxHeight - minHeight) * ((float) System.Math.Exp((double) (-decay * distance)));
        }

        // time is a value in the range [0; 24)
        public static int CalculateTrafficFlowFromTime(float time, ref float homeToWorkTrafficChance, ref float workToHomeTrafficChance, int maxTraffic)
        {
            // Models traffic flow during peak hours - 8AM, 18PM
            float offsetY = 0.1f;
            float homeToWorkTrafficProportion = (7f - offsetY) * Math.NormalDistribution(time, 1.5f, 8f) + offsetY / 2f;
            float workToHomeTrafficProportion = (9f - offsetY) * Math.NormalDistribution(time, 2f, 17f) + offsetY / 2f;

            // Models random traffic during the day & night - more traffic during the day, less during the night
            offsetY = 1.0f;
            float randomTrafficProportion = (3f - offsetY) * Math.NormalDistribution(time, 6, 12) + offsetY;

            float totalTraffic = homeToWorkTrafficProportion + workToHomeTrafficProportion + randomTrafficProportion;
            homeToWorkTrafficChance = homeToWorkTrafficProportion / totalTraffic;
            workToHomeTrafficChance = workToHomeTrafficProportion / totalTraffic;

            // Combines the terms above to get the max number of cars during this time
            int simulatedCarCount = (int)(totalTraffic * maxTraffic / 10.0f);

            // Normalizes car count value
            int simulatedCarCountNormalized = maxTraffic * (simulatedCarCount) / Mathf.Max(simulatedCarCount, maxTraffic);

            return simulatedCarCountNormalized;
        }

        public static float JunctionTypeFromDistance(float distance)
        {
            return 0.0f;
        }
    }
}