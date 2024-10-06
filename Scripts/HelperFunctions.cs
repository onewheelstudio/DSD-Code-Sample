using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using HexGame.Grid;
using HexGame.Resources;
using HexGame.Units;
using Nova;
using System.Collections.Generic;
using Unity.Burst;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

[BurstCompile]
public static class HelperFunctions
{
    public static Hex3 ToHex3(this Vector3 position)
    {
        float SQRT3 = 1.73205080757f;
        float q = SQRT3 / 3f * position.x + position.z / 3f;
        float r = -2f / 3f * position.z;
        float s = -r - q;

        return Hex3.Hex3Round(q, r, s);
    }

    public static int HexRange(Vector3 position1, Vector3 position2)
    {
        return Hex3.DistanceBetween(position1, position2);
    }

    //public static float HexRangeFloat(Vector3 position1, Vector3 position2)
    //{
    //    return HexRangeFloat(ref position1, ref position2);
    //    //return Vector3.Distance(position1, position2) / Hex3.SQRT3;
    //}

    public static float HexRangeFloat(Vector3 position1, Vector3 position2)
    {
        return Mathf.RoundToInt(Vector3.Distance(position1, position2) / Hex3.SQRT3);
    }

    /// <summary>
    /// Returns the max value of the three coordinates. Or the distance from the origin.
    /// </summary>
    /// <param name="hex3"></param>
    /// <returns></returns>
    public static int Max(this Hex3 hex3)
    {
        return Mathf.Max(Mathf.Abs(hex3.q), Mathf.Abs(hex3.r), Mathf.Abs(hex3.s));
    }

    public static int Min(this Hex3 hex3)
    {
        return Mathf.Min(Mathf.Abs(hex3.q), Mathf.Abs(hex3.r), Mathf.Abs(hex3.s));
    }

    public static Hex3 GetMouseHex3OnPlane()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        Vector3 direction = ray.direction;
        Vector3 startPt = Camera.main.transform.position;
        float t = -startPt.y / direction.y;

        return Hex3.Vector3ToHex3(GetMouseVector3OnPlane(false));
    }

    public static Vector3 GetMouseVector3OnPlane(bool roundToNearestHex, Camera camera = null)
    {
        if (camera == null)
            camera = Camera.main;

        Ray ray = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
        float t = -camera.transform.position.y / ray.direction.y;

        if (roundToNearestHex)
            return Hex3.Hex3ToVector3(Hex3.Vector3ToHex3(ray.direction * t + camera.transform.position));
        else
            return ray.direction * t + camera.transform.position;
    }

    public static Vector3 GetMouseVector3OnPlane()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        Vector3 direction = ray.direction;
        Vector3 startPt = Camera.main.transform.position;
        float t = -startPt.y / direction.y;

        return direction * t + startPt;
    }

    public static (Vector3, float) GetClosestHexEdge()
    {
        List<Vector3> edgeMidpoints = Hex3.GetEdgeMidPoints(GetMouseHex3OnPlane());
        Vector3 mouseInWorld = GetMouseVector3OnPlane(false);
        Vector3 position = mouseInWorld;
        float distance = Mathf.Infinity;
        float rotation = 0;

        for (int i = 0; i < 6; i++)
        {
            if ((edgeMidpoints[i] - mouseInWorld).sqrMagnitude < distance)
            {
                position = edgeMidpoints[i];
                distance = (edgeMidpoints[i] - mouseInWorld).sqrMagnitude;

                switch (i)
                {
                    case 0:
                    case 3:
                        rotation = 30;
                        break;
                    case 1:
                    case 4:
                        rotation = 90;
                        break;
                    case 2:
                    case 5:
                        rotation = -30;
                        break;
                }
            }
        }

        return (position, rotation);
    }

    public static Vector3 GetMousePoint(Camera camera = null)
    {
        camera = camera == null ? Camera.main : camera;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
            return hit.point;
        else
            return Vector3.zero;
    }

    public static void UpdatePathfindingGrid(GameObject gameObject, bool walkable = true, uint penalty = 0, int tag = 0)
    {
        AstarPath.active.AddWorkItem(new Pathfinding.AstarWorkItem(() =>
        {
            // Safe to update graphs here
            var node = AstarPath.active.GetNearest(gameObject.transform.position).node;

            node.Walkable = walkable;
            node.Penalty = penalty;
            node.Tag = (uint)tag;
        }));
    }

    public static T PullFirst<T>(this List<T> list)
    {
        T t = list[0];
        list.RemoveAt(0);
        return t;
    }

    public static T PullAtIndex<T>(this List<T> list, int index)
    {
        T t = list[index];
        list.RemoveAt(index);
        return t;
    }

    public static void MoveToLast<T>(this List<T> list, T t)
    {
        list.Remove(t);
        list.Add(t);
    }

    public static List<ScriptableObject> GetScriptableObjects(string path)
    {
#if UNITY_EDITOR

        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ScriptableObject", new[] { path });
        List<ScriptableObject> prefabs = new List<ScriptableObject>();

        foreach (var guid in guids)
        {
            UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            prefabs.Add(UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, typeof(ScriptableObject)) as ScriptableObject);
        }

        return prefabs;
