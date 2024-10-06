using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nova;

public class LeaderButton : MonoBehaviour
{
    [SerializeField] private UIBlock2D leaderImage;
    [SerializeField] private LeaderUpgrades leaderData; 
    private void OnEnable()
    {
        leaderData = FindObjectOfType<SessionManager>()?.LeaderData;
        leaderImage.SetImage(leaderData?.avatar);
    }
}
