using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Nova;
using NovaSamples.UIControls;

[RequireComponent(typeof(ItemView))]
public class NovaToolTip : MonoBehaviour
{
    public static event Action<List<PopUpInfo>, List<PopUpPriorityButton>, List<PopUpValues>, NovaToolTip> openToolTip;
    public static event Action<List<PopUpInfo>, List<PopUpPriorityButton>, List<PopUpValues>, NovaToolTip> updateToolTip;
    public static event Action<List<PopUpStats>, NovaToolTip> updateStats;
    public static event Action<List<PopUpResource>, NovaToolTip> updateResources;
    public static event Action closeToolTip;
    private float delay = 0.75f;
    private bool isActive;

    private ItemVisuals visuals;
    private ItemView view;
    [SerializeField]private UIBlock2D block;

    private void OnEnable()
    {
        view = GetComponent<ItemView>();
        visuals = view.Visuals;

        block.AddGestureHandler<Gesture.OnHover, ButtonVisuals>(OpenOnHoverTip);
        view.UIBlock.AddGestureHandler<Gesture.OnClick, ButtonVisuals>(OpenOnClickTip);
        view.UIBlock.AddGestureHandler<Gesture.OnUnhover, ButtonVisuals>(CloseOnUnHoverTip);
    }
    private void OnDisable()
    {
        block.RemoveGestureHandler<Gesture.OnHover, ButtonVisuals>(OpenOnHoverTip);
        view.UIBlock.RemoveGestureHandler<Gesture.OnClick, ButtonVisuals>(OpenOnClickTip);
        view.UIBlock.RemoveGestureHandler<Gesture.OnUnhover, ButtonVisuals>(CloseOnUnHoverTip);
    }

    private void CloseOnUnHoverTip(Gesture.OnUnhover evt, ButtonVisuals target)
    {
        CloseTip();
    }

    private void OpenOnClickTip(Gesture.OnClick evt, ButtonVisuals target)
    {
        Debug.Log("Opening");
        OpenTip();
    }

    IEnumerator DelayOpen()
    {
        yield return new WaitForSeconds(delay);
        if (!Mouse.current.leftButton.isPressed)
            OpenTip();
    }

    private void OpenOnHoverTip(Gesture.OnHover evt, ButtonVisuals target)
    {
        OpenTip();
    }

    private void OpenTip()
    {
        openToolTip?.Invoke(GetPopUpInfos(), GetPopUpButtons(), GetPopUpValues(), this);
        updateStats?.Invoke(GetPopUpStats(), this);
        updateResources?.Invoke(GetPopUpResources(), this);
        StartCoroutine(UpdateTip());
    }

    private IEnumerator UpdateTip()
    {
        //yield return null; // not needed but helps ensure window is open. reduces fragility due to order of code in OpenTip
        while (isActive)
        {
            //updateToolTip?.Invoke(GetPopUpInfos(), GetPopUpButtons(), GetPopUpToggle(), GetRequestPriority(), this);
            yield return null;
            yield return null;
        }
    }

    private List<PopUpValues> GetPopUpValues()
    {
        IHavePopUpValues[] havePopupInfos = this.GetComponents<IHavePopUpValues>();
        List<PopUpValues> popUpInfos = new List<PopUpValues>();
        foreach (IHavePopUpValues infoObject in havePopupInfos)
        {
            popUpInfos.AddRange(infoObject.GetPopUpValues());
        }

        return popUpInfos;
    }

    private List<PopUpInfo> GetPopUpInfos()
    {
        IHavePopupInfo[] havePopupInfos = this.GetComponents<IHavePopupInfo>();
        List<PopUpInfo> popUpInfos = new List<PopUpInfo>();
        foreach (IHavePopupInfo infoObject in havePopupInfos)
        {
            popUpInfos.AddRange(infoObject.GetPopupInfo());
        }

        return popUpInfos;
    }

    private List<PopUpPriorityButton> GetPopUpButtons()
    {
        IHavePopUpButtons[] havePopUpButtons = this.GetComponents<IHavePopUpButtons>();
        List<PopUpPriorityButton> popUpButtons = new List<PopUpPriorityButton>();
        foreach (var buttons in havePopUpButtons)
            popUpButtons.AddRange(buttons.GetPopUpButtons());

        return popUpButtons;
    }

    private PopUpCanToggle GetPopUpToggle()
    {
        if (this.TryGetComponent<ICanToggle>(out ICanToggle canToggle))
            return canToggle.CanToggleOff();
        else
            return new PopUpCanToggle();
    }

    private RequestStorageInfo GetRequestPriority()
    {
        if (this.TryGetComponent<HexGame.Units.IHaveRequestPriority>(out HexGame.Units.IHaveRequestPriority priority))
            return priority.GetPopUpRequestPriority();
        else
            return new RequestStorageInfo();
    }

    private List<PopUpStats> GetPopUpStats()
    {
        IHaveStats[] stats = this.GetComponents<IHaveStats>();
        List<PopUpStats> popUpStats = new List<PopUpStats>();
        foreach (var statList in stats)
            popUpStats.AddRange(statList.GetPopUpStats());

        return popUpStats;
    }

    private List<PopUpResource> GetPopUpResources()
    {
        IHaveResources[] resources = this.GetComponents<IHaveResources>();
        List<PopUpResource> popUpResources = new List<PopUpResource>();
        foreach (var resourceList in resources)
            popUpResources.AddRange(resourceList.GetPopUpResources());

        return popUpResources;
    }

    private void CloseTip()
    {
        closeToolTip?.Invoke();
    }

    private bool CheckIfOpenToolTip()
    {
        if (PlayerPrefs.GetInt("ShowToolTips", 1) == 1)
            return true;
        else
            return false;
    }


}
