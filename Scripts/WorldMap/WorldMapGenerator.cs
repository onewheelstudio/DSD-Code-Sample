using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;

public class WorldMapGenerator : MonoBehaviour
{

    [SerializeField] private int seed = 1;
    [Range(1, 10)]
    [SerializeField] private int octaves = 1;
    [Range(0f, 5f)]
    [SerializeField] private float damping = 1f;
    [SerializeField] private Texture2D texture1;
    [SerializeField] private Texture2D texture2;
    [SerializeField] private Texture2D texture3;
    [SerializeField] private Texture2D texture4;
    [SerializeField] private Texture2D texture5;
    private float[,] noiseMap;

    [SerializeField, Range(2, 512)] private int size = 256;

    private UIMapTile[] hexTiles;
    [SerializeField, Range(0.001f,1f)] private float offThreshold = 0.5f;
    [SerializeField, Range(0.1f,1f)] private float subtraction = 0.5f;
    [SerializeField, Range(0.1f,1f)] private float enemyPercent = 0.25f;
    [SerializeField, Range(1f, 6f)] private float scale = 3;

    private void OnEnable()
    {
        seed = FindObjectOfType<SessionManager>().WorldSeed;
        GenerateWorld();
    }


    [Button]
    private void Randomize()
    {
        seed = Random.Range(0, 10000);
        GenerateWorld();
    }

    [Button]
    private void DrawNoise()
    {
        NoiseMapGenerator.DrawMap(texture1, NoiseMapGenerator.GetNoiseMap(texture1.width, octaves, seed, damping, scale));
    }

    [Button]
    private void GenerateWorld()
    {
        ResetTiles();
        hexTiles = GameObject.FindObjectsOfType<UIMapTile>(true);

        List<float> stat1 = GetNormalizedNoise(seed);
        List<float> stat2 = GetNormalizedNoise(seed + 1, 0.2f, 0.8f);
        List<float> stat3 = GetNormalizedNoise(seed + 2, 0.2f, 0.8f);
        List<float> stat4 = GetNormalizedNoise(seed + 3, 0.2f, 0.8f);
        List<float> stat5 = GetNormalizedNoise(seed + 4, 0.2f, 0.8f);

        for (int i = 0; i < stat1.Count; i++)
        {
            hexTiles[i].SetNoise(stat1[i], offThreshold);
            hexTiles[i].SetStats(stat2[i], stat3[i], stat4[i], stat5[i]);
        }
    }

    private void GenerateNoise()
    {
        noiseMap = NoiseMapGenerator.GetNoiseMap(size, octaves, seed, damping, Mathf.RoundToInt(size / 2), Mathf.RoundToInt(size / 2));
        NoiseMapGenerator.DrawMap(texture1, noiseMap);
    }

    private List<float> GetNormalizedNoise(int seed, float min = 0f, float max = 1f)
    {
        float localScale = scale / 1000f;
        List<float> noiseValues = new List<float>();
        foreach (var tile in hexTiles)
        {
            float noise = NoiseMapGenerator.GetNoise(octaves, seed, damping, tile.transform.localPosition.x * localScale, tile.transform.localPosition.y * localScale);
            noise -= subtraction * NoiseMapGenerator.GetNoise(octaves, seed + 1, damping, tile.transform.localPosition.x * localScale, tile.transform.localPosition.y * localScale);
            noise = Mathf.Clamp01(noise);
            noiseValues.Add(noise);
        }

        return NormalizeNoise(noiseValues, min, max);
    }

    [Button]
    private void ResetTiles()
    {
        hexTiles = GameObject.FindObjectsOfType<UIMapTile>(true);

        foreach (var tile in hexTiles)
        {
            tile.ResetTileColor();
        }
    }

    public static List<float> NormalizeNoise(List<float> noise)
    {
        float minValue = Mathf.Infinity;
        float maxValue = Mathf.NegativeInfinity;

        for (int i = 0; i < noise.Count; i++)
        {
            minValue = Mathf.Min(noise[i], minValue);
            maxValue = Mathf.Max(noise[i], maxValue);
        }

        for (int i = 0; i < noise.Count; i++)
        {
            noise[i] = (noise[i] - minValue) / (maxValue - minValue);
        }

        return noise;
    }

    public static List<float> NormalizeNoise(List<float> noise, float min, float max)
    {
        float minValue = Mathf.Infinity;
        float maxValue = Mathf.NegativeInfinity;

        for (int i = 0; i < noise.Count; i++)
        {
            minValue = Mathf.Min(noise[i], minValue);
            maxValue = Mathf.Max(noise[i], maxValue);
        }

        for (int i = 0; i < noise.Count; i++)
        {
            noise[i] = (noise[i] + min - minValue) / (maxValue + max - minValue - min);
        }

        return noise;
    }
}
