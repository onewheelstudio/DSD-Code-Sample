#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor.Validation;

[assembly: RegisterValidator(typeof(StatsValidator))]

public class StatsValidator : RootObjectValidator<Stats>
{
    protected override void Validate(ValidationResult result)
    {
        if (this.Object.instanceStats.Count == 0)
            result.AddError("No instance stats have been set.").WithFix("Copy Health", () => CopyHP(), true);
        else if(!this.Object.instanceStats.ContainsKey(Stat.hitPoints) && this.Object.stats.ContainsKey(Stat.hitPoints))
            result.AddError("Hit points needs to be in instance stats.").WithFix("Copy Health", () => CopyHP(), true);
        else if(!this.Object.instanceStats.ContainsKey(Stat.hitPoints))
            result.AddError("Hit points needs to be in instance stats.").WithFix("Copy Health", () => AddHP(), true);

        if(this.Object.stats.ContainsKey(Stat.shield))
            result.AddError("Shield should be in instance stats.")
                .WithFix("Move to instance stats", () => 
                { 
                    this.Object.instanceStats.Add(Stat.shield, this.Object.stats[Stat.shield]); 
                    this.Object.stats.Remove(Stat.shield); 
                }, true);

        if (this.Object.placementList.Count == 0)
            result.AddWarning("No placement tiles listed.");

        foreach (var stat in this.Object.stats.Keys)
        {
            if (this.Object.stats[stat] < 0)
                result.AddError($"{stat} must be greater than or equal to 0.").WithFix("Set to 0", () => this.Object.stats[stat] = 0, true);
        }

    }

    private void CopyHP()
    {
        if(this.Object.stats.ContainsKey(Stat.hitPoints))
        {
            this.Object.instanceStats.Add(Stat.hitPoints, this.Object.stats[Stat.hitPoints]);
            this.Object.stats.Remove(Stat.hitPoints);
        }
    }
    private void AddHP()
    {
        if(this.Object.stats.ContainsKey(Stat.hitPoints))
            this.Object.instanceStats.Add(Stat.hitPoints, 100);
        
    }
}
#endif
