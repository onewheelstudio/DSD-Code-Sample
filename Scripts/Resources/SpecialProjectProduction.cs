using HexGame.Resources;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexGame.Resources
{
    [CreateAssetMenu(menuName = "Hex/Special Project Production")]
    public class SpecialProjectProduction : ResourceProduction
    {
        [SerializeField] private BuildOverTime projectPrefab;
        public BuildOverTime ProjectPrefab => projectPrefab;
    }
}
