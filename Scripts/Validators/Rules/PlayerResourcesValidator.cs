#if UNITY_EDITOR
using HexGame.Resources;
using Sirenix.OdinInspector.Editor.Validation;

[assembly: RegisterValidationRule(typeof(PlayerResourcesValidator), Name = "Player Resources Validator")]
public class PlayerResourcesValidator : RootObjectValidator<PlayerResources>
{
    protected override void Validate(ValidationResult result)
    {
        if (this.Object.GetResources().Count != System.Enum.GetValues(typeof(ResourceType)).Length)
            result.AddError("Number of resource templates and enum values doesn't match.");
    }
}
#endif
