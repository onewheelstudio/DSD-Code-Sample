using HexGame.Resources;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceProductionManager : MonoBehaviour
{
    [SerializeField] private List<ResourceProducers> resourceProducers = new List<ResourceProducers>();

    private void OnEnable()
    {
        ResourceProductionBehavior.productionAdded += AddResourceProducer;
        ResourceProductionBehavior.productionRemoved += RemoveResourceProducer;
    }

    private void OnDisable()
    {
        ResourceProductionBehavior.productionAdded -= AddResourceProducer;
        ResourceProductionBehavior.productionRemoved -= RemoveResourceProducer;
    }

    private void Update()
    {
        for (int i = 0; i < resourceProducers.Count; i++)
        {
            DoProduction(resourceProducers[i]);
        }
    }

    private void DoProduction(ResourceProducers producer)
    {
        if (!producer.resourceProductionBehavior.isFunctional)
            return;

        if(producer.resourceProductionBehavior.isProducing && producer.ProductionIsComplete)
        {
            //One last check. May not be needed.
            if (!producer.resourceProductionBehavior.CanIProduce())
                return;

            producer.resourceProductionBehavior.CreateProducts();
        }
        else if(!producer.resourceProductionBehavior.isProducing)
        {
            if(!producer.resourceProductionBehavior.CanProduceAndUpdateStatus())
                return;
            producer.resourceProductionBehavior.StartProduction();
            producer.productionTime = producer.resourceProductionBehavior.GetTimeToProduce();
        }

    }

    private void AddResourceProducer(ResourceProductionBehavior resourceProductionBehavior)
    {
        ResourceProducers newResourceProducer = new ResourceProducers();
        newResourceProducer.resourceProductionBehavior = resourceProductionBehavior;
        resourceProducers.Add(newResourceProducer);
    }

    private void RemoveResourceProducer(ResourceProductionBehavior resourceProductionBehavior)
    {
        ResourceProducers resourceProducerToRemove = resourceProducers.Find(x => x.resourceProductionBehavior == resourceProductionBehavior);
        resourceProducers.Remove(resourceProducerToRemove);
        
        //foreach (var producer in resourceProducers)
        //{
        //    if(producer.resourceProductionBehavior == resourceProductionBehavior)
        //    {
        //        resourceProducers.Remove(producer);
        //        return;
        //    }
        //}
    }
}

[System.Serializable]
internal class ResourceProducers
{
    public ResourceProductionBehavior resourceProductionBehavior;
    public float productionStartTime => resourceProductionBehavior.GetStartTime();
    public float productionTime;
    public bool ProductionIsComplete => productionStartTime + productionTime < Time.time;
}