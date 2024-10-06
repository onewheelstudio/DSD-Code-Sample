using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HexGame.Resources;
public interface IProduceResource
{
    IEnumerator DoProduction(System.Action<ResourceAmount> request);
}
