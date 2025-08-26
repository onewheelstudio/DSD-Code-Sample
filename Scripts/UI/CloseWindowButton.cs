using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NovaSamples.UIControls;

public class CloseWindowButton : MonoBehaviour
{
    private WindowPopup popup;
    private void OnEnable()
    {
        this.GetComponent<Button>().Clicked += CloseWindow;
        popup = GetComponentInParent<WindowPopup>();
    }

    private void OnDisable()
    {
        this.GetComponent<Button>().Clicked -= CloseWindow;
    }

    private void CloseWindow()
    {
        popup.CloseWindow();
    }
}
