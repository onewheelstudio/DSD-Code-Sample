#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor.Validation;
using UnityEngine;
using UnityEditor;
using Nova;

[assembly: RegisterValidationRule(typeof(UIBlockValidator), Name = "Nova UI Block Validator")]

public class UIBlockValidator : RootObjectValidator<UIBlock>
{
    protected override void Validate(ValidationResult result)
    {
        if(this.Object.GameObjectLayer != 5)
            result.AddError("Nova component not on the UI layer.")
                  .WithFix("Set Layer",() => this.Object.GameObjectLayer = 5);
    }
}
#endif
