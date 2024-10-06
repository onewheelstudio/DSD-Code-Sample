using NovaSamples.UIControls;
using System;
using UnityEngine;

public class ToggleRangeButton : MonoBehaviour
{
    private Button button;
    [SerializeField] private bool cargoRange;
    [SerializeField] private bool attackRange;
    public static event Action<bool> toggleCargoRange;
    public static event Action<bool> toggleAttackRange;
    private bool isOn = false;

    private void Awake()
    {
        button = this.GetComponent<Button>();


        if (cargoRange)
            button.OnClicked.AddListener(ToggleCargo);
        else if (attackRange)
            button.OnClicked.AddListener(ToggleAttack);
    }

    private void ToggleCargo()
    {
        toggleCargoRange?.Invoke(isOn);
        isOn = !isOn;
    }

    private void ToggleAttack()
    {
        toggleAttackRange?.Invoke(isOn);
        isOn = !isOn;
    }
}
