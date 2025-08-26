using Sirenix.OdinInspector;
using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Hex/Triggers/Trigger First Enemy Spawn")]
public class BuildingTutorialComplete : TriggerBase
{
    public static event Action buildingTutorialComplete;
    public override void DoTrigger()
    {
        buildingTutorialComplete?.Invoke();
    }

    public static void TriggerEndOfTutorial()
    {
        buildingTutorialComplete?.Invoke();
    }
}
