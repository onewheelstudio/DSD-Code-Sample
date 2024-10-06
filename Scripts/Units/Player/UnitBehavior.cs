using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using static HexGame.Resources.ResourceProductionBehavior;


namespace HexGame.Units
{
    public abstract class UnitBehavior : MonoBehaviour, IUnitBehavior
    {
        protected Unit _unit;
        protected Unit unit
        {
            get
            {
                if (_unit != null)
                    return _unit;

                _unit = this.gameObject.GetComponent<Unit>();

                //ugly but to work with infantry units
                if (_unit == null)
                    _unit = this.transform.parent.parent.GetComponent<Unit>();

                return _unit;
            }
        }
        public virtual bool isFunctional 
        { 
            get { return _isFunctional; } 
            protected set 
            { 
                _isFunctional = value; 
            } 
        }
        protected bool _isFunctional = false;

        protected WarningIcons warningIconInstance;
        public bool hasWarningIcon => warningIconInstance != null && warningIconInstance.transform.parent == this.transform && warningIconInstance.gameObject.activeSelf;
        protected List<ProductionIssue> issueList = new List<ProductionIssue>();

        public abstract void StartBehavior();

        public abstract void StopBehavior();

        protected enum UnitState
        {
            movingToTarget,
            searchingForNewTarget
        }

        public bool UnitBehaviorTest()
        {
            StartBehavior();

            if (!this.isFunctional)
                return false;

            return true;
        }


        public float GetStat(Stat statType)
        {
            return this.unit.GetStat(statType);
        }

        public int GetIntStat(Stat statType) => (int)GetStat(statType);


    } 
}
