using DG.Tweening;
using UnityEngine;

public class DotweenInitialization : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        DOTween.SetTweensCapacity(500, 50);
    }
}

