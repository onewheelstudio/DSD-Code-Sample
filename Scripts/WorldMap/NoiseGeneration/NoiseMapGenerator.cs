using UnityEngine;
using System.Collections;
using System.Threading;
using Sirenix.OdinInspector;
public static class NoiseMapGenerator
{
    //Method gets individual point of noise
    public static float GetNoise(int octaves, int seed, float damping, float x, float y)
    {
        System.Random rand = new System.Random(seed);
        float _offset;
        _offset = rand.Next(-10000, 10000);

        float noise = 0f;

        for (int i = 0; i < octaves; i++)
        {
            float freq = Mathf.Pow(2, i);
            noise += 1 / freq * Mathf.PerlinNoise(x * freq + _offset, y * freq + _offset);
        }

        //keep noise in 
        noise = Mathf.InverseLerp(1f, 0f, noise);
        noise = Mathf.Pow(noise, damping);

        return noise;
    }

    /// <summary>
    /// Draws noise map
    /// </summary>
    /// <param name="_texture">Texture.</param>
    public static void DrawMap(Texture2D texture, float[,] map)
    {
        Color[] tempArray = texture.GetPixels();

        for (int j = 0; j < texture.width; j++)
        {

            for (int i = 0; i < texture.width; i++)
            {
                float tempValue;

                if (i < map.GetLength(0) && j < map.GetLength(1))
                    tempValue = map[i, j];
                else
                    tempValue = 0f;
                tempArray[i + j * texture.width] = new Color(tempValue, tempValue, tempValue, tempValue);
            }
        }

        texture.SetPixels(tempArray);
        texture.Apply();
    }

    /// <summary>
    /// Return 2D float array of noise
    /// </summary>
    /// <returns>The map.</returns>
    /// <param name="mapSize">Map size.</param>
    /// <param name="octaves">Octaves.</param>
    public static float[,] GetNoiseMap(int mapSize, int octaves, int seed, float damping, float scale, float xOffset = 0f, float yOffset = 0f)
    {
        float[,] map = new float[mapSize, mapSize];

        for (int j = 0; j < mapSize; j++)
        {
            for (int i = 0; i < mapSize; i++)
            {
                map[i, j] = GetNoise(octaves, seed, damping, j / (float)mapSize * scale + xOffset, i / (float)mapSize * scale + yOffset);
            }
        }

        map = NormalizeMap(map);

        return map;
    }

    public static float[,] NormalizeMap(float[,] map)
    {
        float[,] normMap = map;
        float minValue = Mathf.Infinity;
        float maxValue = Mathf.NegativeInfinity;

        for (int i = 0; i < map.GetLength(0); i++)
        {
            for (int j = 0; j < map.GetLength(1); j++)
            {
                minValue = Mathf.Min(map[i, j], minValue);
                maxValue = Mathf.Max(map[i, j], maxValue);
            }
        }

        for (int i = 0; i < map.GetLength(0); i++)
        {
            for (int j = 0; j < map.GetLength(1); j++)
            {
                normMap[i, j] = (map[i, j] - minValue) / (maxValue - minValue);
            }
        }

        return normMap;
    }
}

public class NoiseData
{
    public int _mapSize;
    public int _octaves;
    public int _seed;
    public float _damping;
    public float _xOffset;
    public float _yOffset;

    public NoiseData(int mapSize, int octaves, int seed, float damping, float xOffset = 0f, float yOffset = 0f)
    {
        _mapSize = mapSize;
        _octaves = octaves;
        _seed = seed;
        _damping = damping;
        _xOffset = xOffset;
        _yOffset = yOffset;
    }
}
