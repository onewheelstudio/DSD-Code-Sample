using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nova;

namespace Nova.Animations
{
    public struct MoveAnimation : IAnimation
    {
        public Vector3 startPosition;
        public Vector3 endPosition;
        public Transform transformToMove;

        public void Update(float percentDone)
        {
            transformToMove.localPosition = Vector3.Lerp(startPosition, endPosition, percentDone);
        }
    }
}
