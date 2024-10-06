using OWS.ObjectPooling;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lighting : MonoBehaviour, IPoolable<Lighting>
{
    [SerializeField] private float pointSpacing = .1f;
    [SerializeField] private float randomness = 1f;
    [Range(1,5)]
    [SerializeField] private int pointsPerFrame = 3;
    [SerializeField] private float explosionRadius = 0.5f;
    [SerializeField] private float sparkRadius = 0.25f;
    [SerializeField, Range(1, 10)] private int sparkSpacing = 5;
    [Title("Particle Prefabs")]
    [SerializeField] private GameObject explosion;
    [SerializeField] private GameObject sparks;
    private static ObjectPool<PoolObject> explosionPool;
    private static ObjectPool<PoolObject> sparksPool;
    private LineRenderer lineRenderer;
    private Action<Lighting> returnToPool;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        if(explosionPool == null)
            explosionPool = new ObjectPool<PoolObject>(explosion);
        if(sparksPool == null)
            sparksPool = new ObjectPool<PoolObject>(sparks);
    }
    private void OnDisable()
    {
        ReturnToPool();
    }

    [Button]
    public void DoLighting(Vector3 startPoint, Vector3 endPoint)
    {
        StartCoroutine(AnimateLighting(startPoint, endPoint));
    }
    
    public IEnumerator AnimateLighting(Vector3 startPoint, Vector3 endPoint)
    {
        this.transform.position = startPoint;
        this.transform.LookAt(endPoint); 
        
        List<Vector3> points = GenerateRandomPointsInLine(startPoint, endPoint);

        GameObject explosion = explosionPool.PullGameObject(startPoint, Quaternion.identity);
        explosion.transform.localScale = Vector3.one * explosionRadius;

        int count = Mathf.CeilToInt(points.Count / pointsPerFrame);

        for (int i = 1; i < count; i++)
        {
            int endIndex = i * pointsPerFrame;
            if(endIndex >= points.Count)
                endIndex = points.Count - 1;

            lineRenderer.positionCount = i * pointsPerFrame;
            lineRenderer.SetPositions(points.GetRange(0, endIndex).ToArray());

            if (i % sparkSpacing == 0)
            {
                explosion = sparksPool.PullGameObject(points[endIndex], Quaternion.identity);
                explosion.transform.localScale = Vector3.one * sparkRadius;
            }
            yield return null;
        }

        explosion = explosionPool.PullGameObject(endPoint, Quaternion.identity);
        explosion.transform.localScale = Vector3.one * explosionRadius; 
        
        for (int i = 1; i < count; i++)
        {
            int startIndex = i * pointsPerFrame;
            if (startIndex >= points.Count)
                startIndex = points.Count - 1;

            lineRenderer.positionCount = i * pointsPerFrame - startIndex;
            lineRenderer.SetPositions(points.GetRange(startIndex, points.Count - startIndex).ToArray());

            yield return null;
        }


        this.gameObject.SetActive(false); //should get returned to pool
    }

    private List<Vector3> GenerateRandomPointsInLine(Vector3 startPoint, Vector3 endPoint)
    {
        List<Vector3> points = new List<Vector3>();
        float distance = Vector3.Distance(startPoint, endPoint);
        int numberOfPoints = Mathf.FloorToInt(distance / pointSpacing);
        for (int i = 0; i < numberOfPoints; i++)
        {
            Vector3 randomValue = UnityEngine.Random.insideUnitSphere * randomness;
            randomValue.z = i* pointSpacing;
            points.Add(this.transform.rotation * randomValue + startPoint);
        }

        return points;
    }


    #region Object Pooling
    public void Initialize(Action<Lighting> returnAction)
    {
        //cache reference to return action
        this.returnToPool = returnAction;
    }

    public void ReturnToPool()
    {
        //invoke and return this object to pool
        lineRenderer.positionCount = 0;
        returnToPool?.Invoke(this);
    }
    #endregion
}
