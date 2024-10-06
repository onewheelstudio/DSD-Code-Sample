#if UNITY_EDITOR
using HexGame.Resources;
using Sirenix.OdinInspector.Editor.Validation;
using System.Linq;

[assembly: RegisterValidationRule(typeof(ResourceProductionValidator))]

public class ResourceProductionValidator : RootObjectValidator<ResourceProduction>
{

    protected override void Validate(ValidationResult result)
    {
        if(this.Object.GetProduction().Count == 0)
        {
            result.AddError("Resource Production must produce at least one resource.");
        }
        else if (this.Object.GetProduction().Any(x => x.amount == 0))
        {
            result.AddError("Production amount set to 0.");
        }
        else if(this.Object.GetProduction().Any(x => x.amount > CargoManager.transportAmount))
        {
            result.AddError("Production amount exceeds transport capacity - this will cause pickup issues.");
        }
    }
}
#endif
