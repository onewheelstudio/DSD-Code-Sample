using DG.Tweening;
using HexGame.Units;
using Nova;
using Nova.Animations;
using NovaSamples.UIControls;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BuildMenu : MonoBehaviour, ISaveData
{
    [SerializeField]
    private List<BuildGroup> buildGroups = new List<BuildGroup>();
    private HashSet<PlayerUnitType> unlockedUnits = new HashSet<PlayerUnitType>();

    [Header("Tile Building")]
    [SerializeField] private BuildGroup tileBuilding;
    private bool tileUnlocked = false;

    [Header("Callout Settings")]
    [SerializeField] private float calloutSize = 1.05f;
    [SerializeField] private float calloutTime = 0.75f;

    public static event Action<UIBlock> IndicateButton;
    [SerializeField] private GameSettings gameSettings;

    private void Awake()
    {
        RegisterDataSaving();
    }

    private void Start()
    {
        UpdateVisibility();
    }

    private void OnEnable()
    {
        foreach (var group in buildGroups)
        {
            group.buttons = group.buttonParent.GetComponentsInChildren<AddUnitButton>(true).ToList();

            //if we hover over any button in the group, kill the highlight animation
            foreach (var button in group.buttonParent.GetComponentsInChildren<Button>(true))
            {
                button.Clicked += () => StopAnimation(group);
            }
        }

        foreach (var button in tileBuilding.buttonParent.GetComponentsInChildren<Button>(true))
        {
            button.Clicked += () => StopAnimation(tileBuilding);
        }

        SetTileBuildingButton(false);
        UnlockTileBuilding.unlockTileBuilding += UnlockTiles;
        UnlockUnitTrigger.unitUnlocked += UnLockUnit;
        UnitUnlockUpgrade.unlockBuilding += UnLockUnit;
    }

    private void StopAnimation(BuildGroup group)
    {
        if(group.animHandles.Count == 0)
            return;

        group.animHandles.ForEach(a =>  a.Complete());

        group.buttonIcon.Color = Color.white;
        group.buttonIcon.transform.localScale = Vector3.one;
    }

    private void OnDisable()
    {
        UnlockTileBuilding.unlockTileBuilding -= UnlockTiles;
        UnlockUnitTrigger.unitUnlocked -= UnLockUnit;
        UnitUnlockUpgrade.unlockBuilding -= UnLockUnit;
        DOTween.Kill(this,true);
    }

    private void UpdateVisibility()
    {
        foreach (var group in buildGroups)
        {
            bool someUnlocked = group.buttons.Any(x => x.CanPlace);
            group.buttonIcon.Color = someUnlocked ? Color.white : ColorManager.GetColor(ColorCode.buttonGreyOut);
            group.interactable.enabled = someUnlocked;

            //check status
            foreach (var unitButton in group.buttons)
            {
                unitButton.gameObject.SetActive(unitButton.CanPlace);
            }
        }
    }

    public void UnlockTiles()
    {
        SetTileBuildingButton(true);
        if (!SaveLoadManager.Loading)
            HighlightButton(tileBuilding);
    }

    private void SetTileBuildingButton(bool unlock)
    {
        tileUnlocked = true;
        tileBuilding.buttonIcon.Color = unlock ? Color.white : ColorManager.GetColor(ColorCode.buttonGreyOut);
        tileBuilding.interactable.enabled = unlock;
        if(unlock && !StateOfTheGame.tutorialSkipped && !SaveLoadManager.Loading)
            IndicateButton?.Invoke(tileBuilding.groupButton);
    }

    public void UnlockUnitType(PlayerUnitType playerUnitType)
    {
        if(!unlockedUnits.Add(playerUnitType))
            return;
    }

    public void UnlockAll()
    {
        foreach (PlayerUnitType type in System.Enum.GetValues(typeof(PlayerUnitType)))
        {
            if(gameSettings.IsDemo && !gameSettings.DemoTypes.Contains(type))
                continue;

            if(unlockedUnits.Add(type) && type != PlayerUnitType.hq)
                UnLockUnit(type);
        }
    }

    public void UnLockUnit(PlayerUnitType unitType)
    {
        foreach (var group in buildGroups)
        {
            AddUnitButton button = group.buttons.FirstOrDefault(x => x.unitType == unitType);


            if (button != null)
            {
                button.UnlockButton();
                if (SaveLoadManager.Loading)
                    break;
                MessagePanel.ShowMessage($"Building Unlocked: {unitType.ToNiceString()}", null);
                StopAnimation(group);
                HighlightButton(group);
                IndicateButton?.Invoke(group.groupButton);
                break;
            }
        }
        UpdateVisibility();
    }

    private void HighlightButton(BuildGroup buildGroup)
    {
        UIBlock2D icon = buildGroup.groupButton.transform.GetChild(0).GetComponent<UIBlock2D>();

        if(icon != null)
        {
            ButtonHighlightAnimation animation = new ButtonHighlightAnimation()
            {
                startSize = new Vector3(50, 50, 0),
                endSize = new Vector3(50, 50, 0) * calloutSize,
                startColor = ColorManager.GetColor(ColorCode.callOut),
                endColor = ColorManager.GetColor(ColorCode.callOut),
                endAlpha = 0.5f,
                uIBlock = icon
            };

            AnimationHandle handle = animation.Loop(calloutTime, -1);
            buildGroup.animHandles.Add(handle);
        }
    }

    public void LockUnit(PlayerUnitType unitType)
    {
        unlockedUnits.Remove(unitType);

        foreach (var group in buildGroups)
        {
            AddUnitButton button = group.buttons.FirstOrDefault(x => x.unitType == unitType);

            if (button != null)
            {
                button.LockButton();
                break;
            }
        }
        UpdateVisibility();
    }


    [Button]
    private void AlignMenus()
    {
        foreach (var group in buildGroups)
        {
            Vector3 position = group.buttonParent.GetComponent<UIBlock2D>().Position.Value;
            position.y = group.groupButton.Position.Value.y;
            group.buttonParent.GetComponent<UIBlock2D>().Position.Value = position;
        }
    }

    private const string UNLOCKED_UNITS = "unlockedUnits";
    private const string TILES_UNLOCKED = "tilesUnlocked";

    public void RegisterDataSaving()
    {
        SaveLoadManager.RegisterData(this);
    }

    public void Save(string savePath, ES3Writer writer)
    {
        writer.Write<HashSet<PlayerUnitType>>(UNLOCKED_UNITS, unlockedUnits);
        writer.Write<bool>(TILES_UNLOCKED, tileUnlocked);
    }

    public IEnumerator Load(string loadPath, Action<string> postUpdateMessage)
    {
        if(ES3.KeyExists(UNLOCKED_UNITS, loadPath))
        {
            foreach (PlayerUnitType type in ES3.Load<HashSet<PlayerUnitType>>(UNLOCKED_UNITS, loadPath))
            {
                UnLockUnit(type);
            }
        }

        if(ES3.KeyExists(TILES_UNLOCKED, loadPath))
        {
            tileUnlocked = ES3.Load<bool>(TILES_UNLOCKED, loadPath);
            if(tileUnlocked)
                SetTileBuildingButton(tileUnlocked);
        }

        yield return null;
    }
}

[System.Serializable]
public class BuildGroup
{
    [Title("@groupButton?.name")]
    [OnValueChanged("GetParts")]
    public UIBlock groupButton;
    public Interactable interactable;
    public UIBlock2D buttonIcon;
    public Transform buttonParent;
    [HideIf("@true")]
    public List<AddUnitButton> buttons;
    public List<Tween> tweens = new List<Tween>();
    public List<AnimationHandle> animHandles = new List<AnimationHandle>();

    private void GetParts()
    {
        if (groupButton == null)
            return;
        this.buttonIcon = this.groupButton.GetComponentsInChildren<UIBlock2D>().First(x => x.transform != this.groupButton.transform);
        interactable = groupButton.GetComponent<Interactable>();
    }
}
