using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CrystalSpin : MonoBehaviour
{
    [SerializeField] private float moveAmount = 0.2f;
    [SerializeField] private float rotateSpeed = 0.25f;

    // Start is called before the first frame update
    void Start()
    {
        Sequence crystalSquence = DOTween.Sequence();
        crystalSquence.Append(this.transform.DOLocalMoveY(this.transform.localPosition.y + moveAmount, 3));
        crystalSquence.Append(this.transform.DOLocalMoveY(this.transform.localPosition.y - moveAmount, 3));
        crystalSquence.SetLoops(-1);
    }

    private void OnDisable()
    {
        DOTween.Kill(this,true);
    }

}
