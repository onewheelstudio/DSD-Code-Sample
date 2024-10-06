#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor.Validation;
using UnityEngine;
using UnityEditor;

//[assembly: RegisterValidator(typeof(LayerMaskValidator))]

[assembly: RegisterValidationRule(typeof(LayerMaskValidator), Name = "Layer Mask Set to Zero", Description = "Checks if layer mask fields are set to zero or have not been set.")]

public class LayerMaskValidator : ValueValidator<LayerMask>
{
    protected override void Validate(ValidationResult result)
    {
        if (this.ValueEntry.SmartValue == 0)
            result.AddWarning("Layer mask value not set.");
    }
}
#endif
