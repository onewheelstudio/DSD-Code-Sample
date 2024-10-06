using System.Collections.Generic;
using UnityEngine;
using HexGame.Resources;
using Sirenix.OdinInspector;
using System;
using DG.Tweening;

namespace HexGame.Units
{
    [RequireComponent(typeof(UnitStorageBehavior))]
    public class BuildingSpotBehavior : UnitBehavior, IHavePopupInfo
    {
        [SerializeField]
        public PlayerUnitType unitTypeToBuild { get; private set; }
        [SerializeField]
        private List<ResourceAmount> neededResources = new List<ResourceAmount>();
        [SerializeField]
        private GameObject buildingPrefab;
        [SerializeField]
        private GameObject placeHolderPrefab;
        private UnitStorageBehavior usb;
        private GameObject buildingInstance;

        [Header("Placement Materials")]
        [SerializeField]
        private Material goodMaterial;
        [SerializeField]
        private Material badMaterial;
        private MeshRenderer[] meshRenders;
        [SerializeField] private Color progressColor;
        private StatBar progressBar;
        private BuildOverTime buildOverTime;
        private Stats stats;
        protected static UnitManager unitManager;
        private static Camera mainCamera;
        private FogRevealer fogRevealer;
        private EnemyCrystalManager ecm;

        public static event Action<BuildingSpotBehavior, GameObject> buildingComplete;
        public static event Action<ResourceAmount> resourceConsumed;

        private void Awake()
        {
            if(unitManager == null)
                unitManager = FindObjectOfType<UnitManager>();

            if(mainCamera == null)
                mainCamera = Camera.main;

            if (fogRevealer == null)
                fogRevealer = GetComponentInChildren<FogRevealer>(true);

            if(ecm == null)
                ecm = FindObjectOfType<EnemyCrystalManager>();
        }

        private void OnEnable()
        {
            progressBar = this.GetComponentInChildren<StatBar>();
            buildOverTime = this.GetComponentInChildren<BuildOverTime>();
        }

        protected void OnValidate()
        {
            UpdateAllowedTypes();
        }

        public override void StartBehavior()
        {
            if (neededResources.Count == 0 && unitTypeToBuild != PlayerUnitType.hq)
            {
                BuildingDone();
                return;
            }

            _isFunctional = true;

            fogRevealer.gameObject.SetActive(true);

            if (usb == null)
                usb = GetComponent<UnitStorageBehavior>();
            usb.SetRequestPriority(CargoManager.RequestPriority.medium);
            usb.resourceDelivered += AreAllResoucesDelivered;
            usb.resourceDelivered += UpdateProgress;

            LandingPad lp = this.GetComponentInChildren<LandingPad>();
            if (lp)
                usb.SetLandingPosition(lp.transform);

            usb.ClearAllowedTypes();

            foreach (var resource in neededResources)
            {
                usb.AddToAllowedTypes(resource.type);
                usb.MakeDeliveryRequest(resource, true);
            }

            progressBar.Enable();
            progressBar.ResetAll();

            Sequence sequence = DOTween.Sequence();
            sequence.Append(mainCamera.DOFieldOfView(mainCamera.fieldOfView - 0.04f, 0.05f).SetEase(Ease.Linear));
            sequence.Append(mainCamera.DOFieldOfView(mainCamera.fieldOfView + 0.04f, 0.05f).SetEase(Ease.Linear));
            SFXManager.PlaySFX(SFXType.buildingPlace);

            if (unitTypeToBuild == PlayerUnitType.hq)
            {
                buildOverTime = unitManager.GetBuildOverTimeByType(unitTypeToBuild);
                buildingInstance.gameObject.SetActive(false);
                buildOverTime.transform.SetParent(this.transform);
                buildOverTime.transform.SetLocalPositionAndRotation(Vector3.zero, buildingInstance.transform.localRotation);
                buildOverTime.activationComplete += BuildingDone;
                buildOverTime.ActivateOverTime();
            }
        }

        private void UpdateProgress(UnitStorageBehavior usb, ResourceAmount res)
        {
            float percentComplete = PercentComplete();
            progressBar.UpdateStatBar(percentComplete, 0, progressColor);
            
            if(buildOverTime == null && percentComplete > 0f)
            {
                buildOverTime = unitManager.GetBuildOverTimeByType(unitTypeToBuild);
                if (buildOverTime != null && buildingInstance.activeInHierarchy)
                {
                    buildingInstance.gameObject.SetActive(false);
                    buildOverTime.transform.SetParent(this.transform);
                    buildOverTime.transform.SetLocalPositionAndRotation(Vector3.zero, buildingInstance.transform.localRotation);
                    buildOverTime?.UpdateProgress(percentComplete);
                    buildOverTime.activationComplete += BuildingDone;
                }
            }
            else
                buildOverTime?.UpdateProgress(percentComplete);
        }

