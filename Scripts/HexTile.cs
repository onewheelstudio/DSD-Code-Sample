using DG.Tweening;
using HexGame.Grid;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;

namespace HexGame.Resources
{

    [System.Serializable]
    public class HexTile : MonoBehaviour
    {
        public static event System.Action<HexTile> NewHexTile;
        public static event System.Action<HexTile> HexTileRemoved;

        [HideInInspector]
        public HexTileSideData[] sideData = new HexTileSideData[6];
        public Hex3 hexPosition;
        [SerializeField]
        private HexTileType tileType;
        public HexTileType TileType
        {
            get
            {
                return tileType;
            }
        }

        [BoxGroup("Path Finding")]
        [SerializeField] private uint penalty = 0;
        public uint Penalty => penalty;
        [BoxGroup("Path Finding")]
        [SerializeField] private bool walkable = true;
        public bool Walkable => walkable;
        [BoxGroup("Path Finding")]
        public bool isPlaceHolder = false;
        private FogGroundTile fogTile;
        public FogGroundTile FogTile
        {
            get
            {
                if(fogTile == null)
                    fogTile = this.GetComponent<FogGroundTile>();
                return  fogTile;
            }
        }

        public void Rotate(int i = 1)
        {
            this.transform.Rotate(Vector3.up, 60 * i);
        }

        public HexTileSideData[] GetRotatedData()
        {
            HexTileSideData[] rotatedSideData = new HexTileSideData[6];
            int hexRot = Mathf.RoundToInt(this.transform.rotation.eulerAngles.y / 60);
            int index;

            for (int i = 0; i < sideData.Length; i++)
            {
                //index = i - hexRot >= 0 ? i - hexRot : sideData.Length - hexRot + i;
                index = i - hexRot < 0 ? sideData.Length + hexRot - i : i - hexRot;

                rotatedSideData[index] = sideData[i];
            }

            return rotatedSideData;
        }

        public HexTileSideData[] RotatedData(int rotations)
        {
            rotations = rotations > 5 ? 5 : rotations;

            HexTileSideData[] rotatedSideData = new HexTileSideData[6];
            int index;

            for (int i = 0; i < sideData.Length; i++)
            {
                index = i - rotations >= 0 ? i - rotations : sideData.Length - rotations + i;
                rotatedSideData[index] = sideData[i];
            }

            return rotatedSideData;
        }

        private HexTileSideData GetRotatedDataAtPosition(int position)
        {
            int hexRot = Mathf.RoundToInt(this.transform.rotation.eulerAngles.y / 60);
            int index = (position + hexRot) % 6;
            return sideData[index];
        }

        /// <summary>
        /// Returns the side data for a neighbor.
        /// Position is relative to the neighbor.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public HexTileSideData GetRotataDataForNeighbor(int position)
        {
            if (position <= 2)
                return GetRotatedDataAtPosition(position + 3);
            else
                return GetRotatedDataAtPosition(position - 3);
        }

        public enum HexTileSideData
        {
            none,
            red,
            blue,
            green
        }

        [Button]
        public void AdjustPosition()
        {
            this.transform.position = Hex3.Hex3ToVector3(Hex3.Vector3ToHex3(this.transform.position));
            AdjustHex();
        }

        private void OnEnable()
        {
            LandmassCreator.generationComplete += DoGridDelay;
        }

        private void OnDisable()
        {
            LandmassCreator.generationComplete -= DoGridDelay;
            HexTileRemoved?.Invoke(this);
            DOTween.Kill(this,true);
        }

        private void AdjustHex()
        {
            hexPosition = Hex3.Vector3ToHex3(this.transform.position);
        }

        public void PlaceHex()
        {
            if(!LandmassCreator.generating)
                DoGridDelay();
            NewHexTile?.Invoke(this);
        }

        private void DoGridDelay()
        {
            StartCoroutine(DelayGridUpdate());
        }

        IEnumerator DelayGridUpdate()
        {
            yield return null;
            yield return null;

            if(!isPlaceHolder)
                HelperFunctions.UpdatePathfindingGrid(this.gameObject, walkable, penalty, (int)this.TileType);
        }

        public void DestroyTile()
        {
            StartCoroutine(DoDestructionAnimation());
        }

        private IEnumerator DoDestructionAnimation()
        {
            if(this.TileType != HexTileType.grass)
            {
                this.gameObject.SetActive(false);
                yield break;
            }    

            float randomTime = HexTileManager.GetNextInt(0, 200) / 1000f;
            yield return new WaitForSeconds(randomTime);
            Vector3 scale = this.transform.localScale;
            this.transform.DOScale(0f, 1f);
            randomTime = HexTileManager.GetNextInt(50, 100) / 1000f;
            yield return new WaitForSeconds(randomTime);
            Tween drop = this.transform.DOLocalMoveY(this.transform.position.y - 2f, 1f);
            yield return drop.WaitForCompletion();
            this.transform.localScale = scale;
            this.gameObject.SetActive(false);
        }

    }

    public enum HexTileType
    {
        grass = 0,
        mountain = 1,
        forest = 2,
        water = 3,
        feOre = 4,
        alOre = 5,
        gas = 6,
        tiOre = 7,
        uOre = 8,
        oil = 9,
        sand = 10,
        aspen =11,
        cuOre = 12,
    }
}