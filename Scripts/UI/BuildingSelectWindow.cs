using Nova;
using System;
using UnityEngine;

public class BuildingSelectWindow : WindowPopup
{
    public static Action<BuildingSelectWindow> buildingSelectOpened;
    public static bool _typeIsOpen = false;

    [SerializeField] protected UIBlock button;
    protected Interactable interactable;
    private int buttonSize = 45;
    private int buttonPosition = 0;

    private void Awake()
    {
        interactable = this.button.GetComponent<Interactable>();
    }

    public override void OnEnable()
    {
        base.OnEnable();
        base.CloseWindow();
        BuildingSelectWindow.buildingSelectOpened += AllowOnlyOne;
        //novaGroup.interactable = false;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        BuildingSelectWindow.buildingSelectOpened -= AllowOnlyOne;
    }

    private void AllowOnlyOne(BuildingSelectWindow window)
    {
        if (window != this && instanceIsOpen)
        {
            DoButtonDown();
            base.CloseWindow();
        }
    }

    public override void OpenWindow()
    {
        if(!interactable.enabled)
            return;

        if (BlockWindowHotkeys)
            return;

        buildingSelectOpened?.Invoke(this);
        DoButtonUp();
        base.OpenWindow();
    }

    public override void CloseWindow()
    {
        //sending null prevents the window from reopening on hover
        buildingSelectOpened?.Invoke(null);
        DoButtonDown();
        base.CloseWindow();
    }

    internal void HideWindow()
    {
        buildingSelectOpened?.Invoke(this);
        DoButtonDown();
        base.CloseWindow();
    }

    private void DoButtonUp()
    {
        this.button.Size.Value = new Vector2(buttonSize + 5, buttonSize + 5);
        this.button.Position.Y.Value = 15;
    }

    private void DoButtonDown()
    {
        this.button.Size.Value = new Vector2(buttonSize, buttonSize);
        this.button.Position.Y.Value = buttonPosition;
    }
}
