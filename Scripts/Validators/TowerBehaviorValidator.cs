#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor.Validation;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;

[assembly: RegisterValidator(typeof(TowerBehaviorValidator))]

// Note, if you register this as a rule instead, the validator cannot be generic.
//[assembly: RegisterValidationRule(typeof(ShootingBehaviorValidator), Name = "Name", Description = "Some description text.")]
public class TowerBehaviorValidator: RootObjectValidator<HexGame.Units.TowerBehavior>
{
    protected override void Validate(ValidationResult result)
    {
        if (this.Object.GetComponentInChildren<HexGame.Units.UnitDetection>() == null)
            result.AddError("This object doesn't appear to have a Unit Detection prefab.");
    }
}
#endif
