using HexGame.Units;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitToolTip : MonoBehaviour //, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public static event Action<List<PopUpInfo>, List<PopUpPriorityButton>, PopUpCanToggle, RequestStorageInfo, ReceipeInfo, List<PopUpButtonInfo>, UnitToolTip> openToolTip;
    public static event Action<List<PopUpInfo>, List<PopUpPriorityButton>, PopUpCanToggle, RequestStorageInfo, ReceipeInfo, List<PopUpButtonInfo>, UnitToolTip> updateToolTip;
    public static event Action<List<PopUpStats>, UnitToolTip> updateStats;
    public static event Action<List<PopUpResource>, UnitToolTip> updateResources;
    public static event Action<List<PopUpInfo>, UnitToolTip> updateInfo;
    public static event Action<List<PopUpPriorityButton>, UnitToolTip> updateButtons;
    public static event Action<PopUpCanToggle, UnitToolTip> updateToggle;
    public static event Action<RequestStorageInfo, UnitToolTip> updatePriority;
    public static event Action<ReceipeInfo, RequestStorageInfo, UnitToolTip> updateStorage;

    public static event Action<ReceipeInfo, UnitToolTip> updateRecipe;
    public static event Action closeToolTip;
    private WaitForSeconds delay = new WaitForSeconds(0.75f);

    private UnitStorageBehavior usb;
    [SerializeField] private bool showInventory = true;
    public bool ShowInventory => showInventory;

    //cached collections
    private IHavePopupInfo[] havePopupInfos;
    private IHavePopUpButtons[] havePopUpButtons;
    private IHaveResources[] resources;
    private IHaveStats[] stats;
    private bool isActive
    {
        get
        {
            return UnitInfoWindow.toolTipObject == this;
        }
    }

    private void Awake()
    {
        this.usb = this.GetComponent<UnitStorageBehavior>();
    }

    private void OnEnable()
    {
        UnitSelectionManager.unitSelected += UnitSelected;
        UnitSelectionManager.unitUnSelected += UnitUnSelected;
        if(usb)
            usb.connectionChanged += UpdateStorage;
    }



    private void OnDisable()
    {
        UnitSelectionManager.unitSelected -= UnitSelected;
        UnitSelectionManager.unitUnSelected -= UnitUnSelected;
        if(usb)
            usb.connectionChanged -= UpdateStorage;
    }

    private void UnitUnSelected(PlayerUnit unit)
    {
        if(!isActive)
            CloseTip();
    }

    private void UnitSelected(PlayerUnit unit)
    {
        if(unit == null)
            return;

        if(unit.gameObject == this.gameObject)
        {
            havePopupInfos = this.GetComponents<IHavePopupInfo>();
            havePopUpButtons = this.GetComponents<IHavePopUpButtons>();
            stats = this.GetComponents<IHaveStats>();
            resources = this.GetComponents<IHaveResources>(); 
            OpenTip();
        }
    }

    private void OpenTip()
    {
        openToolTip?.Invoke(GetPopUpInfos(), GetPriorityButtons(), GetPopUpToggle(), GetStorageInfo(), GetReceipes(), GetPopUpButtons(), this);
        updateResources?.Invoke(GetPopUpResources(), this);
        updateStats?.Invoke(GetPopUpStats(), this);
        //isActive = true;
        //if(this.gameObject.activeInHierarchy)
            StartCoroutine(UpdateTip());
    }

    private IEnumerator UpdateTip()
    {
        //yield return null; // not needed but helps ensure window is open. reduces fragility due to order of code in OpenTip
        while (isActive)
        {
            //updateToolTip?.Invoke(GetPopUpInfos(), GetPopUpButtons(), GetPopUpToggle(), GetRequestPriority(), this);
            updateInfo?.Invoke(GetPopUpInfos(), this);
            updateResources?.Invoke(GetPopUpResources(), this);
            updateStats?.Invoke(GetPopUpStats(), this); 
            updateRecipe?.Invoke(GetReceipes(), this);
            yield return null;
        }
    }

    private void UpdateStorage(UnitStorageBehavior behavior)
    {
        updateStorage?.Invoke(GetReceipes(), GetStorageInfo(), this);
    }

    private List<PopUpInfo> GetPopUpInfos()
    {
        List<PopUpInfo> popUpInfos = new List<PopUpInfo>();
        foreach (IHavePopupInfo infoObject in havePopupInfos)
            popUpInfos.AddRange(infoObject.GetPopupInfo());
        
        popUpInfos = popUpInfos.OrderBy(x => x.priority).ToList();

        return popUpInfos;
    }

    private List<PopUpPriorityButton> GetPriorityButtons()
    {
        List<PopUpPriorityButton> popUpButtons = new List<PopUpPriorityButton>();
        foreach (var buttons in havePopUpButtons)
            popUpButtons.AddRange(buttons.GetPopUpButtons());

        popUpButtons = popUpButtons.OrderBy(x => x.priority).ToList();

        return popUpButtons;
    }

    private PopUpCanToggle GetPopUpToggle()
    {
        if (this.TryGetComponent<ICanToggle>(out ICanToggle canToggle))
            return canToggle.CanToggleOff();
        else
            return new PopUpCanToggle();
    }

    private RequestStorageInfo GetStorageInfo()
    {
        if (this.TryGetComponent<HexGame.Units.IHaveRequestPriority>(out HexGame.Units.IHaveRequestPriority priority))
            return priority.GetPopUpRequestPriority();
        else
            return new RequestStorageInfo();
    }

    public void UpdateRecipes(int selectedRecipe)
    {
        updateRecipe?.Invoke(GetReceipes(), this);
    }

    private ReceipeInfo GetReceipes()
    {
        ReceipeInfo receipeInfo = new ReceipeInfo();

        if (this.TryGetComponent(out IHaveReceipes receipeOwner))
        {
            receipeInfo.receipes = receipeOwner.GetReceipes().Where(r => r.IsUnlocked).ToList().AsReadOnly();
            receipeInfo.receipeOwner = receipeOwner;
            receipeInfo.currentRecipe = receipeOwner.GetCurrentRecipe();
            receipeInfo.efficiency = receipeOwner.GetEfficiency();
            receipeInfo.timeToProduce = Mathf.Max(0,receipeOwner.GetTimeToProduce());
        }
            
        return receipeInfo;
    }

    private List<PopUpStats> GetPopUpStats()
    {
        List<PopUpStats> popUpStats = new List<PopUpStats>();
        foreach (var statList in stats)
            popUpStats.AddRange(statList.GetPopUpStats());

        return popUpStats;
    }

    private List<PopUpResource> GetPopUpResources()
    {
        List<PopUpResource> popUpResources = new List<PopUpResource>();
        foreach (var resourceList in resources)
            popUpResources.AddRange(resourceList.GetPopUpResources());

        return popUpResources;
    }

    private List<PopUpButtonInfo> GetPopUpButtons()
    {
        IHaveButtons[] buttons = this.GetComponents<IHaveButtons>();
        List<PopUpButtonInfo> popUpButtonInfos = new List<PopUpButtonInfo>();
        foreach (var button in buttons)
        {
            popUpButtonInfos.AddRange(button.GetButtons());
        }

        return popUpButtonInfos;
    }

    private void CloseTip()
    {
        closeToolTip?.Invoke();
        //isActive = false;
    }

    private bool CheckIfOpenToolTip()
    {
        if (PlayerPrefs.GetInt("ShowToolTips", 1) == 1)
            return true;
        else
            return false;
    }
}
