using HexGame.Grid;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveLoadManager : MonoBehaviour
{
    [SerializeField] private string fileName = "TestDataSaveFile";

    private static List<ISaveData> data = new ();

    private void Awake()
    {
        data = new();
    }

    [Button]
    private void SaveGame()
    {
        for (int i = 0; i < data.Count; i++)
        {
            data[i].Save(fileName + ".ES3");
        }
    }

    [Button]
    private void LoadGame()
    {
        for (int i = 0; i < data.Count; i++)
        {
            data[i].Load(fileName + ".ES3");
        }
    }

    public static void RegisterData(ISaveData saveData)
    {
        data.Add(saveData);
    }
}

public interface ISaveData
{
    void RegisterDataSaving();
    void Save(string savePath);
    void Load(string loadPath);
}
