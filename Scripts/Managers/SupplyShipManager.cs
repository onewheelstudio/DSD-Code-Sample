using HexGame.Resources;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SupplyShipManager : MonoBehaviour
{
    private HashSet<SupplyShipBehavior> supplyShips = new HashSet<SupplyShipBehavior>();
    public int SupplyShipCount => supplyShips.Count;
    public static int supplyShipCapacity = 50;
    private Queue<SubRequest> supplySubRequests = new Queue<SubRequest>();
    public static event Action<SupplyShipManager> subRequestAdded;

    private void OnEnable()
    {
        SupplyShipBehavior.supplyShipAdded += AddSupplyShip;
        SupplyShipBehavior.supplyShipRemoved += RemoveSupplyShip;
        //DirectiveSelection.directiveSelected += PlaceRequest;
        DirectiveMenu.QuestAdded += PlaceRequest;
    }

    private void OnDisable()
    {
        SupplyShipBehavior.supplyShipAdded -= AddSupplyShip;
        SupplyShipBehavior.supplyShipRemoved -= RemoveSupplyShip;
        //DirectiveSelection.directiveSelected -= PlaceRequest;
        DirectiveMenu.QuestAdded -= PlaceRequest;
    }

    private void RemoveSupplyShip(SupplyShipBehavior behavior)
    {
        supplyShips.Remove(behavior);
    }

    private void AddSupplyShip(SupplyShipBehavior behavior)
    {
        supplyShips.Add(behavior);
    }

    private void PlaceRequest(DirectiveQuest quest)
    {
        if(quest.TryGetRequestedResources(out List<ResourceAmount> request))
            ProcessSupplyRequest(request, quest);
    }

    [Button]
    public void ProcessSupplyRequest(List<ResourceAmount> request, DirectiveQuest quest)
    {
        float totalRequestSize = request.Sum(x => x.amount);
        int loadsRequired = Mathf.CeilToInt(totalRequestSize / supplyShipCapacity);
        List<ResourceAmount> resourceRequest = new List<ResourceAmount>();

        for (int i = 0; i < loadsRequired; i++)
        {
            int remainingCapacity = supplyShipCapacity - resourceRequest.Sum(x => x.amount);

            while (remainingCapacity > 0 && request.Count > 0)
            {
                if (request[0].amount <= remainingCapacity)
                {
                    resourceRequest.Add(request[0]);
                    request.RemoveAt(0);
                }
                else
                {
                    resourceRequest.Add(new ResourceAmount(request[0].type, remainingCapacity));
                    request[0] = new ResourceAmount(request[0].type, request[0].amount - remainingCapacity);
                }
                remainingCapacity = supplyShipCapacity - resourceRequest.Sum(x => x.amount);
            }

            SubRequest subRquest = new SubRequest(resourceRequest, quest, quest.requestType);
            supplySubRequests.Enqueue(subRquest);
            subRequestAdded?.Invoke(this);
            resourceRequest = new List<ResourceAmount>();
        }
    }

    /// <summary>
    /// Trys to dequeue a supply request. Returns true if successful, false if not.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public bool TryGetSupplyRequest(out SubRequest subRequest)
    {
        while(supplySubRequests.Count > 0)
        {
            SubRequest _subRequest = supplySubRequests.Dequeue();
            if(!_subRequest.isFailed)
            {
                subRequest = _subRequest;
                return true;
            }
        }

        subRequest = null;
        return false;
    }

    public void ReturnRequest(SubRequest subRequest)
    {
        supplySubRequests.Enqueue(subRequest);
    }

    public HashSet<SupplyShipBehavior> GetSupplyShips() => supplyShips;
}

[System.Serializable]
public class SubRequest
{
    public List<ResourceAmount> resources;
    public DirectiveQuest quest;
    public bool isFailed = false;
    public RequestType buyOrSell = RequestType.sell;
    public event Action requestFailed;

    public SubRequest(List<ResourceAmount> resources, DirectiveQuest quest, RequestType buyOrSell)
    {
        this.resources = resources;
        this.quest = quest;
        this.buyOrSell = buyOrSell;
        DirectiveQuest.questFailed += QuestFailed;
        requestFailed = null;
    }

    private void QuestFailed(DirectiveQuest quest)
    {
        if (this.quest == quest)
        {
            this.isFailed = true;
            DirectiveQuest.questFailed -= QuestFailed;
            requestFailed?.Invoke();
            requestFailed = null;
        }
    }

    public void SetFailedCallback(Action callback)
    {
        requestFailed = callback;
    }

    public void QuestComplete()
    {
        DirectiveQuest.questFailed -= QuestFailed;
    }
}

public enum RequestType
{
    sell,
    buy,
}
