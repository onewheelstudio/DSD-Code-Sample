using Nova;
using NovaSamples.UIControls;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class FeedBackWindow : WindowPopup
{
    [SerializeField] private TextField feedback;
    [SerializeField] private PatchNotes patchNotes;
    [SerializeField] private GameSettings gameSettings;
    [SerializeField] private Toggle shareToggle;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button cancelButton;

    [Header("Before You Go!")]
    [SerializeField] private TextBlock headerBlock;
    [SerializeField] private TextBlock subHeaderBlock;
    [SerializeField] private TextBlock cancelButtonText;
    [SerializeField] private TextBlock submitButtonText;

    [Header("Email Report")]
    [SerializeField] private TextBlock filelist;

    private string formUrl = "https://docs.google.com/forms/u/1/d/e/1FAIpQLSc3mGmm0QOWHbb7iHNq6KzthvE1f-pH7XtIqITiVrVUmzp3eg/formResponse";

    private void Start()
    {
        CloseWindow();
    }

    public override void OnEnable()
    {
        base.OnEnable();
        cancelButton.Clicked += CloseWindow;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        submitButton.Clicked -= SubmitFeedback;
        cancelButton.Clicked -= CloseWindow;
    }

    [Button]
    public void SubmitFeedback()
    {
        StartCoroutine(Post(feedback.Text));
    }

    public override void OpenWindow()
    {
        headerBlock.Text = "Send Feedback!";
        subHeaderBlock.gameObject.SetActive(true);
        filelist.gameObject.SetActive(false);
        feedback.Text = "";
        blockWindowHotkeys = true;
        FindFirstObjectByType<UnitSelectionManager>()?.ClearSelection();
        submitButton.RemoveClickListeners();
        submitButton.Clicked += SubmitFeedback;
        base.OpenWindow();
    }

    public void OpenToEmailReport(string filename)
    {
        headerBlock.Text = "Report Bug and Send Save File";
        subHeaderBlock.gameObject.SetActive(false);
        filelist.gameObject.SetActive(true);
        filelist.Text = $"Sending Files: Player.log, Player-prev.log, {filename}.es3";
        feedback.Text = "";
        blockWindowHotkeys = true; 
        submitButton.RemoveClickListeners();
        submitButton.Clicked += () => SendReport(filename);
        submitButton.Clicked += CloseWindow;
        base.OpenWindow();
    }

    private void SendReport(string filename)
    {
        ReportData data = new ReportData();
        data.message = feedback.Text;
        data.fileName = filename;
        data.user = SteamManager.GetSteamName();
        EmailReport.SendReport(data);
    }

    [Button]
    public void OpenFeedbackOnClose()
    {
        blockWindowHotkeys = true;
        Time.timeScale = 0;
        headerBlock.Text ="Before You GO!";
        headerBlock.Color = ColorManager.GetColor(ColorCode.techCredit);
        subHeaderBlock.Text = "Help make this a better game! Share your thoughts.\nWhat worked? What didn't work?";
        subHeaderBlock.Color = ColorManager.GetColor(ColorCode.repuation);
        cancelButtonText.Text = "Quit";
        cancelButtonText.Color = ColorManager.GetColor(ColorCode.red);
        cancelButton.RemoveAllListeners();
        cancelButton.Clicked += Application.Quit;
        submitButtonText.Color = ColorManager.GetColor(ColorCode.green);
        submitButton.RemoveAllListeners();
        submitButton.Clicked += () => StartCoroutine(Post(feedback.Text, true));
        OpenWindow();
    }

    public override void CloseWindow()
    {
        blockWindowHotkeys = false;
        base.CloseWindow();
    }

    private IEnumerator Post(string feedback, bool closeOnSubmit = false)
    {
        HexTileManager htm = FindFirstObjectByType<HexTileManager>();
        int seed = htm != null ? htm.RandomizeSeed : 0;

        string buildType;
        if (gameSettings.IsDemo)
            buildType = " D";
        else if (gameSettings.IsEarlyAccess)
            buildType = " EA";
        else
            buildType = " FG";

        WWWForm form = new WWWForm();
        form.AddField("entry.1886129011", patchNotes.GetLatestVersion().ToString() + buildType);
        form.AddField("entry.2016818294", seed);
        form.AddField("entry.250142743", feedback);
        //if (shareToggle.ToggledOn)
        {
            form.AddField("entry.519723379", SystemInfo.operatingSystem);
            form.AddField("entry.1559495385", SystemInfo.systemMemorySize.ToString());
            form.AddField("entry.1509179515", SystemInfo.processorType);
            form.AddField("entry.1599050250", Screen.currentResolution.ToString());
            form.AddField("entry.2112149854", SystemInfo.graphicsDeviceName);
            form.AddField("entry.246022014", SystemInfo.graphicsMemorySize.ToString());
        }

        using (UnityWebRequest www = UnityWebRequest.Post(formUrl, form))
        {
            if(!closeOnSubmit)
                yield return www.SendWebRequest();
            else
            {
                headerBlock.Text = "Thank you!";
                yield return www.SendWebRequest();
                Application.Quit();
            }

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Feedback submitted successfully.");
                CloseWindow();
            }
            else
            {
                Debug.LogError("Error in feedback submission: " + www.error);
            }
        }
    }
}
