using DG.Tweening;
using HexGame.Resources;
using HexGame.Units;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static HexGame.Resources.ResourceProductionBehavior;

public class SupplyShipBehavior : UnitBehavior
{
    public static event Action<SupplyShipBehavior> supplyShipAdded;
    public static event Action<SupplyShipBehavior> supplyShipRemoved;
    public static event Action<SupplyShipBehavior> supplyShipLaunched;
    public static event Action<SupplyShipBehavior> LoadSold;
    public static event Action<SupplyShipBehavior> LoadBought;

    [SerializeField]
    private SubRequest currentSubRequest;
    private List<ResourceAmount> requestedAmounts = new List<ResourceAmount>();
    private UnitStorageBehavior usb;
    private bool readyToLaunch = false;
    private bool hasWorkers => usb.GetAmountStored(ResourceType.Workers) > 0;
    public bool ReadyToLaunch => readyToLaunch;
    private SupplyShipManager supplyShipManager;

    public static event Action<ResourceAmount, SubRequest> resourceReceived;
    public static event Action<ResourceAmount, SubRequest> resourcePickedUp;
    public event Action<ResourceAmount> fuelReceived;
    [Title("Ship Parts")]
    [SerializeField] private Transform rocket;
    [SerializeField] private Transform frontEngines;
    [SerializeField] private Transform rearEngines;
    [Title("Particles")]
    [SerializeField] List<ParticleSystem> launchParticles;
    [SerializeField] List<ParticleSystem> mainParticles;

    [Title("Launching Settings")]
    [SerializeField, Range(2f,10f)] private float launchTime = 3f;
    [SerializeField, Range(2,10)] private float launchHeight = 3f;
    [SerializeField] private Ease takeOffEase = Ease.InExpo;
    [SerializeField] private Ease landingEase = Ease.OutExpo;
    [SerializeField] private float unloadTime = 15f;

    private float startHeight;
    private int numberOfWorkers = 0;
    private float requiredNumberOfWorkers => this.GetStat(Stat.workers);

    private void Awake()
    {
        usb = this.GetComponent<UnitStorageBehavior>();
        startHeight = rocket.position.y;
        launchParticles.ForEach(p => p.Stop());
        mainParticles.ForEach(p => p.Stop());
        supplyShipManager = FindObjectOfType<SupplyShipManager>();
    }

    private void OnEnable()
    {
        usb.resourceDelivered += CheckTotals;
        usb.resourcePickedUp += CheckLoad;
        rocket.gameObject.SetActive(false);
    }


    private void OnDisable()
    {
        usb.resourceDelivered -= CheckTotals;
        usb.resourcePickedUp -= CheckLoad;
        //trying to take care of quest if ship is destroyed or deleted
        if(requestedAmounts.Count > 0)
            supplyShipManager.ReturnRequest(currentSubRequest);
        DOTween.Kill(this,true);
    }

    public override void StartBehavior()
    {
        isFunctional = true;
        supplyShipAdded?.Invoke(this);
        SupplyShipManager.subRequestAdded += TryGetRequest;
        usb.RequestWorkers();
        DisplayWarning();
    }

    public override void StopBehavior()
    {
        isFunctional = false;
        supplyShipRemoved?.Invoke(this);
        SupplyShipManager.subRequestAdded -= TryGetRequest;
    }

    private void DisplayWarning()
    {
        List<ProductionIssue> issueList = new List<ProductionIssue>();
        if (requiredNumberOfWorkers > numberOfWorkers)
            issueList.Add(ProductionIssue.missingWorkers);
        else if (numberOfWorkers == 0)
            issueList.Add(ProductionIssue.noWorkers);

        if (issueList.Count == 0 && hasWarningIcon)
        {
            warningIconInstance.ToggleIconsOff();
            //warningIconInstance.gameObject.SetActive(false);
            return;
        }

        if (!hasWarningIcon)
        {
            warningIconInstance = UnitManager.warningIcons.PullGameObject(this.transform.position, Quaternion.identity).GetComponent<WarningIcons>();
            warningIconInstance.transform.SetParent(this.transform);
        }

        warningIconInstance.SetWarnings(issueList);
    }

