using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class OpenWindowButton : MonoBehaviour
{
    [SerializeField]
    private WindowPopup popup;
    private void Start()
    {
        this.GetComponent<Button>().onClick.AddListener(() => OpenWindow());
    }

    private void OpenWindow()
    {
        popup.OpenWindow();
    }
}
