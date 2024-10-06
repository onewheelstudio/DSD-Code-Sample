using Nova;
using NovaSamples.UIControls;
using UnityEngine;
using HexGame.Resources;

[RequireComponent(typeof(Button))]
public class AddTileButton : MonoBehaviour
{
    private Button button;
    private TextBlock label;
    HexTileManager htm;
    [SerializeField] private HexTileType tileType;
    public HexTileType TileType => tileType;
    private void Awake()
    {
        button = this.GetComponent<Button>();
        label = this.GetComponentInChildren<TextBlock>(true);
        htm = FindObjectOfType<HexTileManager>();
        label.Text = tileType.ToNiceString();

        FindAnyObjectByType<TileMenu>().AddTileButton(tileType, this.GetComponent<UIBlock2D>());
    }

    private void OnEnable()
    {
        
        button.clicked += AddTile;
    }

    private void OnDisable()
    {
        button.clicked -= AddTile;
    }

    private void AddTile()
    {
        htm.SetNextTile(TileType, true);
    }
}
