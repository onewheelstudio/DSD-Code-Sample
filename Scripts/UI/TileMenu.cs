using HexGame.Resources;
using Nova;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TileMenu : BuildingSelectWindow
{
    [SerializeField] private Transform buttonParent;
    private Dictionary<HexTileType, UIBlock2D> tileButtons = new Dictionary<HexTileType, UIBlock2D>();

    public bool LoadComplete => loadComplete;
    private bool loadComplete = false;

    private void Start()
    {
        UnlockTileButton(HexTileType.grass);
        UnlockTileButton(HexTileType.forest);
        UnlockTileButton(HexTileType.aspen);
        LockTileButton(HexTileType.hill);
        LockTileButton(HexTileType.water);
        LockTileButton(HexTileType.mountain);
        LockTileButton(HexTileType.sand);

        interactable = this.button.GetComponent<Interactable>();

        CheatCodes.AddButton(()=> UnlockTileButton(HexTileType.mountain), "Unlock Mountain");
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
