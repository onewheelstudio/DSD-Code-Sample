using HexGame.Units;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Hex/Directives/Building Count Directive")]
public partial class BuildingDirective : DirectiveBase, ISelfValidator
{
    [SerializeField]private List<BuildingRequirement> buildingRequirements = new List<BuildingRequirement>();
    [SerializeField] private bool allowAlreadyBuiltUnits = true;

    [Title("Units To Unlock")]
    [Header("Unlock on Initialize")]
    [SerializeField] private List<PlayerUnitType> unlockOnInitialize = new List<PlayerUnitType>();
    [Header("Unlock on Complete")]
    [SerializeField] private List<PlayerUnitType> unlockOnComplete = new List<PlayerUnitType>();

    public override void Initialize()
    {
        Unit.unitCreated += UnitCreated;
        Unit.unitRemoved += UnitRemoved;
        buildingRequirements.ForEach(br => br.numberBuilt = 0);
        CheckAlreadyBuilt();
        CommunicationMenu.AddCommunication(OnStartCommunication);

        BuildMenu bm = FindObjectOfType<BuildMenu>();
        unlockOnInitialize.ForEach(ut => bm.UnLockUnit(ut));
    }

    public override void OnComplete()
    {
        Unit.unitCreated -= UnitCreated;
        Unit.unitRemoved -= UnitRemoved;
        CommunicationMenu.AddCommunication(OnCompleteCommunication);
        OnCompleteTrigger.ForEach(t => t.DoTrigger());

        BuildMenu bm = FindObjectOfType<BuildMenu>();
        unlockOnComplete.ForEach(ut => bm.UnLockUnit(ut));
    }

    private void UnitRemoved(Unit unit)
    {
        if(unit is PlayerUnit playerUnit)
        {
            BuildingRequirement br = GetBuildingRequirement(playerUnit.unitType);
            if (br == null)
                return;

            br.numberBuilt--;
            DirectiveUpdated();
        }
    }

    private void UnitCreated(Unit unit)
    {
        if (unit is PlayerUnit playerUnit)
        {
            BuildingRequirement br = GetBuildingRequirement(playerUnit.unitType);
            if (br == null)
                return;

            br.numberBuilt++;
            DirectiveUpdated();
        }
    }

    public override List<bool> IsComplete()
    {
        return buildingRequirements.Select(br => br.numberBuilt >= br.totalToBuild).ToList();
    }

    public override List<string> DisplayText()
    {
        return buildingRequirements.Select(br => br.DisplayText).ToList();
    }

    private BuildingRequirement GetBuildingRequirement(PlayerUnitType unitType)
    {
        foreach (var br in buildingRequirements)
        {
            if(br.unitType == unitType)
                return br;
        }

        return null;
    }

    private void CheckAlreadyBuilt()
    {
        if (!allowAlreadyBuiltUnits)
            return;

        foreach (var br in buildingRequirements)
        {
            br.numberBuilt = UnitManager.playerUnits.Count(u => u.unitType == br.unitType);
        }

        DirectiveUpdated();
    }

    public override void Validate(SelfValidationResult result)
    {
        base.Validate(result);
        if (this.buildingRequirements.Count == 0)
            result.AddError("Building Requirements cannot be empty");
    }
}