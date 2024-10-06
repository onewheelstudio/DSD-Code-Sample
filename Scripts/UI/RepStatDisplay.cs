using Nova;
using UnityEngine;

public class RepStatDisplay : MonoBehaviour
{
    [SerializeField] private TextBlock repAmount;
    [SerializeField] private TextBlock techCredits;

    private void Awake()
    {
        repAmount.Text = "0";
        techCredits.Text = ES3.Load(GameConstants.techCredits, 0).ToString();
    }

    private void OnEnable()
    {
        HexTechTree.techCreditChanged += UpdateTechCredits;
        ReputationManager.reputationChanged += UpdateRep;
    }

    private void OnDisable()
    {
        HexTechTree.techCreditChanged -= UpdateTechCredits;
        ReputationManager.reputationChanged -= UpdateRep;
    }

    private void UpdateRep(int reputation)
    {
        repAmount.Text = reputation.ToString();
    }

    private void UpdateTechCredits()
    {
        techCredits.Text = HexTechTree.TechCredits.ToString();
    }
}
