using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static bool LeavingScene => isQuitting || ButtonLoadScene.IsLoading;

    private static bool isQuitting;

    private void Awake()
    {
        isQuitting = false;
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }
}
