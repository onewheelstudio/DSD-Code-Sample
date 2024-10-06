using HexGame.Units;
using System;
using UnityEngine;

public class LocationMarker : MonoBehaviour
{
    private Action callback;

    private void OnEnable()
    {
        //do fog stuff?
        DayNightManager.toggleNight += TurnOffGameObject;
    }

    private void OnDisable()
    {
        DayNightManager.toggleNight -= TurnOffGameObject;
    }

    private void TurnOffGameObject(int dayNumber)
    {
        this.gameObject.SetActive(false);
    }

    public void SetCallback(Action callback)
    {
        this.callback = callback;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<PlayerUnit>())
        {
            this.callback?.Invoke();
            this.gameObject.SetActive(false);
        }
    }
}
