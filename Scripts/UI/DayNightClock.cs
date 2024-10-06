using Nova;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DayNightClock : MonoBehaviour
{
    [SerializeField] private TextBlock time;
    [SerializeField] private UIBlock2D sun;
    private DayNightManager dayNightManager;

    private void Awake()
    {
        dayNightManager = FindObjectOfType<DayNightManager>();
    }

    // Start is called before the first frame update
    void Start()
    {
        sun.Position.Percent = new Vector3(0f,0f,0f);
        time.Text = "--:--";
        StartCoroutine(UpdateClockText());
        StartCoroutine(UpdateClockText());
    }

    private void OnEnable()
    {
        DayNightManager.toggleDay += StartOfDay;
        DayNightManager.percentLeft += UpdateSunVisuals;
    }

    private void OnDisable()
    {
        DayNightManager.toggleDay -= StartOfDay;
        DayNightManager.percentLeft -= UpdateSunVisuals;
    }

    private void UpdateSunVisuals(float percentLeft, bool isDay)
    {
        if (isDay)
            sun.Position.Percent = new Vector3(GetSunPosition(percentLeft), 0f, 0f);
        else
            sun.Position.Percent = new Vector3(1f, 0, 0);
    }

    private void StartOfDay(int dayNumber = 0)
    {
        //avoids the one second wait in the coroutine
        SetTimeDisplay(DayNightManager.secondRemaining);
    }

    private IEnumerator UpdateClockText()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            if (DayNightManager.isDay)
                SetTimeDisplay(DayNightManager.secondRemaining);
            else
                time.Text = "--:--";
        }
    }

    //write function that converts second as a int to a string in the format of "00:00:00"
    private void SetTimeDisplay(int seconds)
    {
        if(DayNightManager.isDay && seconds == 0)
        {
            time.Text = "";
            return;
        }

        int minutes = Mathf.RoundToInt(seconds / 60);
        seconds = seconds - (minutes * 60);
        time.Text = $"{minutes}:{seconds.ToString("00")}";
    }

    private float GetSunPosition(float percentLeft)
    {
        if(percentLeft <= 0.25f)
        {
            return percentLeft / 0.25f - 1f;
        }
        else if(percentLeft >= 0.75f)
        {
            return percentLeft / 0.25f -3f;
        }
        else
        {
            return 0f;
        }
    }
}
