using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Nova;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputBinding;
using static UnityEngine.InputSystem.Samples.RebindUI.RebindActionUI;
using HexGame.Resources;

public class InfoToolTip : MonoBehaviour, IHavePopupInfo, IHaveIcon
{
    [SerializeField] private string title;
    [SerializeField] private Sprite icon;
    [TextArea(3,10)]
    [SerializeField] private string description;
    private WaitForSeconds delay = new WaitForSeconds(0.75f);
    [SerializeField] private Vector2 offset;

    public static event Action<List<PopUpInfo>, Sprite, Vector2, InfoToolTip> openToolTip;
    public static event Action<List<PopUpResource>> openToolTipStats;
    public static event Action<InfoToolTip> closeToolTip;

    [Header("Hotkey Tooltip")]
    [SerializeField] private InputActionReference action;

    public void SetToolTipInfo(string title, Sprite icon, string description)
    {
        this.title = title;
        this.icon = icon;
        this.description = description;
    }
    
    public void SetToolTipInfo(string title, Sprite icon)
    {
        this.title = title;
        this.icon = icon;
    }
    
    public void SetToolTipInfo(string title, Sprite icon, Func<string> description)
    {
        this.title = title;
        this.icon = icon;
        this.description = description?.Invoke();
    }
    
    public void SetToolTipInfo(string title, Func<string> description)
    {
        this.title = title;
        this.description = description?.Invoke();
    }

    private void Reset()
    {
        this.title = this.gameObject.name;
    }
    IEnumerator DelayOpen()
    {
        yield return delay;
        OpenToolTip();
    }
    public void OpenToolTip()
    {
        openToolTip?.Invoke(GetPopupInfo(), GetIcon(), offset, this);
    }
    public void CloseTip()
    {
        closeToolTip?.Invoke(this);
    }

    private bool CheckIfOpenToolTip()
    {
        if (PlayerPrefs.GetInt("ShowToolTips", 1) == 1)
            return true;
        else
            return false;
    }

    public List<PopUpInfo> GetPopupInfo()
    {
        if (action != null)
        {
            List<PopUpInfo> info = new List<PopUpInfo>()
            {
                new PopUpInfo(title, PopUpInfo.PopUpInfoType.name),
                new PopUpInfo(GetBindingDisplay(), PopUpInfo.PopUpInfoType.description)
            };

            return info;
        }
        else
        {
            List<PopUpInfo> info = new List<PopUpInfo>()
            {
                new PopUpInfo(title, PopUpInfo.PopUpInfoType.name),
                new PopUpInfo(description, PopUpInfo.PopUpInfoType.description)
            };

            return info;
        }
    }

    public Sprite GetIcon()
    {
        return icon;
    }

    public void SetOffset(Vector2 offset)
    {
        this.offset = offset;
    }

    public string GetBindingDisplay()
    {
        var displayString = string.Empty;
        var deviceLayoutName = default(string);
        var controlPath = default(string);

        // Get display string from action.
        if (action != null)
        {
            //var bindingIndex = action.bindings.IndexOf(x => x.id.ToString() == m_BindingId);
            //if (bindingIndex != -1)
            displayString = action.action.GetBindingDisplayString(0, out deviceLayoutName, out controlPath, InputBinding.DisplayStringOptions.DontUseShortDisplayNames);
        }

        return $"Hotkey: {displayString}";
    }
}
