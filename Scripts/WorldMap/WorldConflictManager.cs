using HexGame.Grid;
using Nova;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorldConflictManager : MonoBehaviour
{
    [SerializeField] private WorldMapGenerator wmg;
    private MapTiles mapTiles => wmg.MapTiles;

    [Header("Map Visuals")]
    [SerializeField] private Color neutralColor;
    [SerializeField] private Color enemyColor;
    [SerializeField] private Color playerColor;
    [SerializeField] private Color contestedColor;

    [Header("Enemy Settings")]
    [SerializeField, Range(0.01f,1f)] private float enemyGrowthRate = 0.05f;
    [SerializeField, Range(0.01f,1f)] private float enemyKillRate = 0.10f;
    [SerializeField] private int enemyExpandThreshold = 300;
    [SerializeField, Range(0.01f,1f)] private float enemyExpandPercent = 0.2f;
    [SerializeField] private int maxEnemyForces = 1000;
    [SerializeField, Range(1, 8)] private int enemyStartSectors = 3;
    [SerializeField, Range(3, 20)] private int minDistanceToPlayer = 5;


    [Header("Player Settings")]
    [SerializeField, Range(0.01f,1f)] private float playerGrowthRate = 0.03f;
    [SerializeField, Range(0.01f,1f)] private float playerKillRate = 0.15f;
    [SerializeField] private int playerExpandThreshold = 300;
    [SerializeField, Range(0.01f,1f)] private float playerExpandPercent = 0.2f;
    [SerializeField] private int maxPlayerForces = 1000;

    private UIMapTile enemyStart;
    private UIMapTile playerStart;
    private Vector3 enemyToPlayer => playerStart.transform.position - enemyStart.transform.position;

    private List<UIMapTile> enemyTiles = new();
    private List<UIMapTile> playerTiles = new();

    [Header("Progress")]
    [SerializeField] private UIBlock2D enemyProgres;
    [SerializeField] private UIBlock2D playerProgress;
    [SerializeField] private TextBlock playerStatus;

    [Button]
    private void DoSimulationTicks(int ticks = 20)
    {
        for (int i = 0; i < ticks; i++)
        {
            DoSimulationTick();
        }
    }

    [Button]
    private void DoSimulationTick()
    {
        DoCombat(playerTiles); //only need tiles with potential conflict

        GrowEnemyForces(enemyTiles);
        GrowPlayerForces(playerTiles);

        DoEnemyExpand(enemyTiles);
        DoPlayerExpand(playerTiles);

        DoResourceTick(playerTiles);

        UpdateProgressBar(mapTiles.tiles);
        UpdateTileColors(mapTiles.tiles);
    }


    private void GrowEnemyForces(List<UIMapTile> tiles)
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            if (tiles[i].levelData.enemyForces == 0)
                continue;
            else if (tiles[i].levelData.enemyForces <= 0)
            {
                tiles[i].levelData.enemyForces = 0;
                continue;
            }

            float growthRate = Mathf.Lerp(enemyGrowthRate, 0f, tiles[i].levelData.enemyForces / maxEnemyForces);
            int growth = Mathf.RoundToInt(tiles[i].levelData.enemyForces * growthRate);
            if(growth == 0)
                growth = 1;
            tiles[i].levelData.enemyForces = Mathf.Clamp(tiles[i].levelData.enemyForces + growth, 0, maxEnemyForces);
        }
    }

    private void GrowPlayerForces(List<UIMapTile> tiles)
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            if (tiles[i].levelData.playerForces == 0)
                continue;
            else if (tiles[i].levelData.playerForces <= 0)
            {
                tiles[i].levelData.playerForces = 0;
                continue;
            }

            int growth = Mathf.RoundToInt(tiles[i].levelData.playerForces * playerGrowthRate);
            if (growth == 0)
                growth = 1;
            tiles[i].levelData.playerForces = Mathf.Clamp(tiles[i].levelData.playerForces + growth, 0, maxPlayerForces);
        }
    }

    private void DoCombat(List<UIMapTile> tiles)
    {
        for(int i = 0; i < tiles.Count; i++)
        {
            UIMapTile tile = tiles[i];
            if (!tile.levelData.isActive)
                continue;

            if (tile.levelData.enemyForces == 0 || tile.levelData.playerForces == 0)
                continue;

            int enemyDeaths = Mathf.RoundToInt(playerKillRate * tile.levelData.playerForces);
            int playerDeaths = Mathf.RoundToInt(enemyKillRate * tile.levelData.enemyForces);

            tile.levelData.enemyForces = Mathf.Max(0, tile.levelData.enemyForces - enemyDeaths);
            tile.levelData.playerForces = Mathf.Max(0, tile.levelData.playerForces - playerDeaths);
        }
    }

    private void UpdateTileColors(List<UIMapTile> tiles)
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            if (tiles[i].levelData.enemyForces > 0 && tiles[i].levelData.playerForces > 0)
            {
                tiles[i].levelData.SetControl(SectorControl.Contested);
                tiles[i].SetColor(contestedColor);
            }
            else if (tiles[i].levelData.enemyForces > 0)
            {
                tiles[i].levelData.SetControl(SectorControl.Enemy);
                tiles[i].SetColor(enemyColor);
            }
            else if (tiles[i].levelData.playerForces > 0)
            {
                tiles[i].levelData.SetControl(SectorControl.Player);
                tiles[i].SetColor(playerColor);
            }
            else
            {
                tiles[i].levelData.SetControl(SectorControl.NONE);
                tiles[i].SetColor(neutralColor);
            }
        }
    }

    private void DoEnemyExpand(List<UIMapTile> enemyTiles)
    {
        for (int i = 0; i < enemyTiles.Count; i++)
        {
            if (enemyTiles[i].levelData.enemyForces > enemyExpandThreshold &&
                enemyTiles[i].levelData.playerForces == 0)
            {
                List<UIMapTile> uncontrolled = enemyTiles[i].levelData.GetNeighborsControlledBy(SectorControl.NONE);
                uncontrolled.AddRange(enemyTiles[i].levelData.GetNeighborsControlledBy(SectorControl.Player));
                if(uncontrolled.Count == 0)
                    continue;
                UIMapTile nextTile = uncontrolled[Random.Range(0, uncontrolled.Count)];

                if(nextTile.levelData.playerForces > 0)
                {
                    nextTile.levelData.SetControl(SectorControl.Contested);
                    nextTile.SetColor(contestedColor);
                }
                else
                {
                    nextTile.levelData.SetControl(SectorControl.Enemy);
                    nextTile.SetColor(enemyColor);
                }
                
                int forcesToTransfer = Mathf.RoundToInt(enemyTiles[i].levelData.enemyForces * enemyExpandPercent);
                enemyTiles[i].levelData.enemyForces -= forcesToTransfer;
                nextTile.levelData.enemyForces = forcesToTransfer;
                enemyTiles.Add(nextTile);
            }
        }
    }
    private void DoPlayerExpand(List<UIMapTile> playerTiles)
    {
        for (int i = 0; i < playerTiles.Count; i++)
        {
            if (playerTiles[i].levelData.playerForces > playerExpandThreshold &&
                playerTiles[i].levelData.enemyForces == 0)
            {
                List<UIMapTile> uncontrolled = playerTiles[i].levelData.GetNeighborsControlledBy(SectorControl.NONE);
                uncontrolled.AddRange(playerTiles[i].levelData.GetNeighborsControlledBy(SectorControl.Enemy));
                if (uncontrolled.Count == 0)
                    continue; 
                UIMapTile nextTile = uncontrolled[Random.Range(0, uncontrolled.Count)];

                if(nextTile.levelData.enemyForces > 0)
                {
                    nextTile.levelData.SetControl(SectorControl.Contested);
                    nextTile.SetColor(contestedColor);
                }
                else
                {
                    nextTile.levelData.SetControl(SectorControl.Player);
                    nextTile.SetColor(playerColor);
                }
                
                int forcesToTransfer = Mathf.RoundToInt(playerTiles[i].levelData.playerForces * playerExpandPercent);
                playerTiles[i].levelData.playerForces -= forcesToTransfer;
                nextTile.levelData.playerForces = forcesToTransfer;
                playerTiles.Add(nextTile);
            }
        }
    }


    [Button]
    private void GetNextSector()
    {
        if(TryGetNextTile(enemyTiles, enemyStart, playerStart, out UIMapTile nextTile))
        {
            nextTile.SetColor(enemyColor);
            nextTile.levelData.SetControl(SectorControl.Enemy);
            enemyTiles.Add(nextTile);
            nextTile.levelData.enemyForces = Random.Range(150, 225);
        }

        if(TryGetNextTile(playerTiles, playerStart, enemyStart, out nextTile))
        {
            nextTile.SetColor(playerColor);
            nextTile.levelData.SetControl(SectorControl.Player);
            playerTiles.Add(nextTile);
            nextTile.levelData.playerForces = Random.Range(100, 200);
        }
    }

    private bool TryGetNextTile(List<UIMapTile> tiles, UIMapTile start, UIMapTile end, out UIMapTile nextTile)
    {
        Vector3 direction = end.transform.position - start.transform.position;
        List<UIMapTile> activeTiles = tiles.Where(x => x.levelData.UnControlledNeighbors > 0).ToList();
        if(activeTiles.Count == 0)
        {
            nextTile = null;
            return false;
        }

        activeTiles = activeTiles.OrderByDescending(t => Vector3.Dot(t.transform.position - start.transform.position, direction)).ToList();
        UIMapTile tile = activeTiles[Random.Range(0, Mathf.Min(3, activeTiles.Count))];
        List<UIMapTile> neighbors = tile.levelData.neighbors.Where(x => x.levelData.SectorControl == SectorControl.NONE).ToList();
        
        if(neighbors.Count == 1)
        {
            nextTile = neighbors[0];
            return true;
        }
        
        neighbors = neighbors.OrderByDescending(n => Vector3.Dot(n.transform.position - tile.transform.position, direction)).ToList();
        nextTile = neighbors[0];
        return true;
    }

    [Button]
    private void PickEnemyStartLocations()
    {
        enemyTiles.Clear();
        for (int i = 0; i < enemyStartSectors; i++)
        {
            if(TryGetEnemyStartSector(out UIMapTile enemyStart))
            {
                enemyStart.levelData.enemyForces = 500;
                enemyStart.SetColor(enemyColor);
                enemyStart.levelData.SetControl(SectorControl.Enemy);
                enemyTiles.Add(enemyStart);
            }
            else
            {
                Debug.LogWarning("No more sectors available for enemy start locations.");
                break;
            }
        }
    }

    private bool TryGetEnemyStartSector(out UIMapTile startSector)
    {
        Hex3 playerLocation = playerStart.levelData.location;
        var randomizedTiles = GetActiveTiles().OrderBy(t => System.Guid.NewGuid());
        foreach (var tile in randomizedTiles)
        {
            if (tile.levelData.SectorControl != SectorControl.NONE)
                continue;

            Hex3 location = tile.levelData.location;
            int distance = Hex3.DistanceBetween(location, playerLocation);

            if (distance > minDistanceToPlayer)
            {
                startSector = tile;
                return true;
            }
        }

        startSector = null; 
        return false;
    }

    private List<UIMapTile> GetActiveTiles()
    {
        List<UIMapTile> activeTiles = new();
        for (int i = 0; i < mapTiles.tiles.Count; i++)
        {
            if (mapTiles.tiles[i].levelData.isActive)
                activeTiles.Add(mapTiles.tiles[i]);
        }

        return activeTiles;
    }

    [Button]
    private void PickPlayerStart()
    {
        if(wmg == null)
            wmg = FindObjectOfType<WorldMapGenerator>(true);

        playerStart = wmg.CenterTile;
        playerStart.levelData.playerForces = 500;
        playerStart.SetColor(playerColor);
        playerStart.levelData.SetControl(SectorControl.Player);
        playerTiles.Clear();
        playerTiles.Add(playerStart);
    }

    private void DoResourceTick(List<UIMapTile> playerTiles)
    {
        for (int i = 0; i < playerTiles.Count; i++)
        {
            playerTiles[i].levelData.DoResourceTick();
        }
    }

    private void UpdateProgressBar(List<UIMapTile> tiles)
    {
        int totalSectors = 0;
        int enemyControlled = 0;
        int playerControlled = 0;

        for (int i = 0; i < tiles.Count; i++)
        {
            if (!tiles[i].levelData.isActive)
                continue;

            totalSectors++;

            if (tiles[i].levelData.SectorControl == SectorControl.Enemy)
                enemyControlled++;
            else if (tiles[i].levelData.SectorControl == SectorControl.Player)
                playerControlled++;
                
        }

        float enemyPercent = (float)enemyControlled / totalSectors;
        float playerPercent = (float)playerControlled / totalSectors;

        enemyProgres.Size.X.Percent = enemyPercent;
        playerProgress.Size.X.Percent = playerPercent;
        playerStatus.Text = "" + (enemyPercent * 100).ToString("F0") + "% Enemy Control  :  " +
            (playerPercent * 100).ToString("F0") + "% Player Control";

    }
}