    private void TryGetRequest(SupplyShipManager manager)
    {
        if(!currentSubRequest.isFailed && this.requestedAmounts != null && this.requestedAmounts.Sum(x =>x.amount) > 0)
            return; //we already have a request, don't need another one

        if (!readyToLaunch || !hasWorkers)
            return; 

        supplyShipManager = manager;
        if(manager.TryGetSupplyRequest(out SubRequest request)) 
            PlaceRequest(request);
    }

    private void PlaceRequest(SubRequest subRequest)
    {
        if (!readyToLaunch && !hasWorkers)
            return; //ship is on the ground don't accept new requests

        this.currentSubRequest = subRequest;
        this.requestedAmounts = new List<ResourceAmount>(subRequest.resources);
        subRequest.SetFailedCallback(QuestFailed);

        if(subRequest.buyOrSell == RequestType.buy)
        {
            usb.ClearPickupTypes();
            usb.ClearAllowedTypes();
            foreach (var resource in subRequest.resources)
            {
                usb.AddToAllowedTypes(resource.type);
                usb.AddToPickUpTypes(resource.type);
            }
            DoLaunch();
        }
        else
        {
            usb.ClearPickupTypes();
            usb.ClearAllowedTypes();
            foreach (var resource in subRequest.resources)
            {
                usb.AddToAllowedTypes(resource.type);
                usb.RequestResource(resource);
            }
        }
    }

    private void QuestFailed()
    {
        if (currentSubRequest == null)
            return;

        Debug.Log("Quest Failed");
        CargoManager.RemoveAllRequests(usb);
        usb.RequestPickup(currentSubRequest.resources);
        usb.RemoveRequestForResources(currentSubRequest.resources);
        StartCoroutine(WaitForUnload(currentSubRequest.resources));
    }

    private IEnumerator WaitForUnload(List<ResourceAmount> resources)
    {
        bool hasResources = true;
        while(hasResources)
        {
            yield return new WaitForSeconds(1f);
            foreach (var resource in resources)
            {
                if (usb.GetResourceTotal(resource.type) > 0)
                {
                    yield return new WaitForSeconds(1f);
                    hasResources = true;
                    break;
                }
                hasResources = false;
            }
        }

        if (supplyShipManager.TryGetSupplyRequest(out SubRequest newSubRequest))
            PlaceRequest(newSubRequest);
    }

    public static int GetFuelAmount()
    {
        return 10;
    }

    //each time something is delivered we check if we have all the requested resources
    private void CheckTotals(UnitStorageBehavior behavior, ResourceAmount resource)
    {
        if(resource.type == ResourceType.Workers)
        {
            WorkersReceived(resource);
            return;
        }

        //if true we've canceled the request
        if (currentSubRequest.isFailed)
        {
            usb.RequestPickup(resource);
            return;
        }

        if (currentSubRequest.buyOrSell == RequestType.buy)
            return;

        if (usb.HasAllResources(requestedAmounts))
        {
            readyToLaunch = true;
            requestedAmounts.ForEach(r => resourceReceived?.Invoke(r, currentSubRequest)); //reports delivery
            currentSubRequest.QuestComplete();
            DoLaunch();
        }
    }

    private void WorkersReceived(ResourceAmount resource)
    {
        numberOfWorkers += resource.amount;
        DisplayWarning();
        StartCoroutine(RocketLanding(3f)); //will check for new request when it lands
    }

    private void CheckLoad(UnitStorageBehavior behavior, ResourceAmount amount)
    {
        if(currentSubRequest.isFailed)
            return;

        if (currentSubRequest.buyOrSell == RequestType.sell)
            return;

        resourcePickedUp?.Invoke(amount, currentSubRequest);
        int amountleft = behavior.GetResourceTotal(amount.type);
        //we should only have one type of resource...
        if (amountleft <= 0)
        {
            readyToLaunch = true;
            this.requestedAmounts.Clear();
            CompleteRequest();
        }
    }

    private void DoLaunch()
    {
        if (!readyToLaunch)
            return;

        supplyShipLaunched?.Invoke(this);
        StartCoroutine(RocketLaunch());
        Debug.Log("Launching"); 
    }

