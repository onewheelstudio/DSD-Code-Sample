#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor.Validation;
using UnityEngine;
using UnityEditor;

[assembly: RegisterValidator(typeof(ProjectileDataValidator<>))]
public class ProjectileDataValidator<T> : RootObjectValidator<T>
    where T : ProjectileData
{
    protected override void Validate(ValidationResult result)
    {
        if (this.Object.projectileCost.Count == 0)
            result.AddError("Projectile cost has not been set.");

        Stat[] statsToCheck = new Stat[] { Stat.speed, Stat.reloadTime, Stat.damage, Stat.aoeRange };
        foreach (var stat in statsToCheck)
        {
            if (this.Object.GetStat(stat) == 0)
                result.AddError($"{stat} needs to be non-zero for projectile data");
        }

        if (this.Object.launchSound.clips.Count == 0)
            result.AddWarning("No lanuch SFX clips have been added.");

        if (this.Object.launchSound.volume == 0)
            result.AddWarning("Launch SFX volume is set to zero");

        if (this.Object.explosionPrefab == null)
            result.AddWarning("Missing explosion prefab");
        else if (this.Object.explosionPrefab.GetComponent<OWS.ObjectPooling.PoolObject>() == null)
            result.AddError("Explosion prefab does not have a PoolObject component");

        if (this.Object.projectilePrefab == null)
            result.AddError("Missing projectile prefab");
        else if (this.Object.projectilePrefab?.GetComponent<OWS.ObjectPooling.PoolObject>() == null)
            result.AddError("Projectile prefab does not have a PoolObject component");

    }
}
#endif
