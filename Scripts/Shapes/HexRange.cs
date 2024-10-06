using HexGame.Grid;
using OWS.ObjectPooling;
using Shapes;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexRange : MonoBehaviour, IPoolable<HexRange>
{
    private int range;
    [SerializeField] private Polygon polygon;
    [SerializeField] private Polyline outerBorder;
    [SerializeField] private Polyline innerBorder;
    [SerializeField] private GameObject polygonRangePrefab;
    private HashSet<Vector3> pointList = new HashSet<Vector3>();
    private HashSet<Hex3> hexList = new HashSet<Hex3>();
    private Action<HexRange> returnAction;
    private float tweenTime = 0.1f;

    public void ShowRange(int range, int minRange, Color borderColor, Color bodyColor)
    {
        this.gameObject.SetActive(true);
        if (this.range != range)
        {
            pointList.Clear();
            GeneratePoints(range, minRange);
        }

        this.range = range;
        outerBorder.Color = new Color(borderColor.r, borderColor.g, borderColor.b, 0f);
        outerBorder.DOFade(borderColor.a, tweenTime);
        innerBorder.Color = new Color(borderColor.r, borderColor.g, borderColor.b, 0f);
        innerBorder.DOFade(borderColor.a, tweenTime);
        polygon.Color = new Color(bodyColor.r, bodyColor.g, bodyColor.b, 0f);
        polygon.DOFade(bodyColor.a, tweenTime);
    }

    public void HideRange()
    {
        if (!this.gameObject.activeSelf)
            return;

        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        outerBorder.DOFade(0f, tweenTime);
        innerBorder.DOFade(0f, tweenTime);
        polygon.DOFade(0f, tweenTime);
        yield return new WaitForSeconds(tweenTime);
        this.gameObject.SetActive(false);
    }

    [Button]
    private void GeneratePoints(int range, int minRange)
    {
        //q = range;
        for (int s = 0; s < range + 1; s++)
        {
            Hex3 hex3 = new Hex3(-range, +range - s, s);
            hexList.Add(hex3);
            pointList.Add(hex3.ToVector3() + Hex3.vertices[3]);
            pointList.Add(hex3.ToVector3() + Hex3.vertices[4]);
        }

        //s = range;
        for (int r = 0; r < range + 1; r++)
        {
            Hex3 hex3 = new Hex3(-range + r, -r, range);
            hexList.Add(hex3);
            pointList.Add(hex3.ToVector3() + Hex3.vertices[4]);
            pointList.Add(hex3.ToVector3() + Hex3.vertices[5]);
        }

        //r = range;
        for (int q = 0; q < range + 1; q++)
        {
            Hex3 hex3 = new Hex3(q, -range, -q + range);
            hexList.Add(hex3);
            pointList.Add(hex3.ToVector3() + Hex3.vertices[0]);
            pointList.Add(hex3.ToVector3() + Hex3.vertices[1]);
        }

        //q = range;
        for (int s = 0; s < range + 1; s++)
        {
            Hex3 hex3 = new Hex3(range, -range + s, -s);
            hexList.Add(hex3);
            pointList.Add(hex3.ToVector3() + Hex3.vertices[1]);
            pointList.Add(hex3.ToVector3() + Hex3.vertices[2]);
        }

        //s = range;
        for (int r = 0; r < range + 1; r++)
        {
            Hex3 hex3 = new Hex3(+range - r, r, -range);
            hexList.Add(hex3);
            pointList.Add(hex3.ToVector3() + Hex3.vertices[2]);
            pointList.Add(hex3.ToVector3() + Hex3.vertices[3]);
        }

        //r = range;
        for (int q = 0; q < range + 1; q++)
        {
            Hex3 hex3 = new Hex3(-q, range, +q - range);
            hexList.Add(hex3);
            pointList.Add(hex3.ToVector3() + Hex3.vertices[3]);
            pointList.Add(hex3.ToVector3() + Hex3.vertices[4]);
        }

        outerBorder?.SetPoints(ConvertToV2());
        polygon?.SetPoints(ConvertToV2());
        pointList.Clear();

        if (minRange == 0)
        { 
            innerBorder?.gameObject.SetActive(false);
            return;
        }
        else
            innerBorder?.gameObject.SetActive(true);

        Hex3 lastPoint = new Hex3(-range, +range, 0);
        Vector3 lastV3 = lastPoint.ToVector3() + Hex3.vertices[3];
        outerBorder.AddPoint(new Vector2(lastV3.x, lastV3.z));
        polygon.AddPoint(new Vector2(lastV3.x, lastV3.z));

        //adjust so we exclude the hex the unit is on. 
        minRange--;

        for (int q = minRange; q > -1; q--)
        {
            Hex3 hex3 = new Hex3(-q, minRange, +q - minRange);
            hexList.Add(hex3);
            pointList.Add(hex3.ToVector3() + Hex3.vertices[4]);
            pointList.Add(hex3.ToVector3() + Hex3.vertices[3]);
        }

        //s = range;
        for (int r = minRange; r > -1; r--)
        {
            Hex3 hex3 = new Hex3(+minRange - r, r, -minRange);
            hexList.Add(hex3);
            pointList.Add(hex3.ToVector3() + Hex3.vertices[3]);
            pointList.Add(hex3.ToVector3() + Hex3.vertices[2]);
        }

        //q = range;
        for (int s = minRange; s > -1; s--)
        {
            Hex3 hex3 = new Hex3(minRange, -minRange + s, -s);
            hexList.Add(hex3);
            pointList.Add(hex3.ToVector3() + Hex3.vertices[2]);
            pointList.Add(hex3.ToVector3() + Hex3.vertices[1]);
        }

        //r = range;
        for (int q = minRange; q > -1; q--)
        {
            Hex3 hex3 = new Hex3(q, -minRange, -q + minRange);
            hexList.Add(hex3);
            pointList.Add(hex3.ToVector3() + Hex3.vertices[1]);
            pointList.Add(hex3.ToVector3() + Hex3.vertices[0]);
        }

        //s = range;
        for (int r = minRange; r > -1; r--)
        {
            Hex3 hex3 = new Hex3(-minRange + r, -r, minRange);
            hexList.Add(hex3);
            pointList.Add(hex3.ToVector3() + Hex3.vertices[5]);
            pointList.Add(hex3.ToVector3() + Hex3.vertices[4]);
        }

        //q = range;
        for (int s = minRange; s > -1; s--)
        {
            Hex3 hex3 = new Hex3(-minRange, +minRange - s, s);
            Debug.Log(hex3);
            hexList.Add(hex3);
            pointList.Add(hex3.ToVector3() + Hex3.vertices[4]);
            pointList.Add(hex3.ToVector3() + Hex3.vertices[3]);
        }


        polygon?.AddPoints(ConvertToV2());
        innerBorder?.gameObject.SetActive(true);
        innerBorder?.SetPoints(ConvertToV2());



        lastPoint = new Hex3(-minRange, minRange, 0);
        lastV3 = lastPoint.ToVector3() + Hex3.vertices[4];
        polygon.AddPoint(new Vector2(lastV3.x, lastV3.z));
    }

    private List<Vector2> ConvertToV2()
    {
        List<Vector2> vector2s = new List<Vector2>();
        foreach (var point in pointList)
        {
            vector2s.Add(new Vector2(point.x,point.z));
        }
        return vector2s;
    }
    private void OnDisable()
    {
        ReturnToPool();
        this.gameObject.SetActive(false);
    }

    public void Initialize(Action<HexRange> returnAction)
    {
        this.returnAction = returnAction;
    }

    public void ReturnToPool()
    {
        this.returnAction?.Invoke(this);
    }
}