    [Button]
    private void TestLaunch()
    {
        StartCoroutine(RocketLaunch());
    }
    private IEnumerator RocketLaunch()
    {
        readyToLaunch = false;
        yield return new WaitForSeconds(2f / GameConstants.GameSpeed);

        launchParticles.ForEach(p => p.Play());
        //play SFX
        yield return new WaitForSeconds(0.5f);

        yield return rocket.DOMoveY(startHeight + launchHeight, launchTime/ GameConstants.GameSpeed).SetEase(takeOffEase).WaitForPosition(launchTime / GameConstants.GameSpeed);

        frontEngines.DOLocalRotate(new Vector3(0f, 90f, 0f), 2f / GameConstants.GameSpeed);
        rearEngines.DOLocalRotate(new Vector3(0f, 90f, 0f), 2f / GameConstants.GameSpeed);
        yield return new WaitForSeconds(0.1f);
        mainParticles.ForEach(p => p.Play());
        float angle = UnityEngine.Random.Range(-90f, 90);
        float nextWaitTime = Mathf.Max(angle / 30f, 2f);//allow engines to fully rotate
        yield return rocket.DOLocalRotate(new Vector3(0f, angle, 0f), angle / 30f).WaitForPosition(nextWaitTime / GameConstants.GameSpeed);
        yield return rocket.DOMove(rocket.transform.position + 100f * rocket.transform.right, launchTime / GameConstants.GameSpeed).SetEase(takeOffEase).WaitForPosition(launchTime / GameConstants.GameSpeed);
        
        //reset rocket
        rocket.transform.localRotation = Quaternion.identity;
        //frontEngines.transform.localRotation = Quaternion.identity;
        //rearEngines.transform.localRotation = Quaternion.identity;
        mainParticles.ForEach(p => p.Stop());
        launchParticles.ForEach(p => p.Stop());

        yield return new WaitForSeconds(unloadTime/(2f * GameConstants.GameSpeed));

        //consume resources
        if(currentSubRequest.buyOrSell == RequestType.sell)
        {
            usb.TryUseAllResources(requestedAmounts);
            requestedAmounts.Clear();
            LoadSold?.Invoke(this);
        }
        else
        {
            usb.DeliverResources(requestedAmounts);
            LoadBought?.Invoke(this);
        }
        yield return new WaitForSeconds(unloadTime/(2f * GameConstants.GameSpeed));

        StartCoroutine(RocketLanding());
    }

    private IEnumerator RocketLanding(float delay = 0f)
    {
        if(delay > 0f)
        {
            rocket.gameObject.SetActive(false);
            yield return new WaitForSeconds(delay / GameConstants.GameSpeed);
        }

        if(!DayNightManager.isDay)
            yield return new WaitUntil(() => DayNightManager.isDay);

        rocket.gameObject.SetActive(true);
        rocket.position = this.transform.position - rocket.transform.right * 100f + Vector3.up * launchHeight;
        launchParticles.ForEach(p => p.Play());
        mainParticles.ForEach(p => p.Play());

        frontEngines.DOLocalRotate(new Vector3(0f, 0f, 0f), 2f / GameConstants.GameSpeed).SetDelay((launchTime -2f) / GameConstants.GameSpeed);
        rearEngines.DOLocalRotate(new Vector3(0f, 0f, 0f), 2f / GameConstants.GameSpeed).SetDelay((launchTime - 2f) / GameConstants.GameSpeed);
        yield return rocket.DOLocalMove(Vector3.zero + Vector3.up * launchHeight, launchTime / GameConstants.GameSpeed).SetEase(landingEase).WaitForPosition(launchTime / GameConstants.GameSpeed);
        mainParticles.ForEach(p => p.Stop());
        yield return rocket.DOMoveY(startHeight, launchTime / GameConstants.GameSpeed).SetEase(landingEase).WaitForPosition(launchTime / GameConstants.GameSpeed);
        
        yield return new WaitForSeconds(0.5f);
        launchParticles.ForEach(p => p.Stop());
        frontEngines.transform.localRotation = Quaternion.identity;
        rearEngines.transform.localRotation = Quaternion.identity;

        if (currentSubRequest == null || currentSubRequest.buyOrSell == RequestType.sell)
            CompleteRequest();
        else
            RequestPickUp();

        //turn off SFX
    }

    private void RequestPickUp()
    {
        usb.RequestPickup(currentSubRequest.resources);
    }

    //we've completed our first request try to get another
    private void CompleteRequest()
    {
        readyToLaunch = true;

        if (supplyShipManager.TryGetSupplyRequest(out SubRequest newSubRequest))
            PlaceRequest(newSubRequest);
    }
}

