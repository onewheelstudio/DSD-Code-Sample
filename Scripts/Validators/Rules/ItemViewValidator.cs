#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor.Validation;
using UnityEngine;
using UnityEditor;
using Nova;

[assembly: RegisterValidationRule(typeof(ItemViewValidator), Name = "ItemVisualsValidator", Description = "Some description text.")]

public class ItemViewValidator : RootObjectValidator<ItemView>
{
    protected override void Validate(ValidationResult result)
    {
        if (this.Object.Visuals == default)
            result.AddError("Nova Item View is missing Item Visuals assignment.");
    }
}
#endif
