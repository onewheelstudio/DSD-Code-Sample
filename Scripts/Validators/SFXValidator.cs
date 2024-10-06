#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor.Validation;
using UnityEngine;
using UnityEditor;

[assembly: RegisterValidationRule(typeof(SFXValidator), Name = "SFXValidator", Description = "Some description text.")]

public class SFXValidator : RootObjectValidator<SFX>
{
    protected override void Validate(ValidationResult result)
    {
        if (this.Object.clips.Count == 0)
            result.AddError("No audio clips assigned to the SFX component.");

        if (this.Object.volume < 0.01f)
            result.AddWarning("The volume is set VERY low.");
    }
}
#endif
