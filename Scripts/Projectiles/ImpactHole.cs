using OWS.ObjectPooling;
using System;
using UnityEngine;

public class ImpactHole : MonoBehaviour, IPoolable<ImpactHole>
{
    [SerializeField] private Material material;
    private MaterialPropertyBlock propertyBlock;
    [SerializeField] private float lifeTime = 10f;
    private float timeLeft;
    private float alphaStepSize;

    [SerializeField] private Color color;
    [SerializeField] private float alpha = 0.3f;
    MeshRenderer meshRenderer;

    private Action<ImpactHole> returnAction;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            return;

        material = meshRenderer.material;
        propertyBlock = new MaterialPropertyBlock();
        color = material.color;
        propertyBlock.SetColor("_Color", color);
        meshRenderer.SetPropertyBlock(propertyBlock);

        material = meshRenderer.material;
        alphaStepSize = material.color.a / lifeTime;
        alphaStepSize *= 2f;
    }

    private void OnEnable()
    {
        timeLeft = lifeTime;
        if (meshRenderer == null)
            return;

        color.a = alpha;
        material.color = color;
    }

    private void OnDisable()
    {
        ReturnToPool();
    }

    private void Update()
    {

        timeLeft -= Time.deltaTime;

        if(timeLeft <= 0)
        {
            this.gameObject.SetActive(false);
        }

        if (meshRenderer == null)
            return;

        if(timeLeft > lifeTime / 2f)
        {
            color.a -= alphaStepSize * Time.deltaTime;
            material.color = color;
        }
    }
    public void Initialize(Action<ImpactHole> returnAction)
    {
        this.returnAction = returnAction;
    }

    public void ReturnToPool()
    {
        returnAction?.Invoke(this);
    }
}
