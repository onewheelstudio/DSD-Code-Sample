using HexGame.Resources;
using System.Collections.Generic;

namespace HexGame.Units
{
    public class TileUnit : Unit, IPlaceable, IHavePopupInfo, IHavePopUpButtons
    {
        private new void OnEnable()
        {
            base.OnEnable();
            HexTile.NewHexTile += NewTileAdded;
        }

        private new void OnDisable()
        {
            base.OnDisable();
            HexTile.NewHexTile -= NewTileAdded;
        }

        private void NewTileAdded(HexTile obj)
        {
            if (obj != null && obj.gameObject == this.gameObject)
            {
                Place();
                HexTile.NewHexTile -= NewTileAdded;
            }
        }

        public override void Place()
        {
            base.Place();
        }

        public HexTileType tileType;
        public List<PopUpInfo> GetPopupInfo()
        {
            List<PopUpInfo> info = new List<PopUpInfo>();
            info.Add(new PopUpInfo(tileType.ToString().ToUpper(), -1000, PopUpInfo.PopUpInfoType.name));

            return info;
        }

        public List<PopUpPriorityButton> GetPopUpButtons()
        {
            return new List<PopUpPriorityButton>()
            {
                //new PopUpButton("Destroy", () => this.gameObject.SetActive(false), 1000, true),
                //new PopUpButton("On/Off", () => CanToggleOff(), -1000)
            };
        }
    }
}

