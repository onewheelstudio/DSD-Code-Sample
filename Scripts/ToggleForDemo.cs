using System.Collections.Generic;
using UnityEngine;

public class ToggleForDemo : MonoBehaviour
{
    [SerializeField] private bool showInDemo = true;

    [SerializeField] private bool toggleGameObject = true;
    [SerializeField] private List<MonoBehaviour> componentsToToggle = new List<MonoBehaviour>();
    [SerializeField] private GameSettings gameSettings;

    private void Awake()
    {
        if (gameSettings == null)
        {
            Debug.Log("Missing GameSettings", this.gameObject);
            return;
        }
        Toggle(gameSettings.IsDemo);
    }

    private void Toggle(bool obj)
    {
        if(toggleGameObject)
            this.gameObject.SetActive(obj == showInDemo);

        foreach (var component in componentsToToggle)
        {
            component.enabled = obj == showInDemo;
        }
    }
}
