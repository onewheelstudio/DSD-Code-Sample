using UnityEngine;

public class GameOverToggleOff : MonoBehaviour
{
    private void OnEnable()
    {
        GameOverMenu.GameOver += ToggleOff;
    }

    private void OnDisable()
    {
        GameOverMenu.GameOver -= ToggleOff;
    }

    private void ToggleOff()
    {
        this.gameObject.SetActive(false);
    }
}
