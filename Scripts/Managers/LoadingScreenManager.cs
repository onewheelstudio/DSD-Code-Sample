using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingScreenManager : MonoBehaviour
{
    [SerializeField] private GameObject loadingScreenPrefab;
    private LoadingScreen loadingScreen;

    public static void StartLoadingScreen()
    {

        LoadingScreenManager LSM = FindObjectOfType<LoadingScreenManager>();
        if (LSM == null)
            return;

        LSM.DoStart();
    }

    public void DoStart()
    {
        FindFirstObjectByType<IntroFade>().FadeToBlack(() =>
        {
            if (this.loadingScreen == null)
                this.loadingScreen = Instantiate(this.loadingScreenPrefab).GetComponentInChildren<LoadingScreen>();
        });
    }
}
