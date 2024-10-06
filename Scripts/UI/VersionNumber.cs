using Nova;
using UnityEngine;

public class VersionNumber : MonoBehaviour
{
    [SerializeField] private PatchNotes patchNotes;
    private TextBlock textBlock;
    private HexTileManager htm;
    private GameSettingsManager gameSettingsManager;

    private void Awake()
    {
        htm = FindObjectOfType<HexTileManager>();
        gameSettingsManager = FindFirstObjectByType<GameSettingsManager>();  
    }

    private void OnValidate()
    {
        if (textBlock == null)
            this.textBlock = this.GetComponent<TextBlock>();
        this.textBlock.Text = $"Version {patchNotes.GetLatestVersion()}";
    }

    void Start()
    {
        if (textBlock == null)
            this.textBlock = this.GetComponent<TextBlock>();

        string version = "";

        if (htm != null)
            version = $"Seed: {htm.RandomizeSeed}";
        
        if(gameSettingsManager != null && gameSettingsManager.IsDemo)
            version += $"\nDemo Version: {patchNotes.GetLatestVersion()}";
        else if(gameSettingsManager != null && gameSettingsManager.IsEarlyAccess)
            version = $"\nEA Version: {patchNotes.GetLatestVersion()}";
        else
            version = $"Version: {patchNotes.GetLatestVersion()}";

        this.textBlock.Text = version;
    }
}
