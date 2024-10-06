using Sirenix.OdinInspector;
using UnityEngine;

[InfoBox("Allows this object to be toggled off from the settings menu")]
public class GameTip : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        GameSettingsWindow.showGameHints += ShowGameTips;
    }

    private void OnDestroy()
    {
        GameSettingsWindow.showGameHints -= ShowGameTips;
    }
    private void ShowGameTips(bool value)
    {
        gameObject.SetActive(value);
    }
}
