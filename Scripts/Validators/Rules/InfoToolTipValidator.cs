#if UNITY_EDITOR
using Nova;
using NovaSamples.UIControls;
using Sirenix.OdinInspector.Editor.Validation;

[assembly: RegisterValidationRule(typeof(InfoToolTipValidator), Name = "Info Tool Tip Validator")]

public class InfoToolTipValidator : RootObjectValidator<InfoToolTip>
{
    protected override void Validate(ValidationResult result)
    {
        if (this.Object.GetComponent<ItemView>() == null)
            result.AddError("Info Tool Tip requires an Item View component to work correctly.")
                  .WithFix(() => this.Object.gameObject.AddComponent<ItemView>());
        else if (this.Object.GetComponent<ItemView>().Visuals is not ButtonVisuals)
            result.AddError("Info Tool Tip requires an Item View component with Button Visuals to work correctly.");

        if(this.Object.GetComponent<Button>() == null)
            result.AddError("Info Tool Tip requires a Button component to work correctly.")
                  .WithFix(() => this.Object.gameObject.AddComponent<Button>());
    }
}
#endif
