using HexGame.Resources;
using Nova;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TileMenu : BuildingSelectWindow
{
    [SerializeField] private Transform buttonParent;
    [SerializeField] private List<HexTileImage> tileImages = new List<HexTileImage>();
    private Dictionary<HexTileType, UIBlock2D> tileButtons = new Dictionary<HexTileType, UIBlock2D>();

    private void Start()
    {
        UnlockTileButton(HexTileType.grass);
        UnlockTileButton(HexTileType.forest);
        LockTileButton(HexTileType.water);
        LockTileButton(HexTileType.mountain);
        LockTileButton(HexTileType.sand);
        LockTileButton(HexTileType.aspen);

        interactable = this.button.GetComponent<Interactable>();
    }

    private new void OnEnable()
    {
        base.OnEnable();
        TileUnlockUpgrade.OnTileUnlocked += UnlockTileButton;
        OpenTileMenuTrigger.OpenTileMenu += OpenWindow;
    }

    private new void OnDisable()
    {
        base.OnDisable();
        TileUnlockUpgrade.OnTileUnlocked -= UnlockTileButton;
        OpenTileMenuTrigger.OpenTileMenu -= OpenWindow;
    }

    public override void OpenWindow()
    {
        base.OpenWindow();
    }

    private void UnlockTileButton(HexTileType tileType)
    {
        if(tileButtons.TryGetValue(tileType, out UIBlock2D button))
        {
            button.gameObject.SetActive(true);
            button.transform.SetAsFirstSibling();
        }
    }

    private void LockTileButton(HexTileType tileType)
    {
        if (tileButtons.TryGetValue(tileType, out UIBlock2D button))
        {
            button.gameObject.SetActive(false);
        }
    }

    public void AddTileButton(HexTileType tileType, UIBlock2D buttonBlock)
    {
        if (tileButtons.ContainsKey(tileType))
            return;

        tileButtons.Add(tileType, buttonBlock);
    }

    [System.Serializable]
    private class HexTileImage
    {
        public HexTileType tileType;
        public Sprite tileImage;
        [Range(0, 100)]
        public int probability;
    }

}
