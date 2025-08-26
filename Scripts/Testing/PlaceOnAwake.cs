using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HexGame.Units;

public class PlaceOnAwake : MonoBehaviour
{
    private void Awake()
    {
        StartCoroutine(Delay());
    }

    IEnumerator Delay()
    {
        yield return new WaitForSeconds(0.5f);
        Unit unit = this.GetComponent<Unit>();
        unit.Place();
    }
}
