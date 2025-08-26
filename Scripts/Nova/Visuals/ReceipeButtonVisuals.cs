using HexGame.Resources;
using HexGame.Units;
using Nova;
using NovaSamples.UIControls;
using UnityEngine;
public class ReceipeButtonVisuals : ButtonVisuals
{
    public ListView requirements;
    public ListView products;
    public Button receipeButton;
    public RecipeInfo receipeInfo;
    public bool Initialized = false;
    private PlayerResources playerResources;
    private UnitManager unitManager;

    public void Initialize()
    {
        if (Initialized)
            return;

        playerResources = GameObject.FindFirstObjectByType<PlayerResources>();
        unitManager = GameObject.FindFirstObjectByType<UnitManager>();
        requirements.AddDataBinder<ResourceAmount, UnitInfoButtonVisuals>(BindResources);
        products.AddDataBinder<ResourceAmount, UnitInfoButtonVisuals>(BindResources);

        Initialized = true;
    }

    private void BindResources(Data.OnBind<ResourceAmount> evt, UnitInfoButtonVisuals target, int index)
    {
        target.icon.SetImage(playerResources.GetResourceTemplate(evt.UserData.type).icon);
        target.icon.Color = playerResources.GetResourceTemplate(evt.UserData.type).resourceColor;

        int countPerMinute = Mathf.Max(0, Mathf.FloorToInt((60f / receipeInfo.timeToProduce * GameConstants.GameSpeed) * evt.UserData.amount));
        string description = countPerMinute.ToString() + " per min";
        description = TMPHelper.Color(description, ColorManager.GetColor(ColorCode.repuation));
        target.infoToolTip.SetToolTipInfo(evt.UserData.type.ToNiceString(), playerResources.GetResourceTemplate(evt.UserData.type).icon, description);

        if (UnitSelectionManager.selectedUnitType == PlayerUnitType.buildingSpot)
        {
            PlayerUnitType typeToBuild = UnitSelectionManager.selectedUnit.GetComponent<BuildingSpotBehavior>().unitTypeToBuild;
            target.label.Text = $"{evt.UserData.amount}/{unitManager.GetUnitCost(typeToBuild, evt.UserData.type).amount}";
        }
        else
            target.label.Text = evt.UserData.amount.ToString();
    }

}
