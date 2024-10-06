using DG.Tweening;
using NovaSamples.UIControls;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class PlayTestFeedBackWindow : WindowPopup
{
    [SerializeField] private TextField favoriteParts;
    [SerializeField] private TextField leastFavoriteParts;
    [SerializeField] private TextField confusedOrBored;
    [SerializeField] private TextField playAgain;
    [SerializeField] private TextField otherComments;
    [SerializeField] private PatchNotes patchNotes;
    [SerializeField] private Toggle shareSystemToggle;
    [SerializeField] private Toggle shareGameDataToggle;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button cancelButton;

    [Header("ToolTips")]
    [SerializeField] private InfoToolTip gamePlayData;
    [SerializeField] private InfoToolTip systemData;

    [Header("Options")]
    [SerializeField] private bool quitOnSubmit = true;

    private string formUrl = "https://docs.google.com/forms/u/1/d/e/1FAIpQLSdWGmIbLHvp4wvv1PwckQj2cnbo5MCZJxHsAs33TRyPqDgWZw/formResponse";

    private void Start()
    {
        CloseWindow();
    }

    public override void OnEnable()
    {
        base.OnEnable();
        submitButton.clicked += SubmitFeedback;
        cancelButton.clicked += CloseWindow;
        if(quitOnSubmit)
            cancelButton.clicked += QuitOnSend;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        submitButton.clicked -= SubmitFeedback;
        cancelButton.clicked -= CloseWindow;
        if(quitOnSubmit)
            cancelButton.clicked -= QuitOnSend;
    }

    public override void OpenWindow()
    {
        base.OpenWindow();
        FeedbackData feedbackData = new FeedbackData();
        gamePlayData.SetToolTipInfo("Shared Game Play Data", feedbackData.GetGameData);
        systemData.SetToolTipInfo("Shared Game Play Data", feedbackData.GetSystemInfo);
    }

    [Button]
    public void SubmitFeedback()
    {
        FeedbackData feedback = new FeedbackData();
        feedback.favoriteParst = favoriteParts.Text;
        feedback.leastFavoriteParts = leastFavoriteParts.Text;
        feedback.confusedOrBored = confusedOrBored.Text;
        feedback.wouldYouPlayAgain = playAgain.Text;
        feedback.otherComments = otherComments.Text;
        StartCoroutine(Post(feedback));
    }

    private IEnumerator Post(FeedbackData feedback)
    {
        HexTileManager htm = FindFirstObjectByType<HexTileManager>();
        GamePlayStats gamePlayStats = FindFirstObjectByType<GamePlayStats>();
        int seed = htm != null ? htm.RandomizeSeed : 0;

        WWWForm form = new WWWForm();
        form.AddField("entry.616627476", feedback.favoriteParst);
        form.AddField("entry.344959330", feedback.leastFavoriteParts);
        form.AddField("entry.974295519", feedback.confusedOrBored);
        form.AddField("entry.1231388011", feedback.wouldYouPlayAgain);
        form.AddField("entry.1860359394", feedback.otherComments);
        
        if (shareGameDataToggle.ToggledOn && gamePlayStats)
        {
            form.AddField("entry.1651982571", gamePlayStats.TimePlayed.ToString());
            form.AddField("entry.841375718", gamePlayStats.BuildingsBuilt);
            form.AddField("entry.1630352018", gamePlayStats.UpgradesUnlocked);
            form.AddField("entry.1532552802", gamePlayStats.DaysPlayed);
            form.AddField("entry.985125403", gamePlayStats.TilesPlaced);
            form.AddField("entry.453610537", gamePlayStats.WorkersHired);
            form.AddField("entry.3988777885", gamePlayStats.LoadsSold);
        }

        if (shareSystemToggle.ToggledOn)
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
                if (quitOnSubmit)
                    QuitOnSend();
                else
                    CloseWindow();
            }
            else
            {
                Debug.LogError("Error in feedback submission: " + www.error);
                if (quitOnSubmit)
                    QuitOnSend();
                else
                    CloseWindow();
            }
        }

    }

    private void QuitOnSend()
    {
        if (quitOnSubmit)
        {
            DOTween.KillAll();
            Application.Quit();
        }
    }

    public class FeedbackData
    {
        public string favoriteParst = "";
        public string leastFavoriteParts = "";
        public string confusedOrBored = "";
        public string pacing = "";
        public string wouldYouPlayAgain = "";
        public string otherComments = "";
        public string lengthOfPlay = "";
        public string buildingsAdded = "";
        public string upgradesUnlocked = "";
        public string daysPlayed = "";
        public string tilesPlaced = "";
        public string workersHired = "";
        public string loadsSold = "";

        public string GetSystemInfo()
        {
            return $"OS: {SystemInfo.operatingSystem}" +
                $"\nRam: { SystemInfo.systemMemorySize}" +
                $"\nProcessor: {SystemInfo.processorType}" +
                $"\nResolution: {Screen.currentResolution}" +
                $"\nGraphics: {SystemInfo.graphicsDeviceName}" +
                $"\nGraphics Memory: {SystemInfo.graphicsMemorySize}";
        }

        public string GetGameData()
        {
            GamePlayStats gamePlayStats = FindFirstObjectByType<GamePlayStats>();
            return gamePlayStats.ToString();
        }
    }
}
