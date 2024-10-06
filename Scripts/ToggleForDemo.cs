using UnityEngine;

public class ToggleForDemo : MonoBehaviour
{
    [SerializeField] private bool showInDemo = true;

    private void OnEnable()
    {
        GameSettings.demoToggled += Toggle;
    }

    private void OnDisable()
    {
        GameSettings.demoToggled -= Toggle;
    }

    private void Toggle(bool obj)
    {
        this.gameObject.SetActive(obj == showInDemo);
    }
}
