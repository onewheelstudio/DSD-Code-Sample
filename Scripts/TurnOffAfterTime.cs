using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnOffAfterTime : MonoBehaviour
{
    [SerializeField] private float time;

    private void OnEnable()
    {
        StartCoroutine(DoTimer());
    }

    IEnumerator DoTimer()
    {
        yield return new WaitForSeconds(time);
        this.gameObject.SetActive(false);
    }
}
