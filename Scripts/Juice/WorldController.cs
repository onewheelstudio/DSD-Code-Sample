using System.Collections.Generic;
using UnityEngine;

public class WorldController : MonoBehaviour
{
    [SerializeField] private List<GameObject> toggleAtGameStart = new List<GameObject>();
    [SerializeField] private List<GameObject> toggleOnPlayMode = new List<GameObject>();

    private void Start()
    {
        foreach (var go in toggleOnPlayMode)
        {
            go.SetActive(true);
        }
    }

    private void OnEnable()
    {
        StateOfTheGame.GameStarted += OnGameStarted;
    }

    private void OnDisable()
    {
        StateOfTheGame.GameStarted -= OnGameStarted;
    }

    private void OnGameStarted()
    {
        foreach (var go in toggleAtGameStart)
        {
            go.SetActive(true);
        }
    }
}
