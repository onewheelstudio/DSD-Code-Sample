using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

public class RestartButton : MonoBehaviour
{
    private void OnEnable()
    {
        this.GetComponent<Button>().onClick.AddListener(() =>
        {
            DOTween.KillAll(true);
            SceneManager.LoadScene(0);
        });
    }
}
