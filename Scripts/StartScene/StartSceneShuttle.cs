using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HexGame.Units;
using System.Threading.Tasks;
using System.Threading;
using DG.Tweening;

[RequireComponent(typeof(HoverMoveBehavior))]
public class StartSceneShuttle : MonoBehaviour
{
    private ShuttleLandingPad[] shuttleLandingPads;
    private ShuttleLandingPad nextTarget;
    private HoverMoveBehavior hmb;
    private Task task;
    CancellationTokenSource cts;

    private void Awake()
    {
        shuttleLandingPads = GameObject.FindObjectsOfType<ShuttleLandingPad>();
        hmb = this.GetComponent<HoverMoveBehavior>();
    }

    private void OnEnable()
    {
        cts = new CancellationTokenSource();
        task = MoveShuttle(cts);
    }

    private void OnDisable()
    {
        cts.Cancel();
        DOTween.Kill(this,true);
    }

    private ShuttleLandingPad GetRandomShuttlePad()
    {
        return shuttleLandingPads[Random.Range(0, shuttleLandingPads.Length)];
    }

    private async Task MoveShuttle(CancellationTokenSource cts)
    {
        while (true)
        {
            while (hmb.isMoving)
            {
                await Task.Yield();
            }

            await Task.Delay(Random.Range(1000, 3000));

            nextTarget = GetRandomShuttlePad();
            hmb.SetDestination(nextTarget.transform.position);

            if (cts.IsCancellationRequested)
                break;
        }
    }
}
