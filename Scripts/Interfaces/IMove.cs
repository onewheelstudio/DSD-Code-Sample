using HexGame.Grid;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMove 
{
    void ToggleReadyToMove();
    void StartMove();
    void DoMove(Hex3 location);
    void CancelMove();
    bool ReadyToMove { get; }
    bool UnitsAreMoving { get; }
}
