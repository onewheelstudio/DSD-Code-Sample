using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class ZoomWithScroll : MonoBehaviour
{
    [SerializeField,Range(0.01f,2f)] private float zoomRate = 1f;
    [SerializeField,Range(1f,50f)] private float lerpSpeed = 1f;
    private Vector3 targetScale;
    private float maxScale = 1f;
    private float minScale = 0.4f;
    private bool canMove = false;
    private float timeStep;

    private UIControlActions uiActions;

    private void Awake()
    {
        targetScale = this.transform.localScale;
        timeStep = Time.deltaTime;
        uiActions = new UIControlActions();
    }

    private void OnEnable()
    {
        HexTechTree.techTreeOpen += TechTreeOpen;
        uiActions.UI.MouseZoom.Enable();
    }



    private void OnDisable()
    {
        HexTechTree.techTreeOpen -= TechTreeOpen;
        uiActions.UI.MouseZoom.Disable();
    }

    private void TechTreeOpen(bool techTreeIsOpen)
    {
        canMove = techTreeIsOpen;
    }

    private void Update()
    {
        if (!canMove)
            return;

        if (Time.timeScale > 0)
            timeStep = Time.deltaTime;

        // Get the current scroll wheel delta
        Vector2 mouseScrollDelta = Mouse.current.scroll.ReadValue() * 120;

        if (DistanceFromTarget() > 0.01f)
        {
            Vector3 newScale = Vector3.Lerp(this.transform.localScale, targetScale, timeStep * lerpSpeed);
            this.transform.localScale = newScale;
        }

        if (mouseScrollDelta.y == 0)
            return;

        targetScale += mouseScrollDelta.y * zoomRate * timeStep * Vector3.one;

        if (targetScale.x < minScale)
            targetScale = minScale * Vector3.one;
        else if (targetScale.x > maxScale)
            targetScale = maxScale * Vector3.one;

    }


    private float DistanceFromTarget()
    {
        return Mathf.Abs(targetScale.x - this.transform.localScale.x);
    }
}
