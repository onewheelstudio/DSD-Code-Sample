using HexGame.Grid;
using HexGame.Units;
using Shapes;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HexAreaDraw : ImmediateModeShapeDrawer
{
    [Header("Hex Settings")]
    [SerializeField, Range(0.5f,1f)] private float hexSize = 0.95f;
    [SerializeField] private Color hexFinalColor = new Color(0.5f, 0.5f, 0.5f, 0.2f);
    private Color hexStartColor;
    [SerializeField, Range(0f,0.5f)] private float borderThickness = 0.06f;
    [SerializeField, Range(-0.5f,0.5f)] private float borderOffset = -0.077f;
    [SerializeField] private Color borderFinalColor = new Color(0.5f, 0.5f, 0.5f, 0.25f);
    private Color borderStartColor;

    [Header("Draw Options")]
    [SerializeField] private float angle = 0f;
    [SerializeField] private float rotation = 30f;
    [SerializeField] private Vector3 rotAxis = Vector3.forward;
    [SerializeField, Range(0f, 20f)] private float fadeSpeed = 12.5f;

    [Header("Tiles")]
    [SerializeField] private HashSet<Hex3> tiles = new HashSet<Hex3>();
    private float timeStep;

    private new void OnEnable()
    {
        base.OnEnable();
        UnitSelectionManager.UnitMoved += UnitMoved;
    }

    private new void OnDisable()
    {
        base.OnDisable();
        UnitSelectionManager.UnitMoved -= UnitMoved;
    }

    public override void DrawShapes(Camera cam)
    {
        if (tiles.Count == 0)
            return;

        if (Time.timeScale > 0)
            timeStep = Time.deltaTime;

        using (Draw.Command(cam))
        {
            //Draw.Color = hexColor; // Set color to grey with 50% alpha
            Draw.Matrix = transform.localToWorldMatrix;
            Draw.BlendMode = ShapesBlendMode.Transparent;
            Draw.Rotate(rotation * Mathf.Deg2Rad, rotAxis);
            hexStartColor.a = Mathf.Lerp(hexStartColor.a, hexFinalColor.a, timeStep * fadeSpeed);
            borderStartColor.a = Mathf.Lerp(borderStartColor.a, borderFinalColor.a, timeStep * fadeSpeed);
            foreach (var tile in tiles)
            {
                Vector3 position = tile.ToVector3().SwapYZ();
                Draw.RegularPolygonBorder(position, 6, hexSize + borderOffset, borderThickness, angle * Mathf.Deg2Rad, borderStartColor);
                Draw.RegularPolygon(position, 6, hexSize, angle * Mathf.Deg2Rad, hexStartColor);
            }
        }
    }

    private bool IsPositionVisible(Camera cam, Vector3 position)
    {
        Vector3 viewportPoint = cam.WorldToViewportPoint(position);

        // Check if the point is in front of the camera
        if (viewportPoint.z < 0)
            return false;

        // Check if the point is within the camera's viewport rectangle (0 to 1 in x and y)
        return viewportPoint.x >= -0.2f && viewportPoint.x <= 1.2f &&
               viewportPoint.y >= -0.2f && viewportPoint.y <= 1.2f;
    }

    public void DrawHexTiles(HashSet<Hex3> tiles)
    {
        this.tiles = tiles;
    }

    [Button]
    public void StopDrawing()
    {
        tiles.Clear();
    }

    public void SetColors(Color hexColor, Color borderColor)
    {
        this.hexFinalColor = hexColor;
        this.borderFinalColor = borderColor;

        this.hexStartColor = hexColor;
        this.hexStartColor.a = 0f;

        this.borderStartColor = borderColor;
        this.borderStartColor.a = 0f;
    }

    [Button]
    public void DrawRange(Hex3 center, int min, int max)
    {
        this.tiles = Hex3.GetNeighborsAtDistance(center, min, max).ToHashSet();
    }

    public void AddRange(Hex3 center, int min, int max)
    {
        var newTiles = Hex3.GetNeighborsAtDistance(center, min, max);
        tiles.AddRange(newTiles);
    }

    private void UnitMoved(Hex3 hex1, Hex3 hex2, PlayerUnit unit)
    {
        StopDrawing();
    }
}
