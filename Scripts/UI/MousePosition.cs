using HexGame.Grid;
using Nova;
using UnityEngine;
using UnityEngine.InputSystem;

public class MousePosition : MonoBehaviour
{
    private TextBlock textBlock;
    private Camera camera;

    private void Awake()
    {
        camera = Camera.main;
        textBlock = this.GetComponent<TextBlock>();
    }

    // Update is called once per frame
    void Update()
    {
        textBlock.Text = Mouse.current.position.ReadValue() + "\n";
        textBlock.Text += HelperFunctions.GetMouseVector3OnPlane(false, camera).ToString() + "\n";
        textBlock.Text += Hex3.Vector3ToHex3(HelperFunctions.GetMouseVector3OnPlane(false, camera));
    }
}
