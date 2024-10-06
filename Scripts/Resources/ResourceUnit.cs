using Sirenix.OdinInspector;

namespace HexGame.Units
{
    public class ResourceUnit : Unit
    {
        private static int count = 0;
        public static int Count => count;

        private new void OnEnable()
        {
            //base.OnEnable();
            Place();
            count++;
        }

        private new void OnDisable()
        {
            base.OnDisable();
            count--;
        }

        [Button]
        public override void Place()
        {
            base.Place();
        }
    }
}
