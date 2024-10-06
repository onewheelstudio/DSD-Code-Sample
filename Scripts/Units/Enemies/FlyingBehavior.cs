using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace HexGame.Units
{
    public class FlyingBehavior : UnitBehavior, IHaveTarget
    {
        private Unit target;
        [SerializeField]
        [Range(0.01f, 10f)]
        private float speed = 1f;
        private Tween moveTween;

        [Button]
        public void SetTarget(Unit target)
        {
            this.target = target;
            float moveTime = (this.transform.position - target.transform.position).magnitude / GetStat(Stat.speed);
            this.transform.DOLookAt(target.transform.position, 0.25f);
            moveTween = this.transform.DOMove(target.transform.position, moveTime);
        }

        private void OnDisable()
        {
            DOTween.Kill(this,true);

        }

        public override void StartBehavior()
        {
        }

        public override void StopBehavior()
        {
        }
    } 
}
