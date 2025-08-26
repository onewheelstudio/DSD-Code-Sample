using NovaSamples.UIControls;
using UnityEngine;

public class TechCreditToolTip : MonoBehaviour
{
    [SerializeField] private Button hoverButton;
    private InfoToolTip toolTip;

    private void Awake()
    {
        this.toolTip = this.GetComponentInChildren<InfoToolTip>(true);
        hoverButton.OnHover.AddListener(OnHover);
    }

    private void OnHover()
    {
        int earned = HexTechTree.TechCreditCollectedYesterday;
        int spent = Mathf.Abs(HexTechTree.TechCreditSpentYesterday);
        string description = $"Used to purchase tech tree knowledge.\nEarned: {earned}\nSpent: {spent}";
        this.toolTip?.SetDescription(description);
    }
}
