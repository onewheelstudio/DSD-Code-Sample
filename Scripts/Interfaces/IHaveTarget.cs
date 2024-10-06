using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexGame.Units
{
    public interface IHaveTarget
    {
        void SetTarget(Unit target);
    }
}
