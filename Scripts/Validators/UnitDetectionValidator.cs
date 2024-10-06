#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor.Validation;
using UnityEngine;

[assembly: RegisterValidator(typeof(UnitDetectionValidator))]

public class UnitDetectionValidator : RootObjectValidator<HexGame.Units.UnitDetection>
{
    protected override void Validate(ValidationResult result)
    {
        if (this.Object.GetComponent<SphereCollider>() == null)
            result.AddError("Unit Detection Object is missing a sphere collider")
                  .WithFix(()=>this.Object.gameObject.AddComponent<SphereCollider>());

        if (this.Object.gameObject.layer != 10 && this.Object.gameObject.layer != 17)
            result.AddError("Unit Detection object is not on a unit detection layer.");

        if (this.Object.typesToDetect.Count == 0 && this.Object.isPlayerUnit)
            result.AddError("No types to detect have been set.");
    }
}
#endif
