using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HexGame.Units;

public class TerraformerBehavior : UnitBehavior, IHavePopUpButtons
{
    public override void StartBehavior()
    {
        isFunctional = true;
    }

    public override void StopBehavior()
    {
        isFunctional = false;
    }

    public List<PopUpPriorityButton> GetPopUpButtons()
    {
        return new List<PopUpPriorityButton>()
            {
                new PopUpPriorityButton("TerraForm", () => TerraFormingManager.ToggleTerraForming(this), 1, true),
            };
    }

    public float GetRange()
    {
        return this.GetStat(Stat.maxRange);
    }
}
