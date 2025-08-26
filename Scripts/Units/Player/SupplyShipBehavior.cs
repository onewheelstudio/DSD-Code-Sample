using DG.Tweening;
using HexGame.Resources;
using HexGame.Units;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using UnityEngine;
using static HexGame.Resources.ResourceProductionBehavior;

public class SupplyShipBehavior : UnitBehavior
{
    public static event Action<SupplyShipBehavior> supplyShipAdded;
    public static event Action<SupplyShipBehavior> supplyShipRemoved;
    public static event Action<SupplyShipBehavior> supplyShipLaunched;
    public static event Action<SupplyShipBehavior, RequestType, List<ResourceAmount>> LoadShipped;
    public static event Action<RequestType> requestComplete;

    [SerializeField]
    private SubRequest currentSubRequest;
    private List<ResourceAmount> requestedAmounts = new List<ResourceAmount>();
    public ShipStorageBehavior SSB => ssb;
    private ShipStorageBehavior ssb;
    private bool readyToLaunch = false;
    private bool hasWorkers => ssb.GetAmountStored(ResourceType.Workers) > 0;
    public bool ReadyToLaunch => readyToLaunch;
    private bool isOnGround = false;
    private SupplyShipManager supplyShipManager;

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

    [Header("Import Bits")]
    [SerializeField] private ResourceType importType;
    private int remainingImportShipments;
    private float startHeight;

    private void Awake()
    {
        ssb = this.GetComponent<ShipStorageBehavior>();
        startHeight = rocket.position.y;
        launchParticles.ForEach(p => p.Stop());
        mainParticles.ForEach(p => p.Stop());
        supplyShipManager = FindFirstObjectByType<SupplyShipManager>();
    }

    private void OnEnable()
    {
        ssb.resourceDelivered += CheckTotals;
        ssb.resourcePickedUp += ResourcePickedUp;
        rocket.gameObject.SetActive(false);

        var awaitable = CheckLoadStatus(this.destroyCancellationToken);
    }

    private void OnDisable()
    {
        ssb.resourceDelivered -= CheckTotals;
        ssb.resourcePickedUp -= ResourcePickedUp;
 
        DOTween.Kill(this,true);
    }

    public override void StartBehavior()
    {
        isFunctional = true;
        supplyShipAdded?.Invoke(this);
        DisplayWarning();
    }

    public override void StopBehavior()
    {
        isFunctional = false;
        supplyShipRemoved?.Invoke(this);
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

        if (warningIconInstance == null)
        {
            warningIconInstance = UnitManager.warningIcons.PullGameObject(this.transform.position, Quaternion.identity).GetComponent<WarningIcons>();
            warningIconInstance.transform.SetParent(this.transform);
        }

        warningIconInstance.SetWarnings(issueList);
    }


    //each time something is delivered we check if we have all the requested resources
    private void CheckTotals(UnitStorageBehavior behavior, ResourceAmount resource)
    {
        if(resource.type == ResourceType.Workers)
        {
            WorkersReceived(resource);
            return;
        }
    }

    private void ResourcePickedUp(UnitStorageBehavior behavior, ResourceAmount amount)
    {
        if (isOnGround && remainingImportShipments > 0 && CanLaunchForImport())
            DoLaunch();
    }

    private void WorkersReceived(ResourceAmount resource)
    {
        numberOfWorkers += resource.amount;
        DisplayWarning();
        if (SaveLoadManager.Loading)
        { 
            rocket.gameObject.SetActive(true);
            mainParticles.ForEach(p => p.Stop());
            CompleteRequest();// attempts to get new request
        }
        else
            StartCoroutine(RocketLanding(3f)); //will check for new request when it lands
    }

    public void ImportResource(ResourceType resource, int numShipments)
    {
        this.importType = resource;
        this.remainingImportShipments = numShipments;

        //make sure we can handle imported resource
        this.ssb.AddAllowedResource(resource);

        if (CanLaunchForImport())
            DoLaunch();
    }

    private bool CanLaunchForImport()
    {
        var storedResouces = ssb.GetStoredResources();
        var hasResources = false;
        for (int i = 0; i < storedResouces.Count; i++)
        {
            if (storedResouces[i].type != ResourceType.Workers && storedResouces[i].amount > 0)
            {
                hasResources = true;
                break;
            }
        }

        return !hasResources;
    }

    public void DoLaunch()
    {
        if (!readyToLaunch)
            return;

        supplyShipLaunched?.Invoke(this);
        StartCoroutine(RocketLaunch());
    }
    
