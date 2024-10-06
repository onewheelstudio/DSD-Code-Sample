using NovaSamples.UIControls;
using UnityEngine;
using System.Collections;
using DG.Tweening;
using UnityEngine.Events;

[RequireComponent(typeof(Button))]
public class ButtonLoadScene : MonoBehaviour
{
    [SerializeField] private int sceneIndex = 0;
    [SerializeField] private UnityEngine.SceneManagement.LoadSceneMode mode;
    [SerializeField] private bool unloadCurrent = true;
    [SerializeField] private bool useLoadingScreen = true;
    [SerializeField] private UnityEvent beforeLoad;
    private int currentIndex = 0;

    private void OnEnable()
    {
        this.GetComponent<Button>().OnClicked.AddListener(() => LoadScene());
    }

    private void OnDisable()
    {
        this.GetComponent<Button>().OnClicked.RemoveAllListeners();
    }

    private void LoadScene()
    {
        beforeLoad.Invoke();
        if (useLoadingScreen)
            LoadingScreenManager.StartLoadingScreen();
        else
        {
            StartCoroutine(LoadAsync());
            currentIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        }
    }

    IEnumerator LoadAsync()
    {

        AsyncOperation async = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneIndex, mode);

        while (!async.isDone)
        {
            yield return null;
        }

        if (unloadCurrent)
            UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(currentIndex);
    }
}
