using DG.Tweening;
using HexGame.Grid;
using HexGame.Units;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FogRevealer : MonoBehaviour
{
    public static event Action<FogRevealer> fogAgentMoved;
    public event Action<FogRevealer> fogRevealDisabled;
    public int sightDistance = 2;
    protected static HexTileManager htm;

    [SerializeField]private bool canMove = false;
    private bool isMoving;
    List<Hex3> revealedLocations;
    private Hex3 currentLocation;

    private void Awake()
    {
        fogAgentMoved?.Invoke(this);

        if (this.transform.parent.TryGetComponent(out Unit unit))
            sightDistance = (int)unit.GetStat(Stat.sightDistance);
        else if(this.transform.root.TryGetComponent(out Unit rootUnit))
            sightDistance = (int)rootUnit.GetStat(Stat.sightDistance);

        if (htm == null)
            htm = FindObjectOfType<HexTileManager>();
    }

    private void OnEnable()
    {
        List<Hex3> neighbors = Hex3.GetNeighborsInRange(this.transform.position.ToHex3(), sightDistance);
        htm.AddFogAgent(neighbors, this);

        if (canMove)
            revealedLocations = neighbors;
    }

    private void OnDisable()
    {
        List<Hex3> neighbors = Hex3.GetNeighborsInRange(this.transform.position.ToHex3(), sightDistance);
        neighbors.Remove(this.transform.position.ToHex3()); //make sure we can see our own tile
        htm.RemoveFogAgent(neighbors, this);

        fogRevealDisabled?.Invoke(this);
        DOTween.Kill(this,true);
    }

    public void StartMove()
    {
        if(!canMove)
            return;

        isMoving = true;
        currentLocation = this.transform.position.ToHex3();
        StartCoroutine(CheckPosition());
    }

    private IEnumerator CheckPosition()
    {
        while(isMoving)
        {
            yield return null;
            if(currentLocation != this.transform.position.ToHex3())
            {
                currentLocation = this.transform.position.ToHex3();
                List<Hex3> newNeighbors = Hex3.GetNeighborsInRange(currentLocation, sightDistance);
                for (int i = revealedLocations.Count - 1; i >= 0; i--)
                {
                    if (newNeighbors.Contains(revealedLocations[i]))
                        revealedLocations.RemoveAt(i);
                }

                htm.AddFogAgent(newNeighbors, this, false);
                htm.RemoveFogAgent(new List<Hex3>(revealedLocations), this, false);
                revealedLocations = new List<Hex3>(newNeighbors);
            }
        }
    }

    public void UpdatePosition(Hex3 location)
    {
        if (currentLocation != location)
        {
            currentLocation = location;
            List<Hex3> newNeighbors = Hex3.GetNeighborsInRange(currentLocation, sightDistance);
            for (int i = revealedLocations.Count - 1; i >= 0; i--)
            {
                if (newNeighbors.Contains(revealedLocations[i]))
                    revealedLocations.RemoveAt(i);
            }

            htm.AddFogAgent(newNeighbors, this, false);
            htm.RemoveFogAgent(new List<Hex3>(revealedLocations), this, false);
            revealedLocations = new List<Hex3>(newNeighbors);
        }
    }

    public void CompleteMove()
    {
        isMoving = false;
        StopAllCoroutines();
    }

    public void UpdateSightDistance()
    {
        if (this.transform.parent.TryGetComponent(out Unit unit))
            sightDistance = (int)unit.GetStat(Stat.sightDistance);
        else if (this.transform.root.TryGetComponent(out Unit rootUnit))
            sightDistance = (int)rootUnit.GetStat(Stat.sightDistance);

        List<Hex3> newNeighbors = Hex3.GetNeighborsInRange(currentLocation, sightDistance);
        if(revealedLocations != null)
        {
            for (int i = revealedLocations.Count - 1; i >= 0; i--)
            {
                if (newNeighbors.Contains(revealedLocations[i]))
                    revealedLocations.RemoveAt(i);
            }
        }

        htm.AddFogAgent(newNeighbors, this, false);
        if(revealedLocations != null)
            htm.RemoveFogAgent(new List<Hex3>(revealedLocations), this, false);
        revealedLocations = new List<Hex3>(newNeighbors);
    }

} 
