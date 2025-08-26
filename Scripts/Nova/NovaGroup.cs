using DG.Tweening;
using Nova;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public class NovaGroup : MonoBehaviour
{
    [SerializeField, ToggleLeft] private bool interactable = true;
    [SerializeField, ToggleLeft] private bool _ObstructDrags;
    [SerializeField] private List<GameObject> excludeList;
    [SerializeField] private bool subscribeToMaskColorChange = false;
    [SerializeField] private bool refreshBlocksOnClose = false;
    private List<UIBlock2D> visibleBlocks = new();
    private bool visible = true;
    public bool Visible
    {
        get => visible;
        set
        {
            if (visible == value)
                return;
            visible = value;
            ToggleVisible(value);
        }
    }
    public bool Interactable
    {
        get { return interactable; }
        set
        {
            if (interactables == null || interactables.Length == 0)
                UpdateInteractables();
            SetInteractable(value);
            interactable = value;
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

    private void Awake()
    {
        GetBlocks();
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
        Interactable = endingColor.a >= startColor.a;
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

    [Button]
    private void ToggleVisible(bool visible)
    {
        if (!Application.isPlaying)
            return;

        if (refreshBlocksOnClose && !visible)
            GetBlocks();

        for (int i = 0; i < visibleBlocks.Count; i++)
        {
            visibleBlocks[i].Visible = visible;
        }
    }

    private void GetBlocks()
    {
        foreach (var block in this.GetComponentsInChildren<UIBlock2D>())
        {
            if(block.Visible)
                visibleBlocks.Add(block);
        }
    }
}
