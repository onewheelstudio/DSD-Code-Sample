using NovaSamples.UIControls;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerAnalytics : MonoBehaviour
{
    [SerializeField] private bool sendInEditor = false;
    private string formUrl = "https://docs.google.com/forms/u/1/d/e/1FAIpQLSe1a9nq-x77afxcEziL3Du7_OoPoEyoX8DKg7m0iVv8JA_HXA/formResponse";
    [SerializeField] private PatchNotes patchNotes;
    [SerializeField] private GameSettings gameSettings;

    private bool sendingData = false;
    private void OnApplicationQuit()
    {
        SubmitAnalytics();
    }

    private void OnEnable()
    {
        LogTracking.errorWhenLoading += SubmitAnalytics;
    }

    private void OnDisable()
    {
        LogTracking.errorWhenLoading -= SubmitAnalytics;
    }

    [Button]
    public void SubmitAnalytics()
    {
        if(!sendInEditor && Application.isEditor)
            return;

        StartCoroutine(Post());
    }

    private IEnumerator Post()
    {
        if (sendingData)
            yield break;
        sendingData = true;

        bool shareGameData = ES3.Load<bool>("ShareGameData", GameConstants.preferencesPath, true);
        bool shareSystemInfo = ES3.Load<bool>("ShareSystemInfo", GameConstants.preferencesPath, true);

        GamePlayStats gamePlayStats = FindFirstObjectByType<GamePlayStats>();
        WWWForm form = new WWWForm();

        if (gamePlayStats == null)
            yield break;

        //if (shareGameData && gamePlayStats)
        {
            string buildType;
            if(gameSettings.IsDemo)
                buildType = " D";
            else if(gameSettings.IsEarlyAccess)
                buildType = " EA";
            else
                buildType = " FG";

            form.AddField("entry.1898748325", patchNotes.GetLatestVersion().ToString() + buildType);
            if (Application.isEditor 
                || SteamManager.GetSteamName() == "hankestevens"
                || SteamManager.GetSteamName() == "onewheelstudio")
                form.AddField("entry.1651982571", "Owner");
            else
                form.AddField("entry.1651982571", gamePlayStats.TimePlayed.ToString("N0"));

            form.AddField("entry.841375718", gamePlayStats.BuildingsBuilt);
            form.AddField("entry.1578342203", gamePlayStats.PlayerUnitsCreated);
            form.AddField("entry.1630352018", gamePlayStats.UpgradesUnlocked);
            form.AddField("entry.1532552802", gamePlayStats.DaysPlayed);
            form.AddField("entry.985125403", gamePlayStats.TilesPlaced);
            form.AddField("entry.453610537", gamePlayStats.WorkersHired);
            form.AddField("entry.398777885", gamePlayStats.LoadsSold);
            form.AddField("entry.1123585091", gamePlayStats.QuestsCompleted);
            form.AddField("entry.230864031", gamePlayStats.DirectivesCompleted);
            form.AddField("entry.672513351", gamePlayStats.AdditionalStats);
        }

        //if (shareSystemInfo)
        {
            form.AddField("entry.1684197371", SystemInfo.operatingSystem);
            form.AddField("entry.2023642400", SystemInfo.systemMemorySize.ToString());
            form.AddField("entry.1340522679", SystemInfo.processorType);
            form.AddField("entry.964313898", Screen.currentResolution.ToString());
            form.AddField("entry.1913157510", SystemInfo.graphicsDeviceName);
            form.AddField("entry.1181879683", SystemInfo.graphicsMemorySize.ToString());
        }

        //logs
        LogTracking logTracking = FindFirstObjectByType<LogTracking>();
        if(logTracking != null)
            form.AddField("entry.1432313433", logTracking.GetLogs());

        using (UnityWebRequest www = UnityWebRequest.Post(formUrl, form))
        {
            yield return www.SendWebRequest();
            sendingData = false;
            if (www.result == UnityWebRequest.Result.Success)
            {
                Application.Quit();
            }
            else
            {
                Debug.LogError("Error in feedback submission: " + www.error);
                Application.Quit();
            }
        }
    }
}
