using NovaSamples.UIControls;
using UnityEngine;

public class OpenSteamPage : MonoBehaviour
{
    private Button button;

    private void OnEnable()
    {
        button = GetComponent<Button>();
        button.clicked += SteamManager.OpenSteamPage;
    }

    private void OnDisable()
    {
        button.clicked -= SteamManager.OpenSteamPage;
    }
}
