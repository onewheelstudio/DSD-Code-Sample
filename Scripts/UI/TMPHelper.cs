using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HexGame.Resources;
public static class TMPHelper
{
    public static string Icon(Stat stat)
    {
        return $"<sprite={(int)stat}>";
    }

    public static string Icon(Stat stat, Color color)
    {
        return $"<sprite={(int)stat} color=#{ColorUtility.ToHtmlStringRGBA(color)}>";
    }

    public static string Icon(ResourceType resource)
    {
        return $"<sprite={(int)resource}>";
    }

    public static string ResearchIcon()
    {
        return $"<sprite={15}>";
    }

    public static string Icon(ResourceType resource, Color color)
    {
        return $"<sprite={(int)resource} color=#{ColorUtility.ToHtmlStringRGBA(color)}>";
    }


    public static string Color(string text, Color color)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{text}</color>";
    }
    
    public static string TMP_Color(this string text, Color color)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{text}</color>";
    }

    public static string Bold(string text)
    {
        return $"<b>{text}</b>";
    }

    public static string Italic(string text)
    {
        return $"<i>{text}</i>";
    }

    public static string Indent(string text, int percent)
    {
        return $"<indent={percent}%>{text}</indent>";
    }

    public static string Size(string text, int scale)
    {
        return $"<size={scale}%>{text}</size>";
    }
}
