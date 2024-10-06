using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using HexGame.Grid;

public class OnClickTest : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        

        //Debug.Log($"Clicked on {this.gameObject.name} at {eventData.position}");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"Clicked on {this.gameObject.name} at {eventData.position}");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log($"Button Up at {eventData.position} {(Hex3)eventData.pointerCurrentRaycast.worldPosition}");
    }
}
