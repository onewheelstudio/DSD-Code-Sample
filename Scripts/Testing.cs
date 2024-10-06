using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OWS.ObjectPooling;
using System;
using Sirenix.OdinInspector;
using HexGame.Units;
using Shapes;

public class Testing : ImmediateModeShapeDrawer
{
    private Camera _camera;

    private new void OnEnable()
    {
        base.OnEnable();
        _camera = Camera.main;
    }

    private void Update()
    {
        DrawHexagon(Vector3.zero);
    }

    [Button]
    private void DrawHexagon(Vector3 location)
    {
        using (Draw.Command(_camera))
        {
            Draw.Matrix = transform.localToWorldMatrix;
            Draw.RegularPolygon(6);
            Draw.RegularPolygon(location);
            Draw.RegularPolygon(1.5f);
            Draw.RegularPolygon(Color.red);
            Draw.RegularPolygon(6, 1.5f, Color.green);
        }
    }

    private void FunctionToCallWhenItsDone()
    {
        Debug.Log("Done!");
    }

    [Button]
    private void StartCo()
    {
        StartCoroutine(WrapperCoroutine());
    }

    IEnumerator WrapperCoroutine()
    {
        //wait for another coroutine to finish
        yield return StartCoroutine(StartingCoroutine());
        //hey look it's done!!
        FunctionToCallWhenItsDone();
        yield break;
    }

    IEnumerator StartingCoroutine()
    {
        //do stuff
        yield return StartCoroutine(AnotherCoroutine());
    }
    IEnumerator AnotherCoroutine()
    {
        //do stuff
        yield return StartCoroutine(YetANOTHERCoroutine());
    }
    IEnumerator YetANOTHERCoroutine()
    {
        //do stuff
        yield return StartCoroutine(YetANOTHERCoroutine());
    }

    IEnumerator WTFCoroutine()
    {
        //do stuff
        yield break;
    }
}


