using DG.Tweening;
using Nova;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;

public class NovaGroup : MonoBehaviour
{
    [SerializeField, ToggleLeft] private bool _interactable = true;
    [SerializeField, ToggleLeft] private bool _ObstructDrags;
    [SerializeField] private List<GameObject> excludeList;
    [SerializeField] private bool subscribeToMaskColorChange = false;
    public bool interactable
    {
        get { return _interactable; }
        set
        {
            if (interactables == null || interactables.Length == 0)
                UpdateInteractables();
            SetInteractable(value);
            _interactable = value;
        }
    }
    private Interactable[] interactables;
    private Scroller[] scrollers;
    public bool obstructDrags
    {
        get { return _ObstructDrags; }
        set
        {
            SetObstructDrags(value);
            _ObstructDrags = value;
        }
    }

    private void OnEnable()
    {
        if(subscribeToMaskColorChange && this.gameObject.TryGetComponent(out ClipMask clipMask))
            clipMask.colorChanged += MaskColorChange; 

        UpdateInteractables();
        scrollers = this.GetComponentsInChildren<Scroller>(true);
    }

    private void OnDisable()
    {
        if (subscribeToMaskColorChange && this.gameObject.TryGetComponent(out ClipMask clipMask))
            clipMask.colorChanged -= MaskColorChange;
        DOTween.Kill(this,true);
    }

    private void MaskColorChange(Color startColor, Color endingColor)
    {
        if(endingColor.a >= startColor.a)
            UpdateInteractables();
        interactable = endingColor.a >= startColor.a;
    }

    private void SetInteractable(bool isInteractable)
    {
        foreach (var interact in interactables)
        {
            if (excludeList.Contains(interact.gameObject))
                continue;
            interact.enabled = isInteractable;
        }

        if (scrollers != null)
        {
            foreach (var scroller in scrollers)
            {
                if (excludeList.Contains(scroller.gameObject))
                    continue;
                scroller.enabled = isInteractable;
            }
        }
    }

    private void SetObstructDrags(bool blocksRaycast)
    {
        foreach (var interact in interactables)
        {
            if (excludeList.Contains(interact.gameObject))
                continue;
            interact.ObstructDrags = blocksRaycast;
        }
    }

    [Button]
    public void UpdateInteractables()
    {
        this.interactables = this.GetComponentsInChildren<Interactable>(true);
    }
}