        public override void StopBehavior()
        {
            if (usb == null)
                usb = GetComponent<UnitStorageBehavior>();

            usb.resourceDelivered -= AreAllResoucesDelivered;
            usb.resourceDelivered -= UpdateProgress;
            _isFunctional = false;
            fogRevealer.gameObject.SetActive(false);
        }

        private void AreAllResoucesDelivered(UnitStorageBehavior usb, ResourceAmount resource)
        {
            resourceConsumed?.Invoke(resource);

            foreach (ResourceAmount r in neededResources)
            {
                if (!usb.HasResource(r))
                    return;
            }

            if(buildOverTime == null)
                BuildingDone();
        }

        private void BuildingDone(BuildOverTime bto)
        {
            bto.activationComplete -= BuildingDone;
            BuildingDone();
        }

        private void BuildingDone()
        {
            buildingComplete?.Invoke(this, buildingPrefab);
            StopBehavior();
            this.gameObject.SetActive(false);
        }

        public void SetTypeToBuild(PlayerUnitType unitType, GameObject building, GameObject placeHolder, List<ResourceAmount> costs)
        {
            this.buildingPrefab = building;
            this.placeHolderPrefab = placeHolder;
            this.stats = building.GetComponent<PlayerUnit>().GetStats();
            neededResources = costs;
            unitTypeToBuild = unitType;
            CreatePlaceHolder();
        }

        private void CreatePlaceHolder()
        {
            if (buildingInstance != null)
                Destroy(buildingInstance);
            if (buildOverTime != null)
                Destroy(buildOverTime.gameObject);
            buildingInstance = Instantiate(placeHolderPrefab);
            buildingInstance.transform.SetParent(this.transform);
            buildingInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            meshRenders = buildingInstance.GetComponentsInChildren<MeshRenderer>();
        }

        //called from the unit manager
        public void CheckIfLocationValid()
        {
            if (IsValidPlacement())
                SetPlaceHolderMaterial(goodMaterial);
            else
                SetPlaceHolderMaterial(badMaterial);
        }

        private void SetPlaceHolderMaterial(Material material)
        {
            foreach (var mr in meshRenders)
                mr.material = material;
        }

        public bool IsValidPlacement()
        {
            HexTile tile = HexTileManager.GetHexTileAtLocation(this.transform.position);
            if (tile == null)
                return false;

            if (tile.isPlaceHolder)
                return false;

            if (ecm.IsCrystalNearBy(this.transform.position, out EnemyCrystalBehavior crystal))
                return false;

            if (!stats.placementList.Contains(tile.TileType) || !stats.CanPlaceAtLocation(this.transform.position))
            {
                //if(lastTileType != tile.tileType)
                //    MessagePanel.ShowMessage($"Can not place {unitTypeToBuild} on {tile.tileType}.", this.gameObject);
                return false;
            }

            if (tile.TryGetComponent(out FogGroundTile fgt) && !fgt.HasBeenRevealed)
                return false;

            return true;
        }

        [Button]
        private float PercentComplete()
        {
            if (usb == null)
                usb = GetComponent<UnitStorageBehavior>();

            float totalStored = usb.TotalStored();
            float totalNeeded = 0f;

            foreach (var resource in neededResources)
            {
                totalNeeded += resource.amount;
            }

            if (Mathf.RoundToInt(totalNeeded) == 0)
                return 1f;

            return totalStored / totalNeeded;
        }

        public List<PopUpInfo> GetPopupInfo()
        {
            List<PopUpInfo> popUpInfos = new List<PopUpInfo>();
            popUpInfos.Add(new PopUpInfo($"Constructing...", 1000, PopUpInfo.PopUpInfoType.name, (int)unitTypeToBuild));
            popUpInfos.Add(new PopUpInfo($"{Mathf.RoundToInt(PercentComplete() * 100f)}% Complete", 0, PopUpInfo.PopUpInfoType.stats));

            return popUpInfos;
        }
        private void UpdateAllowedTypes()
        {
            if (usb == null)
                usb = GetComponent<UnitStorageBehavior>();

            List<ResourceType> allowedTypes = new List<ResourceType>();
            foreach (var resource in neededResources)
            {
                allowedTypes.Add(resource.type);
            }

            usb.SetAllowedTypes(allowedTypes);
        }
    }
}
