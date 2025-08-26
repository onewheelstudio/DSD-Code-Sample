using HexGame.Resources;
using HexGame.Units;
using System.Collections;
using UnityEngine;

public class SentryTowerBehavior : UnitBehavior
{
    private FogRevealer fogRevealer;
    private ResourceProductionBehavior resourceProductionBehavior;

    private void Awake()
    {
        fogRevealer = GetComponentInChildren<FogRevealer>(true);
        resourceProductionBehavior = GetComponent<ResourceProductionBehavior>();
        this.GetComponent<UnitStorageBehavior>().AddDeliverType(ResourceType.Energy);

    }

    public override void StartBehavior()
    {
        isFunctional = true;
        StartCoroutine(ScanForEnemies());
    }

    public override void StopBehavior()
    {
        isFunctional = false;
        StopAllCoroutines();
        fogRevealer.gameObject.SetActive(false);
    }

    private IEnumerator ScanForEnemies()
    {
        while(isFunctional)
        {
            yield return new WaitForSeconds(1f);
            if(resourceProductionBehavior.hasWarningIcon && fogRevealer.gameObject.activeSelf)
                fogRevealer.gameObject.SetActive(false);
            else if(!resourceProductionBehavior.hasWarningIcon && !fogRevealer.gameObject.activeSelf)
                fogRevealer.gameObject.SetActive(true);
        }
    }
}
