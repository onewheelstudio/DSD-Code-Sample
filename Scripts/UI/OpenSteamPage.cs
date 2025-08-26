using NovaSamples.UIControls;
using UnityEngine;

public class OpenSteamPage : MonoBehaviour
{
    private Button button;

    private void OnEnable()
    {
        button = GetComponent<Button>();
        button.Clicked += SteamManager.OpenSteamPage;
    }

    private void OnDisable()
    {
        button.Clicked -= SteamManager.OpenSteamPage;
    }
}
