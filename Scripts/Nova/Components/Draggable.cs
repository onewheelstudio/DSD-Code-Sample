using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nova;
using System;
using NovaSamples.UIControls;

public class Draggable : MonoBehaviour
{
    private UIBlock draggableBlock;
    [SerializeField] private bool dragToLastSibling = true;

    private void Awake()
    {
        draggableBlock = this.GetComponent<UIBlock>();
    }

    private void OnEnable()
    {
        this.draggableBlock.AddGestureHandler<Gesture.OnDrag, WindowBar>(HandleDragEvent);
    }

    private void OnDisable()
    {
        this.draggableBlock.RemoveGestureHandler<Gesture.OnDrag,WindowBar>(HandleDragEvent);
    }

    private void HandleDragEvent(Gesture.OnDrag evt, WindowBar bar)
    {
        Vector3 drag = evt.DragDeltaLocalSpace;
        drag = this.transform.rotation * drag; //rotate vector to match camera

        this.transform.localPosition += drag * this.transform.localScale.x;
        if(dragToLastSibling)
            this.transform.SetAsLastSibling();
    }
}
