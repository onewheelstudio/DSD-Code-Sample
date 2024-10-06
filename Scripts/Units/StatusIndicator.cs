using Sirenix.OdinInspector;
using UnityEngine;

public class StatusIndicator : MonoBehaviour
{
    [SerializeField] private Material greenMat;
    [SerializeField] private Material yellowMat;
    [SerializeField] private Material redMat;
    [SerializeField,Range(0.1f,5f)] private float flashTime;
    private MeshRenderer[] renderers;
    private Status status = Status.green;


    private void Awake()
    {
        renderers = this.GetComponentsInChildren<MeshRenderer>();
    }


    [Button]
    public void SetStatus(Status status)
    {
        if (this.status == status)
            return;

        this.status = status;

        switch (status)
        {
            case Status.green:
                SetMaterial(greenMat);
                break;
            case Status.yellow:
                SetMaterial(yellowMat);
                break;
            case Status.red:
                SetMaterial(redMat);
                break;
        }
    }

    private void SetMaterial(Material material)
    {
        foreach (var mr in renderers)
            mr.material = material;
    }

    public enum Status
    {
        green,
        yellow,
        red,
    }
}
