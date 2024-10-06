using UnityEngine;

public class SpawnIndicator : MonoBehaviour
{
    private void OnEnable()
    {
        DayNightManager.toggleDay += ToggleOff;
    }

    private void OnDisable()
    {
        DayNightManager.toggleDay -= ToggleOff;
    }

    private void ToggleOff(int dayNumber)
    {
        this.gameObject.SetActive(false);
    }
}
