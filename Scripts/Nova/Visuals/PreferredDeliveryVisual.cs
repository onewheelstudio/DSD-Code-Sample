using HexGame.Resources;
using Nova;
using NovaSamples.UIControls;
using System.Collections.Generic;
using UnityEngine;

public class PreferredDeliveryVisual : ItemVisuals
{
    public TextBlock label;
    public Button deleteButton;
    public Transform parentTransform;
    public ListView resourceImages;
    private bool initialized;

    [Header("Priority")]
    public UIBlock2D priorityIcon;
    private InfoToolTip priorityToolTip;
    [SerializeField] private Sprite offPrioritySprite;
    [SerializeField] private Sprite lowPrioritySprite;
    [SerializeField] private Sprite mediumPrioritySprite;
    [SerializeField] private Sprite highPrioritySprite;
    [SerializeField] private Sprite urgentPrioriteSprite;

    public void AddResources(List<ResourceTemplate> resources)
    {
        if (!initialized)
        {
            resourceImages.AddDataBinder<ResourceTemplate, ResourceImageVisuals>(PopulateResources);
            initialized = true;
        }
        resourceImages.SetDataSource(resources);
    }

    private void PopulateResources(Data.OnBind<ResourceTemplate> evt, ResourceImageVisuals target, int index)
    {
        target.resourceImage.SetImage(evt.UserData.icon);
        target.resourceImage.Color = evt.UserData.resourceColor;
    }

    public void SetPriorityDisplay(CargoManager.RequestPriority priority)
    {
        if (priorityToolTip == null)
            priorityToolTip = priorityIcon.GetComponent<InfoToolTip>();

        switch (priority)
        {
            case CargoManager.RequestPriority.off:
                priorityIcon.SetImage(offPrioritySprite);
                priorityIcon.Color = ColorManager.GetColor(ColorCode.offPriority);
                priorityToolTip.SetToolTipInfo("Priority Off", offPrioritySprite);
                break;
            case CargoManager.RequestPriority.low:
                priorityIcon.SetImage(lowPrioritySprite);
                priorityIcon.Color = ColorManager.GetColor(ColorCode.lowPriority);
                priorityToolTip.SetToolTipInfo("Low Priority", lowPrioritySprite);
                break;
            case CargoManager.RequestPriority.medium:
                priorityIcon.SetImage(mediumPrioritySprite);
                priorityIcon.Color = ColorManager.GetColor(ColorCode.mediumPriority);
                priorityToolTip.SetToolTipInfo("Medium Priority", mediumPrioritySprite);
                break;
            case CargoManager.RequestPriority.high:
                priorityIcon.SetImage(highPrioritySprite);
                priorityIcon.Color = ColorManager.GetColor(ColorCode.highPriority);
                priorityToolTip.SetToolTipInfo("High Priority", highPrioritySprite);
                break;
            case CargoManager.RequestPriority.urgent:
                priorityIcon.SetImage(urgentPrioriteSprite);
                priorityIcon.Color = ColorManager.GetColor(ColorCode.urgentDelivery);
                priorityToolTip.SetToolTipInfo("Urgent Delivery", urgentPrioriteSprite);
                break;
            default:
                break;
        }
        
        //priority set by color
        priorityIcon.SetAlpha(0.7f);
    }
}
