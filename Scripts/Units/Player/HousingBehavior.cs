using HexGame.Units;
using System;

public class HousingBehavior : UnitBehavior, IHaveHappiness
{
    public static event Action<HousingBehavior> housingAdded;
    public static event Action<HousingBehavior> housingRemoved;

    public override void StartBehavior()
    {
        isFunctional = true;
        housingAdded?.Invoke(this);
        WorkerManager.AddHappyBuilding(this, this.GetComponent<PlayerUnit>());
    }

    public override void StopBehavior()
    {
        isFunctional = false;
        housingRemoved?.Invoke(this);
        WorkerManager.RemoveHappyBuilding(this);
    }
    public int GetHappiness()
    {
        return GetIntStat(Stat.happiness);
    }

    public string GetHappinessString()
    {
        return "";
    }
}
