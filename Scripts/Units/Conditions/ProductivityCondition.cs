using Sirenix.OdinInspector;
using UnityEngine;

public abstract class ProductivityCondition : ScriptableObject
{
    [Range(0.1f, 3f)]
    [InfoBox("Smaller is faster. Larger is slower.")]
    public float boost = 1f;
    [TextArea(3, 5)]
    public string description;
    public abstract float ProductivityMultiplier(GameObject unit);
}
