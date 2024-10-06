using NovaSamples.UIControls;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerAnalytics : MonoBehaviour
{
    [SerializeField] private bool sendInEditor = false;
    private string formUrl = "https://docs.google.com/forms/u/1/d/e/1FAIpQLSe1a9nq-x77afxcEziL3Du7_OoPoEyoX8DKg7m0iVv8JA_HXA/formResponse";

    private void OnApplicationQuit()
    {
        SubmitAnalytics();
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
        bool shareGameData = ES3.Load<bool>("ShareGameData", GameConstants.preferencesPath, true);
        bool shareSystemInfo = ES3.Load<bool>("ShareSystemInfo", GameConstants.preferencesPath, true);

        if (!shareGameData && !shareSystemInfo)
            yield break;

        GamePlayStats gamePlayStats = FindFirstObjectByType<GamePlayStats>();
        WWWForm form = new WWWForm();
        
        if (shareGameData && gamePlayStats)
        {
            form.AddField("entry.1651982571", gamePlayStats.TimePlayed.ToString());
            form.AddField("entry.841375718", gamePlayStats.BuildingsBuilt);
            form.AddField("entry.1630352018", gamePlayStats.UpgradesUnlocked);
            form.AddField("entry.1532552802", gamePlayStats.DaysPlayed);
            form.AddField("entry.985125403", gamePlayStats.TilesPlaced);
            form.AddField("entry.453610537", gamePlayStats.WorkersHired);
            form.AddField("entry.3988777885", gamePlayStats.LoadsSold);
        }

        if (shareSystemInfo)
        {
            form.AddField("entry.1684197371", SystemInfo.operatingSystem);
            form.AddField("entry.2023642400", SystemInfo.systemMemorySize.ToString());
            form.AddField("entry.1340522679", SystemInfo.processorType);
            form.AddField("entry.964313898", Screen.currentResolution.ToString());
            form.AddField("entry.1913157510", SystemInfo.graphicsDeviceName);
            form.AddField("entry.1181879683", SystemInfo.graphicsMemorySize.ToString());
        }

        using (UnityWebRequest www = UnityWebRequest.Post(formUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {

            }
            else
            {
                Debug.LogError("Error in feedback submission: " + www.error);
            }
        }
    }
}
