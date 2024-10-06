#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor.Validation;
using Sirenix.Utilities;

[assembly: RegisterValidationRule(typeof(UpgradeValidator), Name = "UpgradeValidator", Description = "Some description text.")]

public class UpgradeValidator : RootObjectValidator<Upgrade>
{
    // Introduce serialized fields here to make your validator
    // configurable from the validator window under rules.
    public int SerializedConfig;

    protected override void Validate(ValidationResult result)
    {
        if (this.Object.UpgradeName.IsNullOrWhitespace())
            result.AddError("A nice name is required.");
    }
}
#endif
