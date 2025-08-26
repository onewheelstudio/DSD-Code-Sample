
using DG.Tweening;
using DG.Tweening.Core;
using HexGame.Grid;
using Sirenix.OdinInspector;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class CursorManager : MonoBehaviour
{
    [Required]
    [SerializeField]
    private Texture2D hexCursorTexture;    
    [InfoBox("Cursor scale set with const float")]
    [Required]
    [SerializeField]
    private GameObject cursor;
    private MeshRenderer cursorRenderer;
    private const float hexCursorScale = 0.19f;
    [SerializeField]
    [Range(0f, 0.1f)]
    private float verticalOffset = 0.01f;
    private Hex3 lastLocation;
    private AudioSource audioSource;

    [BoxGroup("Tween Settings")]
    [Range(0.01f, 1f)]
    [SerializeField]
    private float moveTime = 0.1f;

    [SerializeField, Required]
    private CursorInfoDictionary cursorInfoDictionary;
    [SerializeField]
    private Camera mainCamera;
    private CursorType cursorType;
    private UnitSelectionManager usm;
    private bool snapToHex = true;
    private Tween moveTween;

    private DOGetter<Vector3> getPosition;
    private DOSetter<Vector3> setPosition;

    private void Awake()
    {
        cursor.transform.localScale = hexCursorScale * Vector3.one;
        cursorRenderer = cursor.GetComponent<MeshRenderer>();
        SetCursor(CursorType.hex);
        audioSource = this.GetComponent<AudioSource>();
        usm = FindObjectOfType<UnitSelectionManager>();

        getPosition = () => cursor.transform.position;
        setPosition = x => cursor.transform.position = x;
    }

    private void OnDisable()
    {    
        DOTween.Kill(this,true);
        DOTween.Kill(cursor.transform,true);
    }

    void Update()
    {
        MoveCusor();

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            SetCursor(CursorType.hex);
            SetCursorColor(Color.white);
        }
    }

    private void MoveCusor()
    {
        if (cursor == null)
            return;

        Vector3 location = HelperFunctions.GetMouseVector3OnPlane(snapToHex, mainCamera);//.ToHex3();
        if(moveTween == null)
        {
            moveTween = DOTween.To(getPosition, setPosition, location + Vector3.up * verticalOffset, moveTime).SetUpdate(true);
        }
        else
        {
            moveTween.Kill();
            moveTween = DOTween.To(getPosition,  setPosition, location + Vector3.up * verticalOffset, moveTime).SetUpdate(true);
        }

        if (this.cursorType == CursorType.moveUnit)
            SetMoveCursorColor(location);

        lastLocation = cursor.transform.position;
    }

    private void SetMoveCursorColor(Hex3 location)
    {
        if (usm.IsValidPlacement(location, false))
            cursorRenderer.material.color = Color.white;
        else
            cursorRenderer.material.color = ColorManager.GetColor(ColorCode.red);
    }

    public void SetCursor(CursorType cursorType)
    {
        if(cursorRenderer == null)
            cursorRenderer = cursor.GetComponent<MeshRenderer>();

        cursorRenderer.enabled = true;

        if (cursorInfoDictionary.Cursors.TryGetValue(cursorType, out CursorInfo cursorInfo))
        {
            cursorRenderer.material = cursorInfo.cursorMaterial;
            this.cursorType = cursorType;
        }

        switch (cursorType)
        {
            case CursorType.target:
                snapToHex = false;
                break;
            case CursorType.hex:
            case CursorType.moveUnit:
            case CursorType.rallyPoint:
                snapToHex = true;
                break;
        }
    }

    public void SetCursorColor(Color color)
    {
        switch (cursorType)
        {
            case CursorType.target:
                cursorRenderer.material.SetColor("_BaseColor", color);
                break;
            default:
                cursorRenderer.material.color = color;
                break;
        }
    }

    public void SetProgress(float progress)
    {
        switch (cursorType)
        {
            case CursorType.target:
                cursorRenderer.material.SetFloat("_Progress", progress);
                break;
        }
    }

    public Vector3 CursorLocation()
    {
        return cursor.transform.position;
    }

    public void CursorOff()
    {
        cursorRenderer.enabled = false;
    }

    public void CursorOn()
    {
        cursorRenderer.enabled = true;
    }
}

public class CursorInfo
{
    public CursorType cursorType;
    public Material cursorMaterial;
}

public enum CursorType
{
    hex,
    target,
    moveUnit,
    rallyPoint,
}
