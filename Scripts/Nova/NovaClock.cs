using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nova;

public class NovaClock : MonoBehaviour
{
    [SerializeField] private UIBlock2D timeSlider;
    [SerializeField] private Color dayColor;
    [SerializeField] private Color nightColor;

    private void OnEnable()
    {
        DayNightManager.percentLeft += UpdateTime;
    }

    public void UpdateTime(float time, bool isDay)
    {
        if (isDay)
        {
            MoveMoon(time);
            timeSlider.Color = dayColor;
        }
        else
        {
            MoveMoon(1 - time);
            timeSlider.Color = nightColor;
        }
    }

    private void MoveMoon(float position)
    {
        timeSlider.Size.X.Percent = position;
    }
}
