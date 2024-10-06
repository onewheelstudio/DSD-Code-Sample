using NovaSamples.UIControls;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class FeedBackWindow : WindowPopup
{
    [SerializeField] private TextField feedback;
    [SerializeField] private PatchNotes patchNotes;
    [SerializeField] private Toggle shareToggle;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button cancelButton;

    private string formUrl = "https://docs.google.com/forms/u/1/d/e/1FAIpQLSc3mGmm0QOWHbb7iHNq6KzthvE1f-pH7XtIqITiVrVUmzp3eg/formResponse";

    private void Start()
    {
        CloseWindow();
    }

    public override void OnEnable()
    {
        base.OnEnable();
        submitButton.clicked += SubmitFeedback;
        cancelButton.clicked += CloseWindow;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        submitButton.clicked -= SubmitFeedback;
        cancelButton.clicked -= CloseWindow;
    }

    [Button]
    public void SubmitFeedback()
    {
        StartCoroutine(Post(feedback.Text));
    }

    public override void OpenWindow()
    {
        feedback.Text = "";
        blockWindowHotkeys = true;
        FindFirstObjectByType<UnitSelectionManager>()?.ClearSelection();
        base.OpenWindow();
    }

    public override void CloseWindow()
    {
        blockWindowHotkeys = false;
        base.CloseWindow();
    }

    private IEnumerator Post(string feedback)
    {
        HexTileManager htm = FindFirstObjectByType<HexTileManager>();
        int seed = htm != null ? htm.RandomizeSeed : 0;

        WWWForm form = new WWWForm();
        form.AddField("entry.1886129011", patchNotes.GetLatestVersion().ToString());
        form.AddField("entry.2016818294", seed);
        form.AddField("entry.250142743", feedback);
        if (shareToggle.ToggledOn)
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
            yield return www.SendWebRequest();

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
