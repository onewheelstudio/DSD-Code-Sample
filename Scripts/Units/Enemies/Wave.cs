using HexGame.Units;
using Sirenix.OdinInspector;
using System.Collections.Generic;

[System.Serializable]
public class Wave
{
    [Required, HorizontalGroup, LabelWidth(50)]
    public EnemyUnitType type;
    [MinValue(1), HorizontalGroup, LabelWidth(50)]
    public int number;
}
