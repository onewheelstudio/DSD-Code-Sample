using System.Collections;
using UnityEngine;

public class ParticleDeactivate : MonoBehaviour
{
    [SerializeField] private float delay = 2f;

    private void OnEnable()
    {
        StartCoroutine(Deactivate());
    }

    private IEnumerator Deactivate()
    {
        yield return new WaitForSeconds(delay);
        this.gameObject.SetActive(false);
    }

    public void StopTimer()
    {
        this.StopAllCoroutines();
    }
}
