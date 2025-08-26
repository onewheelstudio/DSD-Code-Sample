using Nova;
using NovaSamples.UIControls;
using UnityEngine;

public class SpecialTileUIVisual : ItemVisuals
{
    [SerializeField] private Dropdown tileType;
    [SerializeField] private Slider tileCount;
    [SerializeField] private Slider minClumpSize;
    [SerializeField] private Slider maxClumpSize;
    [SerializeField] private Slider minDistance;
    [SerializeField] private Slider maxDistance;
}
