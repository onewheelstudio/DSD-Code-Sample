using DG.Tweening;
using Nova;
using NovaSamples.UIControls;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(ClipMask))]
public abstract class WindowPopup : MonoBehaviour
{
    public static bool isOpen;
    public static List<WindowPopup> openWindows = new List<WindowPopup>();
    public bool instanceIsOpen;
    public static bool blockWindowHotkeys = false;
    public static bool BlockWindowHotkeys
    {
        get
        {
            return blockWindowHotkeys || TextFieldSelector.IsFocused;
        }
    }
    [SerializeField]
    protected ClipMask clipMask;
    protected UIControlActions uiControls;
    protected InputAction closeWindow;
    public event Action windowOpened;
    public event Action windowClosed;
    public static event Action<WindowPopup> SomeWindowOpened;
    public InputActionReference toggleWindow;
    public InputActionReference openWindow;
    protected NovaGroup novaGroup;
    [SerializeField] protected bool pauseOnOpen = false;
    protected Tween openCloseTween;
    [SerializeField] protected InteractableControl interactableControl;
    protected UIBlock2D uiBlock;
    protected static DayNightManager dayNightManager;
    protected UnitManager unitManager;
    [SerializeField]
    private bool playOpenCloseSFX = true;

    public virtual void OnEnable()
    {
        if (uiBlock == null)
            uiBlock = this.GetComponent<UIBlock2D>();

        unitManager = FindFirstObjectByType<UnitManager>();

        uiControls = new UIControlActions();
        uiControls.UI.CloseWindow.performed += CloseWindow;
        uiControls.Enable();

        if(toggleWindow != null)
        {
            toggleWindow.action.performed += ToggleWindow;
            toggleWindow.asset.Enable();
        }
        else if(openWindow != null)
        {
            openWindow.action.performed += OpenWindow;
            openWindow.asset.Enable();
        }

        if (novaGroup == null)
            novaGroup = this.GetComponent<NovaGroup>();
    }

    public virtual void OnDisable()
    {
        if (uiControls != null) //why this ever happens I'm not sure
        {
            uiControls.UI.CloseWindow.performed -= CloseWindow;
            uiControls.Disable();
        }

        if (toggleWindow != null)
        {
            toggleWindow.action.performed -= ToggleWindow;
            toggleWindow.asset.Disable();
        }
        else if (openWindow != null)
        {
            openWindow.action.performed -= OpenWindow;
            openWindow.asset.Disable();
        }
        DOTween.Kill(this.gameObject);
        DOTween.Kill(clipMask);
    }

    private void OnDestroy()
    {
        openWindows.Clear();
    }

    private void OpenWindow(InputAction.CallbackContext context)
    {
        OpenWindow();
    }

    [ButtonGroup("WindowButtons")]
    public virtual void OpenWindow()
    {
        OpenWindow(true);
    }

    public virtual void OpenWindow(bool cancelAction)
    {
        instanceIsOpen = true;
        isOpen = true;

        if (clipMask == null)
            clipMask = this.GetComponent<ClipMask>();

        openCloseTween?.Complete();

        if (Application.isPlaying)
            clipMask.DoFade(1f, 0.1f);
        else
            clipMask.SetAlpha(1f);

        this.transform.SetAsLastSibling();
        if (novaGroup != null)
        {
            novaGroup.Interactable = true;
            novaGroup.Visible = true;
        }

        windowOpened?.Invoke();
        if(cancelAction)
            SomeWindowOpened?.Invoke(this);
        if(!openWindows.Contains(this))
            openWindows.Add(this);

        if (dayNightManager == null)
            dayNightManager = FindFirstObjectByType<DayNightManager>();

        if (pauseOnOpen)
            dayNightManager.SetPause(true, false);

        if (playOpenCloseSFX)
            SFXManager.PlaySFX(SFXType.openMenu, false);
    }

    protected void CloseWindow(InputAction.CallbackContext obj)
    {
        if (unitManager != null && unitManager.IsPlacing && UnitSelectionManager.selectedUnit != null)
            return;

        if(openWindows.Count > 0 && openWindows[openWindows.Count - 1] == this)
            openWindows[openWindows.Count - 1].CloseWindow();

    }

    [ButtonGroup("WindowButtons")]
    public virtual void CloseWindow()
    {
        if (clipMask == null)
            clipMask = this.GetComponent<ClipMask>();

        openCloseTween?.Complete();

        if (Application.isPlaying)
            clipMask.DoFade(0f, 0.1f);
        else
            clipMask.SetAlpha(0f);

        isOpen = false;
        instanceIsOpen = false;
        if(novaGroup != null)
        {
            novaGroup.Interactable = false;
            novaGroup.Visible = false;
        }
        windowClosed?.Invoke();
        openWindows.Remove(this);

        if(dayNightManager == null)
            dayNightManager = FindFirstObjectByType<DayNightManager>();

        if (pauseOnOpen)
            dayNightManager.SetPause(false, false);

        if(playOpenCloseSFX)
            SFXManager.PlaySFX(SFXType.closeMenu, false);
    }

    private void ToggleWindow(InputAction.CallbackContext obj)
    {
        if (BlockWindowHotkeys)
            return;
        ToggleWindow();
    }
    public virtual void ToggleWindow()
    {
        if (this.instanceIsOpen)
            CloseWindow();
        else
            OpenWindow();
    }

    public static void SetBlockWindowHotkeys(bool block)
    {
        blockWindowHotkeys = block;
    }
}
