using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;


namespace HexGame.Resources
{
    [System.Serializable]
    [LabelWidth(100)]
    public class Cost
    {
        [HorizontalGroup]
        public int amount;
        [HorizontalGroup]
        public ResourceType resource;

        public override string ToString()
        {
            return $"{resource} {amount}";
        }
    }
}
