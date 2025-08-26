using Nova;
using UnityEngine;

public class HappinessIndicator : MonoBehaviour
{
    [SerializeField] private UIBlock2D icon;
    [SerializeField] private Sprite happyFace;
    [SerializeField] private Sprite neutralFace;
    [SerializeField] private Sprite sadFace;
    [SerializeField] private Gradient gradient;
    [SerializeField] private TextBlock efficiencyText;
    [SerializeField] InfoToolTip toolTip;

    private void OnEnable()
    {
        WorkerManager.EfficiencyChanged += UpdateHappiness;
    }

    private void OnDisable()
    {
        WorkerManager.EfficiencyChanged -= UpdateHappiness;
    }

    private void UpdateHappiness(float Efficiency)
    {
        float efficiency = WorkerManager.globalWorkerEfficiency;
        int happiness = WorkerManager.happiness;
        icon.Color = gradient.Evaluate(efficiency);

        string tooltipText = $"Compliance: {happiness}\nEfficiency: {Mathf.RoundToInt(efficiency * 100)}%\nDaily Cost: {(WorkerManager.wages * WorkerManager.TotalWorkers).ToString()}";
        if (efficiencyText != null)
            efficiencyText.Text = ($"{Mathf.RoundToInt(efficiency * 100)}%");

        if (efficiency >= 0.85f)
        {
            icon.SetImage(happyFace);
            toolTip?.SetToolTipInfo("Compliance", happyFace, tooltipText);
        }
        else if (efficiency > 0.70f)
        {
            icon.SetImage(happyFace);
            toolTip?.SetToolTipInfo("Compliance", happyFace, tooltipText);
        }
        else
        {
            icon.SetImage(sadFace);
            toolTip?.SetToolTipInfo("Compliance", sadFace, tooltipText);
        }
    }
}
