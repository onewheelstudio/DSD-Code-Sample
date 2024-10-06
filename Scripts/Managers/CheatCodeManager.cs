using HexGame.Resources;
using Sirenix.OdinInspector;

public class CheatCodeManager : SerializedMonoBehaviour
{
    private void Start()
    {
        CheatCodes.AddButton(() => FindObjectOfType<BuildMenu>().UnlockAll(), "Unlock");
        CheatCodes.AddButton(() => FindObjectOfType<BuildMenu>().UnlockTiles(), "Tiles");
        CheatCodes.AddButton(() => RevealAllTiles(), "Reveal Tiles");
        CheatCodes.AddButton(AddAllResources, "All R's");
    }

    private void AddAllResources()
    {
        PlayerResources ps = FindObjectOfType<PlayerResources>();
        foreach (ResourceType resource in System.Enum.GetValues(typeof(ResourceType)))
        {
            ps.ChangeStorageLimit(resource, 500);
            ps.AddResource(resource, 500);
        }
    }

    private void RevealAllTiles()
    {
        FogGroundTile[] tiles = FindObjectsOfType<FogGroundTile>(true);
        foreach (var tile in tiles)
        {
            tile.DoTileAppear(0, DG.Tweening.Ease.Linear);
            tile.enabled = false;
        }
    }
}


