using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class UpgradeWindow : OdinEditorWindow
{
    private List<Upgrade> upgradeList;
    [BoxGroup("Sorting Options")]
    [SerializeField, Range(0,10), OnValueChanged("SortUpgrades")] private int Tier = 0;
    [BoxGroup("Sorting Options")] 
    [SerializeField, OnValueChanged("SortUpgrades")] private bool hideUnlocked = true;
    [BoxGroup("Sorting Options")] 
    [SerializeField, OnValueChanged("SortUpgrades")] private bool showNotInTechTree = false;

    [ListDrawerSettings(NumberOfItemsPerPage = 20), Searchable]
    public List<UpgradeData> upgradeDataList = new List<UpgradeData>();

    [MenuItem("Tools/Upgrade Window")]
    private static void OpenWindow()
    {
        UpgradeWindow window = GetWindow<UpgradeWindow>();
        window.Show();
        window.SortUpgrades();
    }

    [Button, GUIColor(0.5f,1f,0.5f)]
    private void GetUpgrades()
    {
        upgradeList = HelperFunctions.GetScriptableObjects<Upgrade>("Assets/ScriptableObjects/Upgrades");

        upgradeList = upgradeList.OrderBy(x => x.upgradeTier).ThenBy(x => x.subTier).ToList();

        upgradeDataList.Clear();
        foreach (var upgrade in upgradeList)
        {
            UpgradeData data = new UpgradeData(upgrade);
            upgradeDataList.Add(data);
        }
    }

    private void SortUpgrades()
    {
        GetUpgrades();
        
        upgradeDataList = upgradeDataList.Where(u => u.Tier == this.Tier).OrderBy(x => x.subTier).ToList();

        if (hideUnlocked)
            upgradeDataList = upgradeDataList.Where(u => !u.unlockedAtStart).ToList();

        if (!showNotInTechTree)
            upgradeDataList = upgradeDataList.Where(u => u.showInTechTree).ToList();
    }

    [System.Serializable, PreviewField]
    public class UpgradeData
    {
        [HideInInspector]
        public Upgrade upgrade;
        [HorizontalGroup("Top", width: 250), ShowInInspector, HideLabel]
        public string upgradeName
        {
            get => upgrade.UpgradeName;
        }
        
        [HorizontalGroup("Top", width: 125), ShowInInspector]
        //[Range(0,10)]
        public int Tier
        {
            get => upgrade.upgradeTier;
            set => upgrade.upgradeTier = value;
        }
        [HorizontalGroup("Top", width: 125), ShowInInspector]
        //[Range(0,10)]
        public int subTier
        {
            get => upgrade.subTier;
            set => upgrade.subTier = value;
        }

        [HorizontalGroup("Top", width: 175), ShowInInspector, LabelWidth(125)]
        public bool showInTechTree
        {
            get => upgrade.showInTechTree;
            set => upgrade.showInTechTree = value;
        }
        [HorizontalGroup("Top", width: 200), ShowInInspector, LabelWidth(125)]
        [InlineButton("Select", ButtonColor = "RGB(0.5,0.5,1)")]
        public bool unlockedAtStart
        {
            get => upgrade.unlockedAtStart;
            set => upgrade.unlockedAtStart = value;
        }

        public UpgradeData(Upgrade upgrade)
        {
            this.upgrade = upgrade;
        }

     
        private void Select()
        {
           Selection.activeObject = upgrade;
        }
    }
}
