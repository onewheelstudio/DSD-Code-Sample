using Nova;
using UnityEngine;

[RequireComponent(typeof(ScreenSpace))]
public class DrawCanvas : MonoBehaviour
{
    private UIBlock canvas;
    private Vector3 upperLeft;
    private Vector3 upperRight;
    private Vector3 lowerRight;
    private Vector3 lowerLeft;
    [SerializeField, Tooltip("If true outline of the UIBlock will always be drawn.")] 
    private bool drawCanvas = true;
    [SerializeField] private Color color = new Color(1f,0f,0.9f);

    private void OnDrawGizmos()
    {
        if(!drawCanvas)
            return;

        if(canvas == null)
            canvas = GetComponent<UIBlock>();

        Vector2 size = canvas.Size.XY.Value * this.transform.localScale.x / 2f ;
        Gizmos.color = color;
        upperLeft = this.transform.position + new Vector3(-size.x, size.y, 0);
        upperRight = this.transform.position + new Vector3(size.x, size.y, 0);
        lowerRight = this.transform.position + new Vector3(size.x, -size.y, 0);
        lowerLeft = this.transform.position + new Vector3(-size.x, -size.y, 0);

        Gizmos.DrawLine(upperLeft, upperRight);
        Gizmos.DrawLine(lowerRight, upperRight);
        Gizmos.DrawLine(upperLeft, lowerLeft);
        Gizmos.DrawLine(lowerLeft, lowerRight);
    }
}
