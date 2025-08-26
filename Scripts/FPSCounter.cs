using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Nova;

public class FPSCounter : MonoBehaviour
{
    private TextBlock textBlock;
    private float runningTotal;
    [SerializeField]
    private int framesToAverage = 5;

    // Start is called before the first frame update
    void Start()
    {
        textBlock = this.GetComponent<TextBlock>();
        StartCoroutine(FPSAverage());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    IEnumerator FPSAverage()
    {
        while (true)
        {
            for (int i = 0; i < framesToAverage; i++)
            {
                runningTotal += Time.deltaTime;
                yield return null;
            }

            float fps = Mathf.Round(framesToAverage / runningTotal);

            textBlock.Text = $"{fps} fps";
            runningTotal = 0f;
        }
    }
}