    private IEnumerator RocketLaunch()
    {
        if(!DayNightManager.isDay)
            yield return new WaitUntil(() => DayNightManager.isDay);

        isOnGround = false;
        readyToLaunch = false;
        yield return new WaitForSeconds(2f / GameConstants.GameSpeed);

        launchParticles.ForEach(p => p.Play());
        //play SFX
        yield return new WaitForSeconds(0.5f);

        rocket.DOMoveY(startHeight + launchHeight, launchTime / GameConstants.GameSpeed).SetEase(takeOffEase);
        yield return new WaitForSeconds(launchTime / GameConstants.GameSpeed);

        frontEngines.DOLocalRotate(new Vector3(0f, 90f, 0f), 2f / GameConstants.GameSpeed);
        rearEngines.DOLocalRotate(new Vector3(0f, 90f, 0f), 2f / GameConstants.GameSpeed);
        yield return new WaitForSeconds(0.1f);
        mainParticles.ForEach(p => p.Play());
        float angle = UnityEngine.Random.Range(-90f, 90);
        float nextWaitTime = Mathf.Max(angle / 30f, 2f);//allow engines to fully rotate
        rocket.DOLocalRotate(new Vector3(0f, angle, 0f), angle / 30f);
        yield return new WaitForSeconds(nextWaitTime / GameConstants.GameSpeed);
        rocket.DOMove(rocket.transform.position + 100f * rocket.transform.right, launchTime / GameConstants.GameSpeed).SetEase(takeOffEase);
        yield return new WaitForSeconds(launchTime / GameConstants.GameSpeed);
        
        //reset rocket
        rocket.transform.localRotation = Quaternion.identity;
        mainParticles.ForEach(p => p.Stop());
        launchParticles.ForEach(p => p.Stop());

        yield return new WaitForSeconds(unloadTime/(2f * GameConstants.GameSpeed));

        //consume resources
        var storedResouces = ssb.GetStoredResources();
        if(storedResouces.Any(r => r.type != ResourceType.Workers && r.amount > 0))
            LoadShipped?.Invoke(this, RequestType.sell, storedResouces.Where(r => r.type != ResourceType.Workers).ToList());
        for(int i = 0; i < storedResouces.Count; i++)
        {
            if(storedResouces[i].type != ResourceType.Workers && storedResouces[i].amount > 0)
            {
                ssb.SellResource(storedResouces[i]);
            }
        }
        
        yield return new WaitForSeconds(unloadTime/(2f * GameConstants.GameSpeed));
        
        if (remainingImportShipments > 0)
        {
            DoImport(importType);
            yield return new WaitForSeconds(unloadTime/(2f * GameConstants.GameSpeed));
        }

        StartCoroutine(RocketLanding());
    }

    private void DoImport(ResourceType importType)
    {
        ResourceAmount import = new ResourceAmount(importType, SupplyShipManager.supplyShipCapacity);
        ssb.BuyResource(import);
        LoadShipped?.Invoke(this, RequestType.buy, new List<ResourceAmount>() {import});
        remainingImportShipments = Mathf.Max(0, remainingImportShipments - 1);
    }

    private IEnumerator RocketLanding(float delay = 0f)
    {
        if (!DayNightManager.isDay)
            yield return new WaitUntil(() => DayNightManager.isDay);

        if (delay > 0f)
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
        rocket.DOLocalMove(Vector3.zero + Vector3.up * launchHeight, launchTime / GameConstants.GameSpeed).SetEase(landingEase);
        yield return new WaitForSeconds(launchTime / GameConstants.GameSpeed);

        mainParticles.ForEach(p => p.Stop());
        rocket.DOMoveY(startHeight, launchTime / GameConstants.GameSpeed).SetEase(landingEase);
        yield return new WaitForSeconds(launchTime / GameConstants.GameSpeed);
        
        yield return new WaitForSeconds(0.5f);
        launchParticles.ForEach(p => p.Stop());
        frontEngines.transform.localRotation = Quaternion.identity;
        rearEngines.transform.localRotation = Quaternion.identity;

        if (currentSubRequest == null || currentSubRequest.buyOrSell == RequestType.sell)
        {
            CompleteRequest();
            requestComplete?.Invoke(currentSubRequest.buyOrSell);
        }    
        else
            RequestPickUp();

        isOnGround = true;
        //turn off SFX
    }

    private void RequestPickUp()
    {
        ssb.RequestPickup(currentSubRequest.resources);
    }

    //we've completed our first request try to get another
    private void CompleteRequest()
    {
        readyToLaunch = true;
    }

    private async Awaitable CheckLoadStatus(CancellationToken destroyCancellationToken)
    {
        while(!destroyCancellationToken.IsCancellationRequested)
        {
            await Awaitable.WaitForSecondsAsync(5f, destroyCancellationToken);

            if (currentSubRequest.buyOrSell == RequestType.buy)
                continue;

            if (!isOnGround)
                continue;

            if (ssb.HasAllResources(requestedAmounts) && requestedAmounts.Count > 0)
            {
                readyToLaunch = true;
                //requestedAmounts.ForEach(r => resourceSold?.Invoke(r, currentSubRequest)); //reports delivery
                currentSubRequest.QuestComplete();
                DoLaunch();
            }
        }
    }
}

