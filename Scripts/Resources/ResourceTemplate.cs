using UnityEngine;
using Sirenix.OdinInspector;

namespace HexGame.Resources
{
    [CreateAssetMenu(menuName = "Resource")]
    [ManageableData]
    public class ResourceTemplate : ScriptableObject
    {
        [DisableIf("@true")]
        public ResourceType type;
        public string resourceName
        {
            get
            {
                return type.ToString();
            }
        }
        [PreviewField(200f)]
        public Sprite icon;
        public int startingStorage = 0;
        public Color resourceColor = Color.white;
        public float baseCost = 10;
    }
}
