using UnityEngine;

public class SteamManager : MonoBehaviour
{
    private static bool isConnected;
    public static bool IsConnected => isConnected;

    [SerializeField] private GameSettings gameSettings;
    private static uint appId;
    public static uint AppId => appId;
    private void OnEnable()
    {
        appId = gameSettings.IsDemo ? (uint)2630960 : (uint)2510180;
        try
        {
            Steamworks.SteamClient.Init(appId, true);
            isConnected = true;
        }
        catch (System.Exception e)
        {
            // Something went wrong! Steam is closed?
            isConnected = false;
        }
    }

    private void OnDisable()
    {
        Steamworks.SteamClient.Shutdown();
    }

    void Update()
    {
        Steamworks.SteamClient.RunCallbacks();
    }

    public static void OpenSteamPage()
    {
        //Steamworks.SteamFriends.OpenStoreOverlay(1446560);
        bool isDemo = FindObjectOfType<GameSettingsManager>().IsDemo;
        string storeURL = isDemo ? "https://store.steampowered.com/app/2630960/Deep_Space_Directive_Demo" : "https://store.steampowered.com/app/2510180/Deep_Space_Directive";
        
        if (isConnected)
            Steamworks.SteamFriends.OpenWebOverlay(storeURL);
        else
            Application.OpenURL(storeURL);
    }
}
