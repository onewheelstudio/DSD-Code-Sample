using Sirenix.OdinInspector;
using UnityEngine;

public class Spin : MonoBehaviour
{
    [Range(0f, 10f)]
    [SerializeField] private float spinRate = 1f;
    [SerializeField] private Vector3 axis = Vector3.up;
    private MeshRenderer meshRenderer;
    [ShowInInspector]
    private bool IsVisible => meshRenderer != null ? meshRenderer.isVisible : false;

    private void Awake()
    {
        meshRenderer = this.GetComponent<MeshRenderer>();
    }


    // Update is called once per frame
    void Update()
    {
        this.transform.Rotate(axis, spinRate);
    }
}
