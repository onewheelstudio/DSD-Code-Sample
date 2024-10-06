using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildIngButton : MonoBehaviour
{
    [SerializeField] private Transform buttonParent;
    private AddUnitButton[] buttonList;

    private void OnEnable()
    {
        buttonList = buttonParent?.GetComponentsInChildren<AddUnitButton>();
    }
}
