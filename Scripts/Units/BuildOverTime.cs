using DG.Tweening;
using OWS.ObjectPooling;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(UnitIdentifier))]
public class BuildOverTime : MonoBehaviour
{
    [SerializeField]private List<Transform> buildingParts;
    [SerializeField, Range(0, 1),OnValueChanged("UpdateProgress")] private float progress = 0f;
    [SerializeField] private GameObject dust;
    private int activeParts = 0;
    private int numberToActivate = 0;
    private bool isActivating = false;
    public event Action<BuildOverTime> activationComplete;
    protected static ObjectPool<PoolObject> dustPool;

    private void Awake()
    {
        if(dustPool == null && dust != null)
            dustPool = new ObjectPool<PoolObject>(dust);
    }

    [Button]
    private void Refresh()
    {
        buildingParts = this.GetComponentsInChildren<Transform>(true)
                            .Where(x => x.gameObject != this.gameObject)
                            .OrderBy(x => x.position.y)
                            .ToList();
    }

    [Button]
    private void Toggle()
    {
        if (buildingParts.Count == 0)
            return;

        buildingParts.ForEach(x => x.gameObject.SetActive(!x.gameObject.activeSelf));
    }
    private void OnValidate()
    {
        if (buildingParts != null)
            return;

        buildingParts = this.GetComponentsInChildren<Transform>(true)
                            .Where(x => x.gameObject != this.gameObject)
                            .OrderBy(x => x.position.y)
                            .ToList();
    }

    private void OnEnable()
    {
        activeParts = 0;
        foreach (var part in buildingParts)
        {
            part.gameObject.SetActive(false);
        }
        StartCoroutine(ActivateParts());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    public void UpdateProgress(float progress)
    {
        numberToActivate = Mathf.FloorToInt(progress * buildingParts.Count);
    }

    private IEnumerator ActivateParts()
    {
        while(activeParts < buildingParts.Count)
        {
            yield return new WaitUntil(() => activeParts < numberToActivate);

            if(activeParts < numberToActivate)
            {
                buildingParts[activeParts].gameObject.SetActive(true);
                float startScale = buildingParts[activeParts].localScale.x;
                Vector3 position = buildingParts[activeParts].transform.position;
                buildingParts[activeParts].transform.position -= Vector3.up * 2 * position.y;
                buildingParts[activeParts].transform.DOMove(position, 0.25f);
                //buildingParts[activeParts].localScale = Vector3.zero;
                //buildingParts[activeParts].DOScale(startScale, 0.25f);
                if(dust)
                    dustPool.Pull(position, Quaternion.identity);
                activeParts++;
                yield return new WaitForSeconds(0.25f);
            }
        }

        activationComplete?.Invoke(this);
    }

    public void ActivateOverTime()
    {
        StartCoroutine(ActivatePartsOverTime());
    }
    
    private IEnumerator ActivatePartsOverTime()
    {
        for (int i = 0; i < buildingParts.Count; i++)
        {
            buildingParts[i].gameObject.SetActive(true);
            float startScale = buildingParts[i].localScale.x;
            Vector3 position = buildingParts[i].transform.position;
            buildingParts[i].transform.position -= Vector3.up * 2 * position.y;
            buildingParts[i].transform.DOMove(position, 0.25f);
            if (dust)
                dustPool.Pull(position, Quaternion.identity);
            yield return new WaitForSeconds(0.25f);
        }

        activationComplete?.Invoke(this);
    }
}
