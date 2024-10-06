using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nova;
using NovaSamples.UIControls;
using System;

public class BackToWorldMap : MonoBehaviour
{
    private void OnEnable()
    {
        this.GetComponent<Button>().OnClicked.AddListener(() => LoadWorldMap());
    }

    private void LoadWorldMap()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(1);
    }
}