#else
        return new List<ScriptableObject>();
#endif
    }

    public static List<GameObject> GetPrefabs(string path)
    {
#if UNITY_EDITOR

        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:prefab", new[] { path });
        List<GameObject> prefabs = new List<GameObject>();

        foreach (var guid in guids)
        {
            UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            prefabs.Add(UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)) as GameObject);
        }

        return prefabs;
#else
        return new List<GameObject>();
#endif
    }

    public static List<T> GetScriptableObjects<T>(string path) where T : ScriptableObject
    {
#if UNITY_EDITOR


        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:" + typeof(T).ToString(), new[] { path });
        List<T> scriptableObjects = new List<T>();

        foreach (var guid in guids)
        {
            UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            scriptableObjects.Add(UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, typeof(T)) as T);
        }

        return scriptableObjects;
#else
        return null;
#endif
    }

    public static List<GameObject> GetTiles(string path, bool getPlaceHolder = false)
    {
        List<GameObject> tiles = GetPrefabs(path);
        List<GameObject> newList = new List<GameObject>();
        foreach (var tile in tiles)
        {
            if (tile.GetComponent<HexTile>() != null && tile.GetComponent<HexTile>().isPlaceHolder == getPlaceHolder)
                newList.Add(tile);
        }

        return newList;
    }

    //Gets all event system raycast results of current mouse or touch position.
    private static List<UnityEngine.EventSystems.RaycastResult> GetEventSystemRaycastResults()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Mouse.current.position.ReadValue();
        List<RaycastResult> raysastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raysastResults);
        return raysastResults;
    }

    public static DG.Tweening.Tween DOColor(this Nova.UIBlock2D uIBlock2D, Color finalColor, float time)
    {
        return DG.Tweening.DOTween.To(() => uIBlock2D.Color, x => uIBlock2D.Color = x, finalColor, time);
    }

    public static string ToNiceString(this HexGame.Resources.ResourceType value, bool plural = true)
    {
        if(plural)
            return value.ToNiceStringPlural();

        switch (value)
        {
            case ResourceType.FeOre:
                return "Iron Ore";
            case ResourceType.Food:
                break;
            case ResourceType.Workers:
                return "Worker";
            case ResourceType.Energy:
                return "Fuel Cell";
            case ResourceType.TiOre:
                return "Titanium Ore";
            case ResourceType.Gas:
                break;
            case ResourceType.Thermite:
                break;
            case ResourceType.Terrene:
                return "Terrene";
            case ResourceType.AlOre:
                return "Alunimum Ore";
            case ResourceType.UOre:
                return "Uranium Ore";
            case ResourceType.Oil:
                break;
            case ResourceType.Carbon:
                break;
            case ResourceType.Water:
                break;
            case ResourceType.BioWaste:
                return "Bio Waste";
            case ResourceType.IndustrialWaste:
                return "Industrial Waste";
            case ResourceType.FeIngot:
                return "Iron";
            case ResourceType.AlIngot:
                return "Alunimum";
            case ResourceType.TiIngot:
                return "Titanium";
            case ResourceType.UIngot:
                return "Uranium";
            case ResourceType.SteelPlate:
                return "Steel Plate";
            case ResourceType.SteelCog:
                return "Steel Cog";
            case ResourceType.AlPlate:
                return "Alunimum Plate";
            case ResourceType.AlCog:
                return "Alunimum Cog";
            case ResourceType.Hydrogen:
                break;
            case ResourceType.Nitrogen:
                break;
            case ResourceType.Oxygen:
                break;
            case ResourceType.AmmoniumNitrate:
                return "Ammonium Nitrate";
            case ResourceType.cuOre:
                return "Coppper Ore";
            case ResourceType.cuIngot:
                return "Copper";
            case ResourceType.CannedFood:
                return "Canned Food";
            case ResourceType.FuelRod:
                return "Fuel Rod";
            case ResourceType.WeaponsGradeUranium:
                return "Weapons Grade Uranium";
            case ResourceType.ExplosiveShell:
                return "Explosive Shell";
            case ResourceType.Sulfer:
                break;
            case ResourceType.Plastic:
                break;
            case ResourceType.CarbonFiber:
                return "Carbon Fiber";
            case ResourceType.Sand:
                break;
            case ResourceType.Electronics:
                break;
            case ResourceType.UraniumShells:
                return "Uranium Shell";
            case ResourceType.SulfuricAcid:
                return "Sulfuric Acid";
            default:
                break;
        }

        return value.ToString();
    }

    private static string ToNiceStringPlural(this HexGame.Resources.ResourceType value)
    {
        switch(value)
        {

            case ResourceType.Workers:
                return "Workers";
            case ResourceType.Energy:
                return "Fuel Cells";
            case ResourceType.SteelPlate:
                return "Steel Plates";
            case ResourceType.SteelCog:
                return "Steel Cogs";
            case ResourceType.AlPlate:
                return "Alunimum Plates";
            case ResourceType.AlCog:
                return "Alunimum Cogs";
            case ResourceType.FuelRod:
                return "Fuel Rods";
            case ResourceType.ExplosiveShell:
                return "Explosive Shells";
            case ResourceType.UraniumShells:
                return "Uranium Shells";
            default:
                return ToNiceString(value, false);
        }
    }

    public static string ToNiceString(this HexGame.Units.PlayerUnitType value)
    {
        switch (value)
        {
            case HexGame.Units.PlayerUnitType.hq:
                return "HQ";
            case HexGame.Units.PlayerUnitType.doubleTower:
                return "Laser Tower II";
            case HexGame.Units.PlayerUnitType.artillery:
                return "Artillery";
            case HexGame.Units.PlayerUnitType.tank:
                return "Tank";
            case HexGame.Units.PlayerUnitType.housing:
                return "Housing";
            case HexGame.Units.PlayerUnitType.farm:
                return "Farm";
            case HexGame.Units.PlayerUnitType.mine:
                return "Mine";
            case HexGame.Units.PlayerUnitType.deepMine:
                return "Deep Mine";
            case HexGame.Units.PlayerUnitType.gasRefinery:
                return "Gas Collector";
            case HexGame.Units.PlayerUnitType.smelter:
                return "Smelter";
            case HexGame.Units.PlayerUnitType.repair:
                return "Repair Hub";
            case HexGame.Units.PlayerUnitType.spaceLaser:
                return "Space Laser";
            case HexGame.Units.PlayerUnitType.storage:
                return "Storage";
            case HexGame.Units.PlayerUnitType.powerPlant:
                return "Power Plant";
            case HexGame.Units.PlayerUnitType.solarPanel:
                return "Solar Panel";
            case HexGame.Units.PlayerUnitType.cargoShuttle:
                return "Cargo Shuttle";
            case HexGame.Units.PlayerUnitType.shuttlebase:
                return "Shuttle Hanger";
            case HexGame.Units.PlayerUnitType.spaceElevator:
                return "Space Elevator";
            case HexGame.Units.PlayerUnitType.buildingSpot:
                return "Construction Site";
            case HexGame.Units.PlayerUnitType.missileTower:
                return "Missile Tower";
            case HexGame.Units.PlayerUnitType.missileSilo:
                return "Missile Silo";
            case HexGame.Units.PlayerUnitType.bomberBase:
                return "Bomber Base";
            case HexGame.Units.PlayerUnitType.infantry:
                return "Infantry";
            case HexGame.Units.PlayerUnitType.singleTower:
                return "Laser Tower I";
            case HexGame.Units.PlayerUnitType.barracks:
                return "Barracks";
            case HexGame.Units.PlayerUnitType.waterPump:
                return "Water Pump";
            case HexGame.Units.PlayerUnitType.oilPlatform:
                return "Oil Platform";
            case HexGame.Units.PlayerUnitType.factory:
                return "Factory";
            case HexGame.Units.PlayerUnitType.scoutTower:
                return "Sentry Tower";
            case PlayerUnitType.landMine:
                return "Land Mine";
            case PlayerUnitType.supplyShip:
                return "Supply Ship";
            case PlayerUnitType.collectionTower:
                return "Collection Tower";
            case PlayerUnitType.chemicalPlant:
                return "Chemical Plant";
            case PlayerUnitType.pub:
                return "Pub";
            case PlayerUnitType.foundry:
                return "Foundry";
            case PlayerUnitType.nuclearPlant:
                return "Nuclear Plant";
            case PlayerUnitType.centrifuge:
                return "Centrifuge";
            case PlayerUnitType.sandPit:
                return "Sand Pit";
            case PlayerUnitType.atmosphericCondenser:
                return "Atmospheric Condenser";
            default:
                break;
        }

        return value.ToString();
    } 
    
    public static string ToNiceStringPlural(this HexGame.Units.PlayerUnitType value)
    {
        switch (value)
        {
            case HexGame.Units.PlayerUnitType.hq:
                return "HQ";
            case HexGame.Units.PlayerUnitType.doubleTower:
                return "Laser Tower II";
            case HexGame.Units.PlayerUnitType.tank:
                return "Tanks";
              case HexGame.Units.PlayerUnitType.farm:
                return "Farms";
            case HexGame.Units.PlayerUnitType.mine:
                return "Mines";
            case HexGame.Units.PlayerUnitType.deepMine:
                return "Deep Mines";
            case HexGame.Units.PlayerUnitType.gasRefinery:
                return "Gas Collectors";
            case HexGame.Units.PlayerUnitType.smelter:
                return "Smelters";
            case HexGame.Units.PlayerUnitType.repair:
                return "Repair Hubs";
            case HexGame.Units.PlayerUnitType.spaceLaser:
                return "Space Lasers";
            case HexGame.Units.PlayerUnitType.storage:
                return "Storage Units";
            case HexGame.Units.PlayerUnitType.powerPlant:
                return "Power Plants";
            case HexGame.Units.PlayerUnitType.solarPanel:
                return "Solar Panels";
            case HexGame.Units.PlayerUnitType.cargoShuttle:
                return "Cargo Shuttles";
            case HexGame.Units.PlayerUnitType.shuttlebase:
                return "Shuttle Hangers";
            case HexGame.Units.PlayerUnitType.spaceElevator:
                return "Space Elevators";
            case HexGame.Units.PlayerUnitType.buildingSpot:
                return "Construction Sites";
            case HexGame.Units.PlayerUnitType.missileTower:
                return "Missile Towers";
            case HexGame.Units.PlayerUnitType.missileSilo:
                return "Missile Silos";
            case HexGame.Units.PlayerUnitType.bomberBase:
                return "Bomber Bases";
            case HexGame.Units.PlayerUnitType.singleTower:
                return "Laser Tower I";
            case HexGame.Units.PlayerUnitType.barracks:
                return "Barracks";
            case HexGame.Units.PlayerUnitType.waterPump:
                return "Water Pumps";
            case HexGame.Units.PlayerUnitType.oilPlatform:
                return "Oil Platforms";
            case HexGame.Units.PlayerUnitType.factory:
                return "Factories";
            case HexGame.Units.PlayerUnitType.scoutTower:
                return "Sentry Towers";
            case PlayerUnitType.landMine:
                return "Land Mines";
            case PlayerUnitType.supplyShip:
                return "Supply Ships";
            case PlayerUnitType.collectionTower:
                return "Collection Towers";
            case PlayerUnitType.chemicalPlant:
                return "Chemical Plants";
            case PlayerUnitType.pub:
                return "Pubs";
            case PlayerUnitType.foundry:
                return "Foundries";
            case PlayerUnitType.nuclearPlant:
                return "Nuclear Plants";
            case PlayerUnitType.centrifuge:
                return "Centrifuges";
            case PlayerUnitType.sandPit:
                return "Sand Pits";
            case PlayerUnitType.atmosphericCondenser:
                return "Atmospheric Condensers";
        }

        return value.ToNiceString();
    }

    public static string ToNiceString(this Stat stat)
    {
        switch (stat)
        {
            case Stat.hitPoints:
                return "Hit Points";
            case Stat.shield:
                return "Shield";
            case Stat.speed:
                return "Speed";
            case Stat.movementRange:
                return "Movement Range";
            case Stat.happiness:
                return "Compliance";
            case Stat.minRange:
                return "Min Range";
            case Stat.maxRange:
                return "Max Range";
            case Stat.reloadTime:
                return "Reload Time";
            case Stat.damage:
                return "Damage";
            case Stat.aoeRange:
                return "Damge Range";
            case Stat.reputation:
                break;
            case Stat.maxStorage:
                return "Max Storage";
            case Stat.burst:
                return "Burst";
            case Stat.workers:
                return "Max Workers";
            case Stat.housing:
                return "Housing Capacity";
            case Stat.sightDistance:
                return "Sight Distance";
            case Stat.charges:
                return "Charges";
            default:
                break;
        }

        return stat.ToString();
    }

    public static string ToNiceString(this EnemyUnitType unitType)
    {
        switch (unitType)
        {
            case EnemyUnitType.serpent:
                return "Ground Unit";
            case EnemyUnitType.flying:
                return "Flying Unit";
            case EnemyUnitType.structure:
                return "Enemy Structure";
            case EnemyUnitType.serpentElite:
                return "Elite Ground Unit";
            //case EnemyUnitType.other:
            //    return "Some other enemy unit type";
            default:
                break;
        }

        return unitType.ToNiceString();
    }

    public static T GetRandomEnumValue<T>() where T : System.Enum
    {
        var v = System.Enum.GetValues(typeof(T));
        return (T)v.GetValue(HexTileManager.GetNextInt(0, v.Length));
    }

    public static string ToNiceString(this HexGame.Resources.HexTileType value)
    {
        switch (value)
        {
            case HexTileType.feOre:
                return "Iron Ore";
            case HexTileType.grass:
                return "Grass";
            case HexTileType.mountain:
                return "Mountain";
            case HexTileType.forest:
                return "Forest";
            case HexTileType.water:
                return "Water";
            case HexTileType.alOre: 
                return "Alunimum Ore";
            case HexTileType.gas:   
                return "Gas";
            case HexTileType.tiOre: 
                return "Titanium Ore";
            case HexTileType.uOre:
                return "Uranium Ore";
            case HexTileType.oil:   
                return "Oil";
            case HexTileType.sand:
                return "Sand";
            case HexTileType.aspen:
                return "Aspen Forest";
            case HexTileType.cuOre:
                return "Copper Ore";
            default:
                break;
        }

        return value.ToString();
    }


    public static void ToggleActive(this GameObject gameObject)
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }

    public static TweenerCore<Color, Color, ColorOptions> DOFade(this Shapes.Polyline target, float endValue, float duration)
    {
        TweenerCore<Color, Color, ColorOptions> t = DOTween.ToAlpha(() => target.Color, x => target.Color = x, endValue, duration);
        t.SetTarget(target);
        return t;
    }

    public static TweenerCore<Color, Color, ColorOptions> DOFade(this Shapes.Polygon target, float endValue, float duration)
    {
        TweenerCore<Color, Color, ColorOptions> t = DOTween.ToAlpha(() => target.Color, x => target.Color = x, endValue, duration);
        t.SetTarget(target);
        return t;
    }

    //public static AnimationHandle DoFade(this Nova.ClipMask clipMask, float endValue, float duration)
    //{
    //    if (clipMask != null)
    //    {
    //        ClipMaskAlphaAnimation clipMaskAlphaAnimation = new ClipMaskAlphaAnimation(clipMask, endValue);
    //        return clipMaskAlphaAnimation.Run(duration);
    //    }

    //    UnityEngine.Debug.LogError("Missing Clip Mask");
    //    return new AnimationHandle();
    //}

    public static Tween DoRadius(this SphereCollider sphereCollider, float endValue, float duration)
    {
        Tween tween = DOTween.To(() => sphereCollider.radius, x => sphereCollider.radius = x, endValue, duration);
        return tween;
    }

    public static Tween DoIntensity(this Vignette vignette, float endValue, float duration)
    {
        Tween tween = DOTween.To(() => vignette.intensity.value, x => vignette.intensity.value = x, endValue, duration);
        return tween;
    }

    public static float GetMaxRangeInHex(this HexGame.Units.UnitBehavior unit)
    {
        return (Hex3.SQRT3 / 2f * (1 + 2 * unit.GetStat(Stat.maxRange))) / unit.transform.localScale.x;
    }

    public static float GetMinRangeInHex(this HexGame.Units.UnitBehavior unit)
    {
        return (Hex3.SQRT3 / 2f * (1 + 2 * unit.GetStat(Stat.minRange))) / unit.transform.localScale.x;
    }

    public static bool TryGetComponentInParent<T>(this Transform transform, out T component, bool recursive = true) where T : Component
    {
        if (transform.parent == null)
        {
            component = null;
            return false;
        }

        component = transform.GetComponentInParent<T>();

        if (recursive == false || component != null)
            return component != null;
        else
            return transform.parent.TryGetComponentInParent(out component, recursive);
    }


    /// <summary>
    /// Requires exposes parameter to work.
    /// Takes in values from 0.0001 to 1 and converts to decibels
    /// </summary>
    /// <param name="mixer"></param>
    /// <param name="value"></param>
    /// <param name="parameter"></param>
    public static void SetVolume(this AudioMixer mixer, float value, string parameter = "Volume")
    {
        mixer.SetFloat(parameter, Mathf.Log10(value) * 20);
    }

    public static List<Tween> HighlightUIBlock(UIBlock2D uiBlock, float calloutTime = 0.75f, float calloutSize = 1.05f)
    {
        if (uiBlock == null)
            return null;

        List<Tween> tweens = new List<Tween>();
        Tween tween = uiBlock.DoScale(Vector3.one * calloutSize, calloutTime)
                            .SetLoops(-1, LoopType.Yoyo);
        tweens.Add(tween);

        tween = uiBlock.DOColor(ColorManager.GetColor(ColorCode.callOut), calloutTime)
                    .SetLoops(-1, LoopType.Yoyo);
        tweens.Add(tween);

        return tweens;
    }
}
