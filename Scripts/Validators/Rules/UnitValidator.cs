#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor.Validation;
using UnityEngine;
using UnityEditor;

[assembly: RegisterValidationRule(typeof(UnitValidator))]

public class UnitValidator : RootObjectValidator<HexGame.Units.Unit>
{
    // Introduce serialized fields here to make your validator
    // configurable from the validator window under rules.
    public int SerializedConfig;

    protected override void Validate(ValidationResult result)
    {
        if (this.Object.GetComponentInChildren<HexGame.Units.UnitDetection>() == null
            && this.Object.HasStat(Stat.damage)
            && this.Object.HasStat(Stat.maxRange))
            result.AddError("This object doesn't appear to have a Unit Detection prefab.");
    }
}
#endif
