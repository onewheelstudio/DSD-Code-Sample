
using UnityEngine;

namespace HexGame.Units
{
    public class ShuttleBayBehavior : UnitBehavior
    {
        public override void StartBehavior()
        {
            isFunctional = true;
            foreach (var shuttle in this.GetComponentsInChildren<CargoShuttleBehavior>())
            {
                shuttle.GetComponent<PlayerUnit>().Place();
            }
        }

        public override void StopBehavior()
        {
            isFunctional = false;
        }
    } 
}
