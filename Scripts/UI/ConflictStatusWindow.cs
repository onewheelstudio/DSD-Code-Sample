using HexGame.Resources;
using Nova;
using NovaSamples.UIControls;
using System;
using UnityEngine;

public class ConflictStatusWindow : WindowPopup
{
    [SerializeField] private ListView sectorRequests;
    private PlayerResources playerResources;

    [Header("Sector Stats")]
    [SerializeField] private TextBlock sectorNumber;
    [SerializeField] private TextBlock humanForces;
    [SerializeField] private TextBlock enemyForces;
    [SerializeField] private ListView resourceSliders;

    [SerializeField] private Button buildColony;

    private void Awake()
    {
        playerResources = FindFirstObjectByType<PlayerResources>();
        sectorRequests.AddDataBinder<SectorRequest, SectorRequestVisuals>(PopulateSectorRequests);
        resourceSliders.AddDataBinder<ResourceAmount, ResourceSliderVisuals>(PopulateResourceSliders);
    }

    private void Start()
    {
        CloseWindow();
    }

    private void UIMapTile_OnTileClicked(UIMapTile obj)
    {
        throw new NotImplementedException();
    }

    private new void OnEnable()
    {
        base.OnEnable();
        UIMapTile.OnTileClicked += SectorClicked;
        buildColony.Clicked += BuildNewColony;
    }

    private new void OnDisable()
    {
        base.OnDisable();
        UIMapTile.OnTileClicked -= SectorClicked;
        buildColony.Clicked -= BuildNewColony;
    }

    private void PopulateSectorRequests(Data.OnBind<SectorRequest> evt, SectorRequestVisuals target, int index)
    {
        target.resourceAmount.Text = evt.UserData.resource.amount.ToString();
        target.reinforcementAmount.Text = evt.UserData.reinforcements.ToString();
        target.acceptButton.RemoveClickListeners();
        target.acceptButton.Clicked += () => AddRequest(evt.UserData);

        if(playerResources != null)
        {
            var resourceTemplate = playerResources.GetResourceTemplate(evt.UserData.resource.type);
            target.resourceIcon.SetImage(resourceTemplate.icon);
            target.resourceIcon.Color = resourceTemplate.resourceColor;
        }
    }

    private void AddRequest(SectorRequest request)
    {
        Debug.Log(request.resource.amount + " " + request.reinforcements);
    }

    private void SectorClicked(UIMapTile tile)
    {
        sectorRequests.SetDataSource(tile.levelData.GetRequests());
        sectorNumber.Text = $"Sector {tile.levelData.location.ToNumber()}";
        humanForces.Text = $"Human Forces: {tile.levelData.playerForces.ToString()}";
        enemyForces.Text = $"Enemy Forces: {tile.levelData.enemyForces.ToString()}";

        resourceSliders.SetDataSource(tile.levelData.resources);

        if(tile.levelData.enemyForces > 0 || tile.levelData.playerForces == 0)
            buildColony.gameObject.SetActive(false);
        else
            buildColony.gameObject.SetActive(true);
    }

    private void PopulateResourceSliders(Data.OnBind<ResourceAmount> evt, ResourceSliderVisuals target, int index)
    {
        if (playerResources != null)
        {
            var resourceTemplate = playerResources.GetResourceTemplate(evt.UserData.type);
            target.icon.SetImage(resourceTemplate.icon);
            target.icon.Color = resourceTemplate.resourceColor;
            target.sliderFill.Color = resourceTemplate.resourceColor;
        }

        target.slider.Min = 0;
        target.slider.Max = 100;
        target.slider.Value = evt.UserData.amount;
    }

    private bool CanBuildNewColony()
    {
        return true;
    }

    private void BuildNewColony()
    {
        if (CanBuildNewColony())
        {
            //LoadingScreenManager lsm = this.GetComponent<LoadingScreenManager>();
            //lsm.DoStart();
            GameObject.FindAnyObjectByType<SaveLoadManager>().ChangeColony("AutoSave 1");
        }
        else
        {

        }
    }
}
