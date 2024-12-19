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

        public static float JunctionTypeFromDistance(float distance)
        {
            return 0.0f;
        }
    }
}