using DG.Tweening;
using Nova;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(ClipMask))]
public abstract class WindowPopup : MonoBehaviour
{
    public static bool isOpen;
    public static List<WindowPopup> openWindows = new List<WindowPopup>();
    public bool instanceIsOpen;
    public static bool blockWindowHotkeys = false;
    [SerializeField]
    protected ClipMask clipMask;
    protected UIControlActions uiControls;
    protected InputAction closeWindow;
    public event Action windowOpened;
    public event Action windowClosed;
    public InputActionReference toggleWindow;
    public InputActionReference openWindow;
    protected NovaGroup novaGroup;
    [SerializeField] protected bool pauseOnOpen = false;
    protected Tween openCloseTween;
    [SerializeField] protected InteractableControl interactableControl;
    protected UIBlock2D uiBlock;

    public virtual void OnEnable()
    {
        if (uiBlock == null)
            uiBlock = this.GetComponent<UIBlock2D>();

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
        //if (pauseOnOpen)
            //Time.timeScale = 0f;

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
            novaGroup.interactable = true;
        if(uiBlock != null)
            this.uiBlock.Visible = true;
        windowOpened?.Invoke();
        if(!openWindows.Contains(this))
            openWindows.Add(this);

        if(pauseOnOpen)
            Time.timeScale = 0f;
    }

    protected void CloseWindow(InputAction.CallbackContext obj)
    {
        if(openWindows.Count > 0 && openWindows[openWindows.Count - 1] == this)
            openWindows[openWindows.Count - 1].CloseWindow();
    }

    [ButtonGroup("WindowButtons")]
    public virtual void CloseWindow()
    {
        //if (pauseOnOpen)
        //    Time.timeScale = 1f;

        if (clipMask == null)
            clipMask = this.GetComponent<ClipMask>();

        openCloseTween?.Complete();

        if (Application.isPlaying)
            clipMask.DoFade(0f, 0.1f);
        else
            clipMask.SetAlpha(0f);

        if (uiBlock == null)
            uiBlock = this.GetComponent<UIBlock2D>();

        if(uiBlock != null)
            this.uiBlock.Visible = false;

        //clipMask.interactable = false;
        //clipMask.obstructDrags = false;
        isOpen = false;
        instanceIsOpen = false;
        if(novaGroup != null)
            novaGroup.interactable = false;
        windowClosed?.Invoke();
        openWindows.Remove(this);

        if(pauseOnOpen)
            Time.timeScale = 1f;
    }

    private void ToggleWindow(InputAction.CallbackContext obj)
    {
        if (blockWindowHotkeys)
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
}
