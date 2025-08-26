using HexGame.Units;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexGame.Resources
{
    [System.Serializable]
    [CreateAssetMenu(menuName = "Resource Sink")]
    public class ResourceSink : ScriptableObject
    {
        public List<ResourceAmount> resourcesRequired = new List<ResourceAmount>();
        public List<ResourceAmount> resourcesAtStake = new List<ResourceAmount>();
        public float useTime = 1f;
        public static event System.Action<ResourceAmount> resourceLost;
        public event System.Action insufficientResources;
        public bool skipAutoCheck = false;
        public bool useLocalStorage = true;

        public virtual IEnumerator DoResourceUse(UnitStorageBehavior storageBehavior)
        {
            //this should never be true, but just in case
            if(skipAutoCheck)
                yield break;

            PlayerResources pr = FindObjectOfType<PlayerResources>();

            while (true)
            {
                yield return new WaitForSeconds(useTime);
                CheckForResources(storageBehavior);
            }
        }
        public void CheckForResources(UnitStorageBehavior storageBehavior)
        {
            if (useLocalStorage && !storageBehavior.TryUseAllResources(resourcesRequired))
            {
                insufficientResources?.Invoke();
                foreach (var resource in resourcesAtStake)
                {
                    resourceLost?.Invoke(resource);
                    MessagePanel.ShowMessage($"{resource.amount} {resource.type} lost at {storageBehavior.gameObject.name}.", storageBehavior.gameObject);
                }

                return;
            }
            //else if (!useLocalStorage && !PlayerResources.TryUseAllResources(resourcesRequired))
            //{
            //    insufficientResources?.Invoke();
            //    foreach (var resource in resourcesAtStake)
            //    {
            //        resourceLost?.Invoke(resource);
            //        MessagePanel.ShowMessage($"{resource.amount} {resource.type} lost at {storageBehavior.gameObject.name}.", storageBehavior.gameObject);
            //    }

            //    return;
            //}
        }
    }
}
