using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class OptionsMenu : WindowPopup
{
    private bool openWhenPresed = false;
    private new void OnEnable()
    {
        if (novaGroup == null)
            novaGroup = this.GetComponent<NovaGroup>();

        uiControls = new UIControlActions();
        uiControls.UI.CloseWindow.started += CheckForOpenWindows;
        uiControls.UI.CloseWindow.performed += ToggleWindow;
        uiControls.Enable();
        CloseWindow();
    }

    private void CheckForOpenWindows(InputAction.CallbackContext context)
    {
        openWhenPresed = openWindows.Count != 0;
    }

    private new void OnDisable()
    {
        uiControls.UI.CloseWindow.performed -= ToggleWindow;
        uiControls.UI.CloseWindow.started -= CheckForOpenWindows;
        base.OnDisable();
    }
    
    public override void CloseWindow()
    {
        base.CloseWindow();
    }

    protected void ToggleWindow(InputAction.CallbackContext obj)
    {
        if(instanceIsOpen)
            CloseWindow();
        else if(!openWhenPresed && !isOpen && openWindows.Count == 0)
            OpenWindow();
    }

    public override void OpenWindow()
    {
        if (instanceIsOpen || isOpen)
            return;

        base.OpenWindow();
    }

}
