using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HexGame.Units;
using UnityEngine.InputSystem;
using System;

public class TerraFormingManager : MonoBehaviour
{
    private static TerraFormingWindow terraFormingWindow;
    private static TerraformerBehavior currentTerraFormer;
    private static UnitStorageBehavior currentUSB;
    private UIControlActions currentUIActions;

    private void Awake()
    {
        currentUIActions = new UIControlActions();
    }

    private void OnDisable()
    {
    }

    private void OnEnable()
    {
        terraFormingWindow = FindObjectOfType<TerraFormingWindow>();
    }

    public static void ToggleTerraForming(TerraformerBehavior terraformer)
    {
        terraFormingWindow.OpenWindow();
        currentTerraFormer = terraformer;
        currentUSB = terraformer.GetComponent<UnitStorageBehavior>();
    }

    private void DoTerraForm(InputAction.CallbackContext obj)
    {
        if (!terraFormingWindow.instanceIsOpen)
            return;

        if (!IsTerraFormClose(currentTerraFormer))
            return;

        Debug.Log("Doing terraform.");
    }

    private bool IsTerraFormClose(TerraformerBehavior terraformerBehavior)
    {
        if (terraformerBehavior == null)
            return false;

        if ((terraformerBehavior.transform.position - HelperFunctions.GetMouseHex3OnPlane().ToVector3()).magnitude > terraformerBehavior.GetRange())
            return false;
        else
            return true;
    }
}
