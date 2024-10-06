using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Threading.Tasks;
using System.Threading;

public class StartSceneTurrentRotate : MonoBehaviour
{
    private Quaternion startRotation;
    private float rotateAmout;
    private int direction = 1;
    private Task task;
    CancellationTokenSource cts;

    private void Awake()
    {
        startRotation = this.transform.rotation;
    }

    private void OnEnable()
    {
        //StartCoroutine(DoRotation());
        cts = new CancellationTokenSource();
        task = DoRotation(cts);
    }

    private void OnDisable()
    {
        //StopAllCoroutines();
        cts.Cancel();
        DOTween.Kill(this,true);
    }

    private void Update()
    {
        this.transform.Rotate(Vector3.up, rotateAmout);
    }

    private async Task DoRotation(CancellationTokenSource cts)
    {
        while(true)
        {
            await Task.Delay(Random.Range(3000, 10000));
            direction = Random.Range(-1,2);
            rotateAmout = direction * 0.5f; ;
            await Task.Delay(Random.Range(250, 1000));
            rotateAmout = 0f;

            if(cts.IsCancellationRequested)
                break;
        }
    }
}
