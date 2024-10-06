using Sirenix.OdinInspector;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(DoNotDestroyOnLoad))]
public class WorldLevelManager : MonoBehaviour
{
    public static event Action<float,string> loadingProgress;
    public static event Action loadingStart;
    public static event Action loadingEnd;

    public void SetLevel(UIMapTile mapTile)
    {
        LoadLevel();
    }

    public void LoadLevel()
    {
        LoadingScreenManager.StartLoadingScreen();
        StartCoroutine(LoadYourAsyncScene());
        //StartCoroutine(LoadAsync());
    }

    IEnumerator LoadYourAsyncScene()
    {
        loadingStart?.Invoke();
        yield return new WaitForSeconds(0.25f);
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(2, LoadSceneMode.Additive);
        
        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
        {
            loadingProgress?.Invoke(asyncLoad.progress, "Loading Assets");
            yield return null;
        }
        yield return new WaitForSeconds(0.25f);
        asyncLoad = SceneManager.UnloadSceneAsync(1);
        while (!asyncLoad.isDone)
        {
            loadingProgress?.Invoke(asyncLoad.progress, "Loading Bits and Bytes");
            yield return null;
        }
        yield return new WaitForSeconds(0.25f);
        loadingEnd?.Invoke();
    }
}

