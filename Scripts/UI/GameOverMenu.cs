using HexGame.Units;
using System;

public class GameOverMenu : WindowPopup
{
    public static event Action GameOver;
    public static bool isGameOver = false;

    public override void OnEnable()
    {
        base.OnEnable();
        PlayerUnit.unitRemoved += CheckForGameOver;
        isGameOver = false;
        CloseWindow();
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PlayerUnit.unitRemoved -= CheckForGameOver;
    }
    private void CheckForGameOver(Unit unit)
    {
        if (DayNightManager.DayNumber == 0)
            return;

        if(unit is PlayerUnit playerUnit && playerUnit.unitType == PlayerUnitType.hq)
        {
            OpenWindow();
            GameOver?.Invoke();
            isGameOver = true;
        }
    }

    public override void CloseWindow()
    {
        if(!isGameOver)
            base.CloseWindow();
    }
}
