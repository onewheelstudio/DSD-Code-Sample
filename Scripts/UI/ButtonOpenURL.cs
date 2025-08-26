using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NovaSamples.UIControls;

[RequireComponent(typeof(Button))]
public class ButtonOpenURL : MonoBehaviour
{
    [SerializeField] private string URL;
    private Button button;

    private void OnEnable()
    {
        button = this.GetComponent<Button>();
        button.Clicked += OpenURL;
    }

    private void OnDisable()
    {
        button.Clicked -= OpenURL;
    }

    private void OpenURL()
    {
        Application.OpenURL(URL);
    }
}
