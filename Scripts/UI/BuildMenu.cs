using DG.Tweening;
using HexGame.Units;
using Nova;
using NovaSamples.UIControls;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BuildMenu : MonoBehaviour
{
    [SerializeField]
    private List<BuildGroup> buildGroups = new List<BuildGroup>();
    private HashSet<PlayerUnitType> unlockedUnits = new HashSet<PlayerUnitType>();

    [Header("Tile Building")]
    [SerializeField] private BuildGroup tileBuilding;

    [Header("Callout Settings")]
    [SerializeField] private float calloutSize = 1.05f;
    [SerializeField] private float calloutTime = 0.75f;

    private void OnEnable()
    {
        foreach (var group in buildGroups)
        {
            group.buttons = group.buttonParent.GetComponentsInChildren<AddUnitButton>(true).ToList();

            //if we hover over any button in the group, kill the highlight animation
            foreach (var button in group.buttonParent.GetComponentsInChildren<Button>(true))
            {
                button.hover += () => StopAnimation(group);
            }
        }

        foreach (var button in tileBuilding.buttonParent.GetComponentsInChildren<Button>(true))
        {
            button.hover += () => StopAnimation(tileBuilding);
        }

        SetTileBuildingButton(false);
        UnlockTileBuilding.unlockTileBuilding += UnlockTiles;
        UnlockUnitTrigger.unitUnlocked += UnLockUnit;
        UnitUnlockUpgrade.unlockBuilding += UnLockUnit;
    }

    private void StopAnimation(BuildGroup group)
    {
        if(group.tweens.Count == 0)
            return;

        group.tweens.ForEach(x => x.Kill(true));
        group.tweens.Clear();

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
        HighlightButton(tileBuilding);
    }

    private void SetTileBuildingButton(bool unlock)
    {
        tileBuilding.buttonIcon.Color = unlock ? Color.white : ColorManager.GetColor(ColorCode.buttonGreyOut);
        tileBuilding.interactable.enabled = unlock;
    }

    public void UnitTypeAdded(PlayerUnitType playerUnitType)
    {
        if(!unlockedUnits.Add(playerUnitType))
            return;
    }

    public void UnlockAll()
    {
        foreach (PlayerUnitType type in System.Enum.GetValues(typeof(PlayerUnitType)))
        {
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
                MessagePanel.ShowMessage($"Building Unlocked: {unitType.ToNiceString()}", null);
                button.UnlockButton();
                HighlightButton(group);
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
            Tween tween = icon.DoScale(Vector3.one * calloutSize, calloutTime)
                              .SetLoops(-1, LoopType.Yoyo);
            buildGroup.tweens.Add(tween);

            tween = icon.DOColor(ColorManager.GetColor(ColorCode.callOut), calloutTime)
                        .SetLoops(-1, LoopType.Yoyo);
            buildGroup.tweens.Add(tween);
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

    private void GetParts()
    {
        if (groupButton == null)
            return;
        this.buttonIcon = this.groupButton.GetComponentsInChildren<UIBlock2D>().First(x => x.transform != this.groupButton.transform);
        interactable = groupButton.GetComponent<Interactable>();
    }
}
