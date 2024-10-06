using Nova;
using System.Collections;
using System.Collections.Generic;
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

    private void Start()
    {
        StartCoroutine(UpdateHappiness());
    }

    private void OnEnable()
    {
        WorkerManager.workerStateChanged += DelayUpdateHappiness;
        WorkerMenu.WagesSet += DelayUpdateHappiness;
        WorkerMenu.RationSet += DelayUpdateHappiness;
        DayNightManager.toggleDay += DelayUpdateHappiness;
    }

    private void OnDisable()
    {
        WorkerManager.workerStateChanged -= DelayUpdateHappiness;
        WorkerMenu.WagesSet -= DelayUpdateHappiness;
        WorkerMenu.RationSet -= DelayUpdateHappiness;
        DayNightManager.toggleDay -= DelayUpdateHappiness;
    }

    private void DelayUpdateHappiness(int dayNumber)
    {
        DelayUpdateHappiness();
    }
    private void DelayUpdateHappiness()
    {
        StartCoroutine(UpdateHappiness());
    }

    private IEnumerator UpdateHappiness()
    {
        //delay one frame so worker manager can update
        yield return null;
        float efficiency = WorkerManager.globalWorkerEfficiency;
        int happiness = WorkerManager.happiness;
        icon.Color = gradient.Evaluate(efficiency);

        string tooltipText = $"Compliance: {happiness}\nEfficiency: {efficiency * 100}%";
        if(efficiencyText != null)
            efficiencyText.Text = ($"{efficiency * 100}%");

        if (efficiency >= 0.85f)
        {
            icon.SetImage(happyFace);
            toolTip?.SetToolTipInfo("Compliance", happyFace, tooltipText);
        }
        else if (efficiency > 0.70f)
        {
            icon.SetImage(neutralFace);
            toolTip?.SetToolTipInfo("Compliance", happyFace, tooltipText);
        }
        else
        {
            icon.SetImage(sadFace);
            toolTip?.SetToolTipInfo("Compliance", sadFace, tooltipText);
        }
    }

}
