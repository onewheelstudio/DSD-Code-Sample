#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor.Validation;
using UnityEngine;
using UnityEditor;

[assembly: RegisterValidationRule(typeof(GlobalUpgradeValidator), Name = "GlobalUpgradeValidator", Description = "Some description text.")]

public class GlobalUpgradeValidator : RootObjectValidator<GlobalUpgrade>
{
    protected override void Validate(ValidationResult result)
    {
        if (this.Object.isPercent && this.Object.statValue < 1f)
            result.AddWarning($"Hey dumbass, did you mean for the stat value to be {this.Object.statValue / 100f}%");

        if (this.Object.statValue <= 0f)
            result.AddError("The stat value is zero or negative. That's dumb. Fix it.");
    }
}
#endif
