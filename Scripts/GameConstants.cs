public static class GameConstants
{
    public static string preferencesPath = "GamePrefereneces.es3";   
    public static float recoverResourcePercent = 1f;
    internal static string techCredits = "TechCredits";
    internal static string StatsPath = "StatsInfo.es3";
    internal static string totalTechCreditsCollected = "TotalTechCreditsCollected";

    public static int upgradeCostMultiplier = 500;
    public static float timePerShipment = 2f;
    private static float _gameSpeed = 1f;
    public static float GameSpeed
    {
        get
        {
            if (DayNightManager.isNight)
                return 1f;
            else 
                return _gameSpeed;
        }
        set
        {
            if(value > 0.5f)
                _gameSpeed = value;
        }
    }
    public static int infantryCost = 2500;
    public static int infantryCostIncrease = 500;
}
