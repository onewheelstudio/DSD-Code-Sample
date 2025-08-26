using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;
using HexGame.Resources;

namespace HexGame.Units
{
    public class ResourceSinkBehavior : UnitBehavior
    {
        [SerializeField]
        [InlineEditor(Expanded = true)]
        private HexGame.Resources.ResourceSink resourceSink;
        protected Coroutine sinkCorountine;
        private UnitStorageBehavior storageBehavior;
        private StatusIndicator statusIndicator;

        public override void StartBehavior()
        {
            if (Application.isPlaying)
            {
                if (storageBehavior == null)
                    storageBehavior = this.GetComponent<UnitStorageBehavior>();

                if(sinkCorountine == null && !resourceSink.skipAutoCheck)
                    sinkCorountine = StartCoroutine(resourceSink.DoResourceUse(storageBehavior));
            }

            if (statusIndicator == null)
                statusIndicator = this.GetComponentInChildren<StatusIndicator>();

            isFunctional = true;
        }

        public override void StopBehavior()
        {
            if (Application.isPlaying)
            {
                if (sinkCorountine != null && !resourceSink.skipAutoCheck)
                    StopCoroutine(sinkCorountine);
            }

            isFunctional = false;
        }

        private void Update()
        {
            if (!_isFunctional)
            {
                statusIndicator?.SetStatus(StatusIndicator.Status.red);
                return;
            }
        }

        private void CheckForResources()
        {
            resourceSink.CheckForResources(storageBehavior);
        }
    }
}
