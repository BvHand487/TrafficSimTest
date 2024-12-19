using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Utils
{
    public static class Random
    {
        public static T Select<T> (List<T> list)
        {
            return list.Count != 0 ? list[UnityEngine.Random.Range(0, list.Count)] : default(T);
        }
    
        public static float PerlinNoise(float x, float y, float scale, float octaves=1f, float persistence=1f, float lacunarity=1f)
        {
            if (scale <= 0f)
                scale = 0.01f;

            float total = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float maxValue = 0f;

            for (int i = 0; i < octaves; i++)
            {
                float sampleX = x / scale * frequency;
                float sampleY = y / scale * frequency;

                float noiseValue = Mathf.PerlinNoise(sampleX, sampleY); // Map Perlin range [0, 1] to [-1, 1]
                total += noiseValue * amplitude;

                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return total / maxValue; // Normalize to range [-1, 1]
        }
    }
}
