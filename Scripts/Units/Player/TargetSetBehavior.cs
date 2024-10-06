using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HexGame.Units;
using UnityEngine.EventSystems;

public class TargetSetBehavior : UnitBehavior, IHavePopUpButtons, IPointerClickHandler
{
    private static CursorManager cursorManager;

    public override void StartBehavior()
    {
        _isFunctional = true;
    }

    public override void StopBehavior()
    {
        _isFunctional = false;
    }
    public List<PopUpPriorityButton> GetPopUpButtons()
    {
        return new List<PopUpPriorityButton>()
            {
                new PopUpPriorityButton("Set Target", () => ToggleCursor(), -1, true),
            };
    }

    private void ToggleCursor()
    {
        if(cursorManager == null)
            cursorManager = GameObject.FindObjectOfType<CursorManager>();

        cursorManager.SetCursor(CursorType.target);

        foreach (var waitForTarget in this.gameObject.GetComponents<IWaitForTarget>())
        {
            waitForTarget.StartListeningForTarget();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ToggleCursor();
    }
}
