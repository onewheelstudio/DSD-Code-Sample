using DG.Tweening;
using OWS.ObjectPooling;
using Shapes;
using System;
using System.Collections;
using UnityEngine;

public class ConnectionCubeMotion : MonoBehaviour, IPoolable<ConnectionCubeMotion>
{
    private float distance;
    private float height;
    private Action<ConnectionCubeMotion> callback;
    private Cuboid cuboid;
    private Tween growTween;
    private ConnectionCubeData data;

    private void Awake()
    {
        if (cuboid == null)
            cuboid = GetComponent<Cuboid>();
    }

    private void OnDisable()
    {
        DOTween.Kill(this);
        StopAllCoroutines();
        ReturnToPool();
    }

    public void StartMotion(ConnectionCubeData data)
    {
        this.data = data;
        this.distance = Vector3.Distance(data.start, data.end);
        this.height = Mathf.Max(1f, distance / 5f);
        cuboid.Color = data.color;
        StartCoroutine(MoveCube());

        this.cuboid.Size = Vector3.zero;
        DoSize(Vector3.one * data.size, 0.5f);
    }

    private IEnumerator MoveCube()
    {
        //working in local space
        Vector3 position = Vector3.zero;
        float z = 0 + data.offset;
        while (true)
        {
            z += Time.deltaTime * data.speed;
            if(z > distance)
            {
                this.gameObject.SetActive(false);
                yield break;
            }
            this.transform.localPosition = new Vector3(0, GetHeight(z), z);
            yield return null;
        }
    }

    private float GetHeight(float z)
    {
        return Mathf.Abs(z * (z - distance) * 4 * height / (distance * distance));
    }

    public void Initialize(Action<ConnectionCubeMotion> returnAction)
    {
        this.callback = returnAction;

    }

    public void ReturnToPool()
    {
        this.callback?.Invoke(this);
    }

    public Tween DoSize(Vector3 endValue, float duration)
    {
        growTween = DOTween.To(() => this.cuboid.Size, x => this.cuboid.Size = x, endValue, duration);
        growTween.OnComplete(() => this.cuboid.Size = endValue);
        growTween.SetUpdate(true);
        return growTween;;
    }

    public struct ConnectionCubeData
    {
        public Vector3 start;
        public Vector3 end;
        public float offset;
        public float speed;
        public float size;
        public Color color;
    }
}
