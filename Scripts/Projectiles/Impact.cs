using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Impact : MonoBehaviour
{
    [SerializeField]
    private float lifetime = 5f;

    private void OnEnable()
    {
        StartCoroutine(CleanUp());
    }

    IEnumerator CleanUp()
    {
        yield return new WaitForSeconds(lifetime);
        this.gameObject.SetActive(false);
    }
}
