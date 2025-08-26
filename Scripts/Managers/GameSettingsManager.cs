using Sirenix.OdinInspector;
using UnityEngine;

public class GameSettingsManager : MonoBehaviour
{
    [SerializeField, InlineEditor] private GameSettings gameSettings;
    public bool IsDemo { get => gameSettings.IsDemo; }
    public int MaxTierForDemo { get => gameSettings.MaxTierForDemo; }
    public static event System.Action<bool> demoToggled;
    public bool IsEarlyAccess { get => gameSettings.IsEarlyAccess; }
    public int MaxTierForEarlyAccess { get => gameSettings.MaxTierForEarlyAccess; }
    public static event System.Action<bool> earlyAccessToggled;

    private void Awake()
    {
        if(FindObjectsOfType<GameSettingsManager>().Length > 1)
            gameObject.SetActive(false);
    }

    private void Start()
    {
        gameSettings.ToggleBuildType();
    }
}
