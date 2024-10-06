using DG.Tweening;
using HexGame.Grid;
using Nova;
using NovaSamples.UIControls;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HexTechTree : WindowPopup
{
    [Range(1,5f)]
    public float spread = 1f;
    public static int scale = 73;
    private const float SQRT3 = 1.73205080757f;

    private float radius;
    private Vector2 UISize;

    public static event Action<bool> techTreeOpen;
    public static event Action techCreditChanged;
    public static event Action<int> techCreditEarned;
    public static event Action firstTechCreditCollected;

    [SerializeField] private List<Color> upgradeColors = new List<Color>();

    [SerializeField]
    private static Dictionary<Hex3, UpgradeTile> upgradeTiles = new Dictionary<Hex3, UpgradeTile>();
    private List<HashSet<Hex3>> tempUpgradeTierList = new List<HashSet<Hex3>>();
    [SerializeField]
    private List<Upgrade> upgradeList = new List<Upgrade>();
    [SerializeField]
    private GameObject UIUpgradeTilePrefab;
    [SerializeField]
    private Transform background;

    [SerializeField]
    private StatsInfo statInfo;
    private Vector2 canvasResolution;
    private float canvasScale;

    [Header("Shaping")]
    [SerializeField] private int maxNeighbors = 4;

    [Header("Rep Info")]
    [SerializeField] private TextBlock repText;
    [SerializeField] private TextBlock techCreditText;

    [Header("Button Stuff")]
    [SerializeField] private Button openButton;
    private Animator openButtonAnimator;

    private static int totalTechCreditsCollected = 0;
    private static int totalTechCreditsSpent = 0;
    public static int TotalTechCreditsCollected => totalTechCreditsCollected;
    private static int techCredits = 0;
    public static int TechCredits => techCredits;
    public static int FirstTechCreditOnDay;

    public float zoomScale => background.transform.localScale.x;
    private bool techTreeUnlocked = false;

    [SerializeField] private Camera overlayCamera;
    [SerializeField] private Camera techTreeCamera;

    private void Awake()
    {
        upgradeTiles = new Dictionary<Hex3, UpgradeTile>();
        openButtonAnimator = openButton.GetComponent<Animator>();
        canvasResolution = GameObject.FindObjectOfType<Nova.ScreenSpace>().ReferenceResolution;
        canvasScale = Screen.width / (float)canvasResolution.x;
        techCredits = ES3.Load<int>(GameConstants.techCredits, GameConstants.StatsPath, 0);
        totalTechCreditsCollected = ES3.Load<int>(GameConstants.totalTechCreditsCollected, GameConstants.StatsPath, 0);
        CheatCodes.AddButton(() => ChangeTechCredits(1000), "Add Tech Credit");
        CheatCodes.AddButton(ClearTechCredits, "Clear Tech Credits");
        CheatCodes.AddButton(() => ReputationManager.ChangeReputation(100), "Reputation +100");
        CheatCodes.AddButton(() => ReputationManager.LoseReputation(100), "Rep Penality -200");

        //this should be removed for the demo...?
        ClearTechCredits();
        FirstTechCreditOnDay = -1;

        radius =  spread * UIUpgradeTilePrefab.GetComponent<UIBlock>().Size.Value.y / (2f * canvasResolution.y * canvasScale);
        UISize = background.GetComponent<UIBlock>().Size.XY.Value;
#if UNITY_EDITOR
        GetUpgrades();
#endif
        CreateTree();
    }


    [Button]
    private void ReGenerate()
    {
        for(int i = 0; upgradeTiles.Keys.Count > 0; i++)
        {
            Hex3 key = upgradeTiles.Keys.ElementAt(0);
            Destroy(upgradeTiles[key].gameObject);
            upgradeTiles.Remove(key);
        }

        upgradeTiles = new Dictionary<Hex3, UpgradeTile>();
        tempUpgradeTierList = new List<HashSet<Hex3>>();

#if UNITY_EDITOR
        GetUpgrades();
#endif
        CreateTree();
    }

    private new void OnEnable()
    {
        if (clipMask == null)
            clipMask = this.GetComponent<ClipMask>();

        base.OnEnable();

        DirectiveQuest.questCompleted += UpdateRepInfo;
        DirectiveQuest.questFailed += UpdateRepInfo;
        techCreditChanged += UpdateRepInfo;
        LockTechTree.lockTechTree += ButtonOff;
        UnLockTechTree.unLockTechTree += ButtonOn;
        firstTechCreditCollected += ButtonOn;
        UpgradeTile.upgradeStatusChange += UpgradeStatusChanged;

        interactableControl = new InteractableControl(this.transform);
        CloseWindow();
    }

    private new void OnDisable()
    {
        base.OnDisable();
        UpgradeTile.upgradeStatusChange -= UpgradeStatusChanged;
        DirectiveQuest.questCompleted -= UpdateRepInfo;
        DirectiveQuest.questFailed -= UpdateRepInfo;
        techCreditChanged -= UpdateRepInfo;
        LockTechTree.lockTechTree -= ButtonOff;
        UnLockTechTree.unLockTechTree -= ButtonOn;
        firstTechCreditCollected -= ButtonOn;
        ES3.Save<int>(GameConstants.techCredits, techCredits, GameConstants.StatsPath);
        ES3.Save<int>(GameConstants.totalTechCreditsCollected, totalTechCreditsCollected, GameConstants.StatsPath);
        DOTween.Kill(this,true);
        Debug.Log("Tech Tree Disabling", this.gameObject);
    }

    private void UpgradeStatusChanged(UpgradeTile tile, Upgrade.UpgradeStatus status)
    {
        if (status == Upgrade.UpgradeStatus.purchased)
            UnlockNeighbors(tile.hexPosition);
    }

    [Button]
    private void GetUpgrades()
    {
        upgradeList.Clear();
        upgradeList = HelperFunctions.GetScriptableObjects<Upgrade>("Assets/Prefabs/Upgrades");
    }

    public bool CanUnlock(Hex3 upgradeLocation)
    {
        foreach (var neighbor in Hex3.GetNeighborLocations(upgradeLocation))
        {
            if (upgradeTiles.TryGetValue(neighbor, out UpgradeTile uiHexTile) && uiHexTile.IsPurchased())
            {
                return true;
            }
        }

        return false;
    }

    public void UnlockNeighbors(Hex3 location)
    {
        foreach (var neighbor in Hex3.GetNeighborLocations(location))
        {
            if (upgradeTiles.TryGetValue(neighbor, out UpgradeTile uiHexTile) 
                && uiHexTile.status != Upgrade.UpgradeStatus.purchased)
            {
                uiHexTile.SetStatus(Upgrade.UpgradeStatus.unlocked);
            }
        }
    }

    [Button]
    private void GetUpgradeCount()
    {
        Debug.Log($" Upgrades {upgradeTiles.Count}");
    }

    [Button]
    private void CreateTree()
    {
        for (int i = -1; i < 10; i++)
        {
            CreateTreeTier(i);
        }

        UnLockUpgrades();
    }

    private void UnLockUpgrades()
    {
        List<UpgradeTile> upgradeTiles = HexTechTree.upgradeTiles.Values.ToList();
        upgradeTiles = upgradeTiles.Where(u => u.upgrade.unlockedAtStart == true).ToList();

        foreach (var upgrade in upgradeTiles)
        {
            upgrade.SetStatus(Upgrade.UpgradeStatus.purchased);
            UnlockNeighbors(upgrade.hexPosition);
        } 
    }

    private void CreateTreeTier(int i)
    {
        List<Upgrade> upgrades = new List<Upgrade>();
        upgrades = upgradeList.Where(u => u.upgradeTier == i)
                              .Where(u => u != null)
                              .Select(u => u)
                              .ToList();
        upgrades = upgrades.OrderByDescending(u => u.unlockedAtStart == true)
                           .ThenBy(u => u.subTier)
                           .ToList();

        foreach (var upgrade in upgrades)
        {
            if (!upgrade.showInTechTree)
                continue;

            AddUpgradeTile(GetLocation(i), upgrade);
        }
    }

    private Hex3 GetLocation(int tier)
    {
        if (upgradeTiles.Count == 0)
            return new Hex3();
        else
        {
            HashSet<Hex3> locations = new HashSet<Hex3>();

            if(tier == 0)
            {
                locations.Add(Hex3.Zero);
            }
            else if (tier >= 1)
            {
                locations.UnionWith(tempUpgradeTierList[tier -1]);
                if(tempUpgradeTierList.Count > tier)
                    locations.UnionWith(tempUpgradeTierList[tier]);
            }
            else
                locations.UnionWith(tempUpgradeTierList[0]);

            locations = GetEmptyNeighborLocations(locations);
            locations = FilterByNumberOfNeighbors(locations, tier);
            locations = FilterLocationsByTier(locations, tier);
            if (tier == 1)
                locations.ExceptWith(Hex3.GetNeighborLocations(Hex3.Zero));

            if (locations == null || locations.Count == 0)
                return new Hex3();

            locations.ExceptWith(upgradeTiles.Keys);


            if(locations.Count == 0)
            {
                Debug.Log("No locations found");
                return new Hex3();
            }

            return FinalSort(locations, tier);
        }
    }

    private HashSet<Hex3> FilterByNumberOfNeighbors(HashSet<Hex3> locations, int tier)
    {
        for (int i = locations.Count - 1; i >= 0; i--)
        {
            if (GetFullNeighborLocations(locations.ElementAt(i)).Count >= MaxNeighbors(tier))
                locations.Remove(locations.ElementAt(i));
        }

        return locations;
    }

    private int MaxNeighbors(int tier)
    {
        if (tier == 0)
            return 3;
        else
            return maxNeighbors;
    }

    private Hex3 FinalSort(HashSet<Hex3> locations, int tier)
    {
        if(tier <= 1)
            return locations.ElementAt(HexTileManager.GetNextInt(0, locations.Count));

        while (locations.Count > 0)
        {
            Hex3 location = locations.ElementAt(HexTileManager.GetNextInt(0, locations.Count));
            List<Hex3> neighbors = Hex3.GetNeighborLocations(location);
            bool isGood = true;
            for (int i = 0; i < tier - 1; i++)
            {
                HashSet<Hex3> tempHasHset = new HashSet<Hex3>(tempUpgradeTierList[i]);
                tempHasHset.IntersectWith(neighbors);
                if (tempHasHset.Count > 0)
                {
                    locations.Remove(location);
                    isGood = false;
                    break;
                }
            }

            if (isGood)
                return location;
        }

        return new Hex3();
    }

    private HashSet<Hex3> FilterLocationsByTier(HashSet<Hex3> locations, int tier)
    {
        if (tier <= 1)
            return locations; // no need to filter;

        for (int i = 0; i < tier - 1; i++)
        {
            locations.ExceptWith(tempUpgradeTierList[i]); //remove locations that have a tier 2 lower than current upgrade
        }

        return locations;
    }

    private HashSet<Hex3> GetEmptyNeighborLocations(HashSet<Hex3> locations)
    {
        HashSet<Hex3> emptyNeighbors = new HashSet<Hex3>();
        foreach (var location in locations)
        {
            foreach (var neighbor in Hex3.GetNeighborLocations(location))
                emptyNeighbors.Add(neighbor); //hashset don't need to check contains
        }

        emptyNeighbors.ExceptWith(upgradeTiles.Keys); //remove already placed tile locations

        return emptyNeighbors;
    }

    private List<Hex3> GetEmptyNeighborLocations(Hex3 location)
    {
        List<Hex3> emptyNeighbors = new List<Hex3>();
        foreach (var neighbor in Hex3.GetNeighborLocations(location))
        {
            if (!upgradeTiles.ContainsKey(neighbor))
                emptyNeighbors.Add(neighbor);
        }

        return emptyNeighbors;
    }

    private List<Hex3> GetFullNeighborLocations(Hex3 location)
    {
        List<Hex3> emptyNeighbors = new List<Hex3>();
        foreach (var neighbor in Hex3.GetNeighborLocations(location))
        {
            if (upgradeTiles.ContainsKey(neighbor))
                emptyNeighbors.Add(neighbor);
        }

        return emptyNeighbors;
    }

    private void AddUpgradeTile(Hex3 location, Upgrade upgrade)
    {
        if(upgradeTiles.ContainsKey(location))
        {
            Debug.LogWarning($"Upgrade already in that position. Tier: {upgrade.upgradeTier} Location: {location}");
            return;
        }

        GameObject newUpgrade = Instantiate(UIUpgradeTilePrefab);
        //newUpgrade.transform.SetParent(this.transform);
        newUpgrade.transform.SetParent(background);
        newUpgrade.transform.localScale = Vector3.one;

        Vector3 position = location.Hex3ToVector3Flat() * canvasResolution.y * canvasScale * radius;
        newUpgrade.GetComponent<Nova.UIBlock>().TrySetLocalPosition(position);

        upgradeTiles.Add(location, newUpgrade.GetComponent<UpgradeTile>());
        UpgradeTile tile = newUpgrade.GetComponent<UpgradeTile>();
        tile.hexPosition = location;

        if (tempUpgradeTierList.Count <= upgrade.upgradeTier)
            tempUpgradeTierList.Add(new HashSet<Hex3>());
            
        tempUpgradeTierList[upgrade.upgradeTier].Add(location);

        tile.Initialize(upgrade, this, GetUpgradeColor(upgrade.upgradeTier));
    }

    public Color GetUpgradeColor(int tier)
    {
        return upgradeColors[tier % upgradeColors.Count];
    }

    public Sprite GetStatIcon(Stat stat)
    {
        return statInfo.GetStatInfo(stat).icon;
    }

    [Button]
    public static void ChangeTechCredits(int count)
    {
        if(totalTechCreditsCollected <= 0 && count > 0)
        {
            FirstTechCreditOnDay = DayNightManager.DayNumber;
            firstTechCreditCollected?.Invoke();
        }

        techCredits += count;
        if (count > 0)
        {
            totalTechCreditsCollected += count;
            techCreditEarned?.Invoke(count);
        }
        else
            totalTechCreditsSpent += count;
        techCreditChanged?.Invoke();
    }

    private void UpdateRepInfo(DirectiveQuest quest)
    {
        if (quest.useRepReward)
            UpdateRepInfo();
    }    

    private void UpdateRepInfo()
    {
        repText.Text = ReputationManager.Reputation.ToString();
        techCreditText.Text = techCredits.ToString();
    }

    [Button]
    private void ClearTechCredits()
    {
        ES3.Save<int>(GameConstants.techCredits, 0, GameConstants.StatsPath);
        ES3.Save<int>(GameConstants.totalTechCreditsCollected, 0, GameConstants.StatsPath);
        techCredits = 500;
        totalTechCreditsCollected = 0;
    }

    public override void OpenWindow()
    {
        if(!techTreeUnlocked)
            return;

        overlayCamera.enabled = false;
        techTreeCamera.enabled = true;

        base.OpenWindow();

        if(interactableControl != null)
            interactableControl.SetInteractable(true);

        background.gameObject.SetActive(true);
        UpdateRepInfo();

        techTreeOpen?.Invoke(true);
    }

    public override void CloseWindow()
    {
        overlayCamera.enabled = true;
        techTreeCamera.enabled = false;

        base.CloseWindow();
        if(interactableControl != null)
            interactableControl.SetInteractable(false);
        background.gameObject.SetActive(false);
        techTreeOpen?.Invoke(false);

        if(TotalTechCreditsCollected > 0)
            openButtonAnimator.SetTrigger("ButtonOn");

    }

    private void ButtonOff()
    {
        Interactable interactable = openButton.GetComponent<Interactable>();
        if (interactable.ClickBehavior == ClickBehavior.None)
            return;

        openButton.GetComponent<Interactable>().ClickBehavior = ClickBehavior.None;
        openButtonAnimator.SetTrigger("ButtonOff");
    }

    private void ButtonOn()
    {
        techTreeUnlocked = true;

        Interactable interactable = openButton.GetComponent<Interactable>();
        if(interactable.ClickBehavior == ClickBehavior.OnRelease)
            return;

        openButton.GetComponent<Interactable>().ClickBehavior = ClickBehavior.OnRelease;
        openButtonAnimator.SetTrigger("Highlight");
    }

}
