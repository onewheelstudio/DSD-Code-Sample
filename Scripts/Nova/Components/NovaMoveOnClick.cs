using DG.Tweening;
using NovaSamples.UIControls;
using Sirenix.OdinInspector;
using UnityEngine;

[RequireComponent(typeof(Button))]
public class NovaMoveOnClick : MonoBehaviour
{
    [SerializeField] public Vector3 start;
    [SerializeField] private Vector3 end;
    [SerializeField] private Transform transformToMove;
    [SerializeField] private bool isAtStart = true;
    private Button button;

    private void OnEnable()
    {
        button = this.GetComponent<Button>();
        button.OnClicked.AddListener(() => DoAnimation());
    }

    private void OnDisable()
    {
        DOTween.Kill(this,true);
    }

    private void DoAnimation()
    {
        if(isAtStart)
        {
            //moveAnimation.startPosition = start;
            //moveAnimation.endPosition = end;
            //moveAnimation.transformToMove = transformToMove;
            transformToMove.DOLocalMove(end, 0.25f);
        }
        else
        {
            //moveAnimation.startPosition = end;
            //moveAnimation.endPosition = start;
            //moveAnimation.transformToMove = transformToMove;
            transformToMove.DOLocalMove(start, 0.25f);
        }

        isAtStart = !isAtStart;
    }

    [ButtonGroup]
    private void SetStart()
    {
        if (transformToMove == null)
            return;

        start = transformToMove.localPosition;
    }

    [ButtonGroup]
    private void SetEnd()
    {
        if (transformToMove == null)
            return;

        end = transformToMove.localPosition;
    }

    [ButtonGroup]
    private void GoToStart()
    {
        if (transformToMove == null)
            return;

        transformToMove.localPosition = start;
    }


    [ButtonGroup]
    private void GoToEnd()
    {
        if (transformToMove == null)
            return;

        transformToMove.localPosition = end;
    }
}
