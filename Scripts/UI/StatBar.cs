using Nova;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ClipMask))]
public class StatBar : MonoBehaviour
{
    [SerializeField] private UIBlock2D statBar;
    [SerializeField] private TextBlock label;
    private List<UIBlock2D> statBars = new List<UIBlock2D>();
    private ClipMask clipMask;

    private void OnEnable()
    {
        statBars.Add(statBar);
        clipMask = GetComponent<ClipMask>();
    }

    public void UpdateStatBar(float value, int index, Color color)
    {
        if (index >= statBars.Count)
        {
            UIBlock2D newBar;
            newBar = Instantiate(statBar);
            newBar.transform.SetParent(statBar.transform.parent);
            newBar.Size.X.Percent = Mathf.Max(0.025f,Mathf.Clamp01(value));
            newBar.Color = color;
            statBars.Add(newBar);
        }
        else
        {
            statBars[index].gameObject.SetActive(true);
            statBars[index].Size.X.Percent = Mathf.Max(0.025f, Mathf.Clamp01(value));
            statBars[index].Color = color;
        }
    }

    public void Enable()
    {
        Color tint = clipMask.Tint;
        tint.a = 0.8f;
        clipMask.Tint = tint;

        for (int i = 1; i < statBars.Count; i++)
            statBars[i].gameObject.SetActive(true);
    }

    public void Disable()
    {
        Color tint = clipMask.Tint;
        tint.a = 0f;
        clipMask.Tint = tint;

        for (int i = 1; i < statBars.Count; i++)
            statBars[i].gameObject.SetActive(false);
    }

    internal void ResetAllBars()
    {
        foreach (var bar in statBars)
            bar.Size.X.Percent = 0.025f;
    }

    public void SetLabel(string labelText)
    {
        label.Text = labelText;
    }
}
