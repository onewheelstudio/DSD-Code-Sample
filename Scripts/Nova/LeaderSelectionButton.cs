using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nova;
using NovaSamples.UIControls;
using System;
using Sirenix.OdinInspector;

public class LeaderSelectionButton : UIControl<LeaderSelectionVisuals>
{
    [SerializeField, OnValueChanged("PopulateLeaderData")] private LeaderUpgrades leaderData;
    public static event Action<LeaderSelectionButton> leaderSelected;
    [SerializeField] private UIBlock2D avatar;
    [SerializeField] private TextBlock leaderName;
    [SerializeField] private TextBlock description;
    [SerializeField] private TextBlock abilities;

    private void OnEnable()
    {
        View.UIBlock.AddGestureHandler<Gesture.OnClick, LeaderSelectionVisuals>(SelectLeader);
        View.UIBlock.AddGestureHandler<Gesture.OnHover, LeaderSelectionVisuals>(LeaderSelectionVisuals.OnHover);
        View.UIBlock.AddGestureHandler<Gesture.OnUnhover, LeaderSelectionVisuals>(LeaderSelectionVisuals.OnUnHover);

        leaderSelected += ToggleOff;

        if (leaderData != null)
            PopulateLeaderData();
    }

    private void OnDisable()
    {
        View.UIBlock.RemoveGestureHandler<Gesture.OnClick, LeaderSelectionVisuals>(SelectLeader);
        View.UIBlock.RemoveGestureHandler<Gesture.OnHover, LeaderSelectionVisuals>(LeaderSelectionVisuals.OnHover);
        View.UIBlock.RemoveGestureHandler<Gesture.OnUnhover, LeaderSelectionVisuals>(LeaderSelectionVisuals.OnUnHover);

        leaderSelected -= ToggleOff;
    }

    private void PopulateLeaderData()
    {
        avatar.SetImage(leaderData.avatar);
        leaderName.Text = leaderData.leaderName;
        description.Text = leaderData.backgroundStory;
        string leaderAbilities = "";

        foreach (var statUpgrade in leaderData.statUpgrades)
        {
            leaderAbilities += statUpgrade.UpgradeName + "\n";
        }
        foreach (var globalUpgrade in leaderData.globalUpgrades)
        {
            leaderAbilities += globalUpgrade.UpgradeName + "\n";
        }
        foreach (var unitUnlock in leaderData.unitUnlockUpgrades)
        {
            leaderAbilities += unitUnlock.UpgradeName + "\n";
        }

        abilities.Text = TMPHelper.Color(leaderAbilities, Color.cyan);
    }

    private void SelectLeader(Gesture.OnClick evt, LeaderSelectionVisuals target)
    {
        GameObject.FindObjectOfType<SessionManager>().LeaderData = this.leaderData;
        leaderSelected?.Invoke(this);
        target.ToggleSelection(true);
        //do more stuff
    }

    private void ToggleOff(LeaderSelectionButton leaderButton)
    {
        if (leaderButton == this)
            return;

        LeaderSelectionVisuals visuals = View.Visuals as LeaderSelectionVisuals;
        visuals.ToggleSelection(false);
    }
}
