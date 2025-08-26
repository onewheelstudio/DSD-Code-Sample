using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static LandmassGenerator;

[CreateAssetMenu(fileName = "Land Generation Data", menuName = "Hex/LandGenerationData")]
public class LandGenerationData : ScriptableObject
{
    [Range(3, 50)]
    [SerializeField, Tooltip("Number to spawn at origin")]
    public int chunks = 50;

    //settings
    public List<ResourceToGenerate> specialTiles = new List<ResourceToGenerate>();
    [SerializeField] private bool showDefaultTiles = false;
    [SerializeField, ShowIf("showDefaultTiles")] List<ResourceToGenerate> specialTilesDefault = new List<ResourceToGenerate>();

    [Tooltip("Min number spread around the map")]
    [Range(3, 30)]
    public int minClusters = 10;
    [Tooltip("Max number spread around the map")]
    [Range(6, 100)]
    public int maxClusters = 20;
    [Range(5, 30)]
    public int minDistance = 20;
    [Range(20, 80)]
    public int maxDistance = 60;
    [Range(1, 10)]
    public int minSize = 1;
    [Range(2, 20)]
    public int maxSize = 5;

    public static int globalSize = 42;

    [Header("Other Bits")]
    [Range(1, 10)] public int enemyCrystalCount = 4;
    [MinMaxSlider(10, 50)] public Vector2Int crystalRange = new Vector2Int(10, 50);
    [MinMaxSlider(2, 25)] public Vector2Int gapRange = new Vector2Int(8, 11);

    [Button]
    public void ResetToDefault()
    {
        chunks = 6;
        specialTiles = new List<ResourceToGenerate>(specialTilesDefault);
        minClusters = 4;
        maxClusters = 10;
        minDistance = 10;
        maxDistance = 41;
        minSize = 4;
        maxSize = 8;
        enemyCrystalCount = 4;
        crystalRange = new Vector2Int(15, 35);
        gapRange = new Vector2Int(5, 7);
    }

}
