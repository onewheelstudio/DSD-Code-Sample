using UnityEngine;

public class ColorManager : MonoBehaviour
{
    [SerializeField] private ColorData colorData;
    private static ColorManager _instance;
    
    public static Color GetColor(ColorCode colorCode)
    {
        if (_instance == null)
            _instance = FindObjectOfType<ColorManager>();

        return _instance.colorData.GetColor(colorCode);
    }
}
