using HexGame.Resources;
using HexGame.Units;
using UnityEngine;
using UnityEngine.InputSystem;

public class UnitStatusBars : MonoBehaviour
{
    private PlayerUnit playerUnit;
    private UnitStorageBehavior usb;
    private UIControlActions uiControls;
    private StatBar statBar;
    private PlayerResources playerResources;

    private void Awake()
    {
        uiControls = new UIControlActions();
        playerUnit = GetComponentInParent<PlayerUnit>();
        usb = GetComponentInParent<UnitStorageBehavior>();
        statBar = this.GetComponentInChildren<StatBar>();
        playerResources = FindObjectOfType<PlayerResources>();
    }

    private void OnEnable()
    {
        uiControls.UI.AltPressed.performed += AltPressed;
        uiControls.UI.AltPressed.canceled += AltReleased;
        uiControls.UI.AltPressed.Enable();
        statBar.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        uiControls.UI.AltPressed.performed -= AltPressed;
        uiControls.UI.AltPressed.canceled -= AltReleased;
        uiControls.UI.AltPressed.Disable();
    }

    private void AltReleased(InputAction.CallbackContext context)
    {
        statBar.gameObject.SetActive(false);
    }

    private void AltPressed(InputAction.CallbackContext context)
    {
        statBar.gameObject.SetActive(true);

        float currentHP = playerUnit.GetHP();
        float maxHP = playerUnit.GetStat(Stat.hitPoints);
        statBar.UpdateStatBar(currentHP / maxHP, 0, ColorManager.GetColor(ColorCode.techCredit));

        float maxStorage = playerUnit.GetStat(Stat.maxStorage);

        if (usb.GetAllowedTypes().Count == 0)
            return;

        int index = 1;
        foreach (var resource in usb.GetStoredResources())
        {
            if(resource.type == ResourceType.Workers)
                continue;
            statBar.UpdateStatBar(resource.amount / maxStorage, index, playerResources.GetResourceTemplate(resource.type).resourceColor);
            index++;
        }
    }
}
