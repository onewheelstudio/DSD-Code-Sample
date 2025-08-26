using Nova;
using NovaSamples.UIControls;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CheatCodes : MonoBehaviour
{
    public InputActionReference toggleMenu;
    public GameObject buttonPrefab;
    public Transform buttonParent;
    public GameObject window;
    private static CheatCodes instance;

    private void Awake()
    {
        window.SetActive(false);
    }

    private void OnEnable()
    {
        if (toggleMenu != null)
        {
            toggleMenu.action.performed += ToggleMenu;
            toggleMenu.asset.Enable();
        }
    }

    private void OnDisable()
    {
        if (toggleMenu != null)
        {
            toggleMenu.action.performed -= ToggleMenu;
            toggleMenu.asset.Disable();
        }
    }

    public void Close()
    {
        window.SetActive(false);
    }

    private void ToggleMenu(InputAction.CallbackContext context)
    {
        window.SetActive(!window.activeSelf);
    }

    public static void AddButton(Action action, string label)
    {
        if(instance == null)
            instance = FindObjectOfType<CheatCodes>();

        if(instance == null)
            return;

        if (instance.buttonParent == null)
            return;

        GameObject newButton = Instantiate(instance.buttonPrefab, instance.buttonParent);
        newButton.GetComponent<Button>().OnClicked.AddListener(() => action());
        newButton.GetComponentInChildren<TextBlock>().Text = label;
    }
}
