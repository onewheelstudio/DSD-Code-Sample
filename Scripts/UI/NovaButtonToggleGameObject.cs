using NovaSamples.UIControls;
using UnityEngine;

[RequireComponent(typeof(Button))]
public class NovaButtonToggleGameObject : MonoBehaviour
{
    [SerializeField] private GameObject objectToToggle;

    private void OnEnable()
    {
        this.GetComponent<Button>().OnClicked.AddListener(() => ToggleObject());
    }

    private void ToggleObject()
    {
        objectToToggle.ToggleActive();
    }

    private void OnDisable()
    {
        this.GetComponent<Button>().OnClicked.RemoveAllListeners();
    }
}
