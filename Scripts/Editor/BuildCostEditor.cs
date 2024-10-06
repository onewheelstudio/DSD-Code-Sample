using HexGame.Resources;
using HexGame.Units;
using UnityEditor;

public class BuildCostEditor : EnumSOCreator<PlayerUnitType, BuildCost>
{
    [MenuItem("Tools/Build Cost Editor")]
    public static void OpenWindow()
    {
        BuildCostEditor window = GetWindow<BuildCostEditor>();
        playerPrefsPath = $"Build Cost SO Path";
        window.GetPath();
        window.GetPrefix();
        window.Show();
    }
}
