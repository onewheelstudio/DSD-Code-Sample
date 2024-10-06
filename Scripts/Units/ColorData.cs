using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;

[CreateAssetMenu(menuName = "Hex/Color Data")]
public class ColorData : SerializedScriptableObject
{
    [SerializeField] private List<MyColor> colors = new List<MyColor>();
    public Color GetColor(ColorCode colorCode)
    {
        if(colors == null || colors.Count == 0)
            return Color.white;

        MyColor myColor = colors.FirstOrDefault(x => x.colorCode == colorCode);
        return myColor == null ? Color.white : myColor.color;
    }

    [Button]
    private void AddColor()
    {
        colors.Add(new MyColor());
    }

    [LabelWidth(100), System.Serializable]
    private class MyColor
    {
        [HorizontalGroup("Color")]
        public ColorCode colorCode;
        [HorizontalGroup("Color")]
        public Color color = Color.white;
        [Button("Copy Hex Code"), GUIColor("color")]
        private void CopyColor()
        {
            GUIUtility.systemCopyBuffer = ColorUtility.ToHtmlStringRGB(color).ToString();
        }
    }
}

public enum ColorCode
{
    red,
    green,
    yellow,
    blue,
    offPriority,
    lowPriority,
    mediumPriority,
    highPriority,
    techCredit,
    repuation,
    buttonGreyOut,
    callOut,
    crystalIcon,
    markerIcon,
    enemyUnitIcon,
}


