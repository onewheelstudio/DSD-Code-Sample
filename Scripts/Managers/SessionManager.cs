using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(DoNotDestroyOnLoad))]
public class SessionManager : MonoBehaviour
{
    private System.Random random;
    [SerializeField] private int worldSeed;
    [SerializeField] private LeaderUpgrades leaderData;
    [SerializeField] private LevelData levelData;

    public LeaderUpgrades LeaderData { get => leaderData; set => leaderData = value; }
    public LevelData LevelData { get => levelData; set => levelData = value; }
    public int WorldSeed { get => worldSeed;}

    private void OnEnable()
    {
        worldSeed = Random.Range(0, int.MaxValue);
        random = new System.Random(WorldSeed);
    }

    public int GetNextValue()
    {
        return random.Next();
    }

    public int GetNextValue(int min, int max)
    {
        return random.Next(min, max);
    }
}
