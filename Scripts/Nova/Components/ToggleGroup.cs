using NovaSamples.UIControls;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ToggleGroup : MonoBehaviour
{
    [SerializeField] private bool m_AllowSwitchOff = false;

    /// <summary>
    /// Is it allowed that no toggle is switched on?
    /// </summary>
    /// <remarks>
    /// If this setting is enabled, pressing the toggle that is currently switched on will switch it off, so that no toggle is switched on. If this setting is disabled, pressing the toggle that is currently switched on will not change its state.
    /// Note that even if allowSwitchOff is false, the Toggle Group will not enforce its constraint right away if no toggles in the group are switched on when the scene is loaded or when the group is instantiated. It will only prevent the user from switching a toggle off.
    /// </remarks>
    public bool allowSwitchOff { get { return m_AllowSwitchOff; } set { m_AllowSwitchOff = value; } }

    protected List<Toggle> m_Toggles = new List<Toggle>();

    public void ToggleCallback(Toggle toggle, bool isOn)
    {
        if(isOn)
        {
            foreach (var _toggle in m_Toggles)
            {
                if (_toggle != toggle)
                    _toggle.SetToggleWithOutCallback(false);
            }
        }
        else if(!isOn && !allowSwitchOff)
        {
            for (int i = 0; i < m_Toggles.Count; i++)
            {
                if (i == 0)
                    m_Toggles[i].SetToggleWithOutCallback(true);
                else
                    m_Toggles[i].SetToggleWithOutCallback(false);
            }
        }
    }

    internal void RegisterToggle(Toggle toggle)
    {
        m_Toggles.Add(toggle);
        toggle.toggled += ToggleCallback;
    }

    internal void UnRegisterToggle(Toggle toggle)
    {
        m_Toggles.Remove(toggle);
        toggle.toggled -= ToggleCallback;
    }
}


