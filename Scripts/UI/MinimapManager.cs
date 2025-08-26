using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NovaSamples.UIControls;
using DG.Tweening;
using System;
using Sirenix.OdinInspector;

public class MinimapManager : MonoBehaviour
{
    [Header("Nova Buttons")]
    [SerializeField, Required] private Button zoomInButton;
    [SerializeField, Required] private Button zoomOutButton;
    [SerializeField, Required] private Button goToOriginButton;

    [Header("Minimap Layer Buttons")]
    [SerializeField, Required] private Button enemyLayerButton;
    [SerializeField, Required] private Button playerLayerButton;
    [SerializeField, Required] private Button hextileLayerButton;

    [Header("Other Goodies")]
    [SerializeField, Range(1f,20f)] private float zoomAmount = 5;
    [SerializeField, Required] private Camera minimapCamera;
    public float Size => minimapCamera.orthographicSize;
    [SerializeField] private float minSize = 10;
    [SerializeField] private float maxSize = 60;
    [SerializeField] private float tweenTime = 0.2f;

    public static System.Action<Vector3> moveCamera;

    private void OnEnable()
    {
        zoomInButton.Clicked += ZoomIn;
        zoomOutButton.Clicked += ZoomOut;
        goToOriginButton.Clicked += MoveToOrigin;

        MinimapControls.minimapClicked += MiniMapClicked;
        enemyLayerButton.Clicked += ToggleMinimapEnemyLayer;
        playerLayerButton.Clicked += ToggleMinimapPlayerLayer;
        hextileLayerButton.Clicked += ToggleMinimapTileLayer;
    }

    void OnDisable()
    {
        zoomInButton.Clicked -= ZoomIn;
        zoomOutButton.Clicked -= ZoomOut;
        goToOriginButton.Clicked -= MoveToOrigin;

        MinimapControls.minimapClicked -= MiniMapClicked;
        DOTween.Kill(this,true);
    }

    private void MoveToOrigin()
    {
        moveCamera?.Invoke(Vector3.zero);
    }

    private void MiniMapClicked(Vector3 location)
    {
        location = Quaternion.Euler(0f, minimapCamera.transform.parent.rotation.eulerAngles.y, 0f) * location.SwapYZ(); //rotate vector to match camera
        Vector3 startPoint = location * minimapCamera.orthographicSize + minimapCamera.transform.position;
        Ray ray = new Ray(startPoint, Vector3.down);
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        if(plane.Raycast(ray, out float distance))
            moveCamera?.Invoke(ray.GetPoint(distance));
    }


    private void ZoomIn()
    {
        float size = minimapCamera.orthographicSize;
        size -= zoomAmount;

        if (size <= minSize)
            minimapCamera.DOOrthoSize(minSize, tweenTime);
        else
            minimapCamera.DOOrthoSize(size, tweenTime);
    }

    private void ZoomOut()
    {
        float size = minimapCamera.orthographicSize;
        size += zoomAmount;

        if (size >= maxSize)
            minimapCamera.DOOrthoSize(maxSize, tweenTime);
        else
            minimapCamera.DOOrthoSize(size, tweenTime);
    }

    private void ToggleMinimapEnemyLayer()
    {
        minimapCamera.cullingMask ^= 1 << 14; //toggle layer
    }

    private void ToggleMinimapPlayerLayer()
    {
        minimapCamera.cullingMask ^= 1 << 15; //toggle layer
    }

    private void ToggleMinimapTileLayer()
    {
        minimapCamera.cullingMask ^= 1 << 16; //toggle layer
    }


}
