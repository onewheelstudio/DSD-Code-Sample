using Nova;
using Sirenix.OdinInspector;
using UnityEngine;

[System.Serializable, HideLabel]
public class InteractableControl
{
    Interactable[] interactables;
    private Transform parent;

    public InteractableControl(Transform parent)
    {
        this.parent = parent;
        interactables = parent.GetComponentsInChildren<Interactable>(true);
    }

    public void SetInteractable(bool isInteractable)
    {
        foreach (Interactable interactable in interactables)
        {
            if(interactable == null)
                continue;
            interactable.enabled = isInteractable;
        }
    }

    public void Update()
    {
        if (parent == null)
            return;

        interactables = parent.GetComponentsInChildren<Interactable>(true);
    }
}
