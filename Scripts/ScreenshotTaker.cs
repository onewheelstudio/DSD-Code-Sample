using UnityEngine;
using Sirenix.OdinInspector;
using System.Threading.Tasks;
using UnityEngine.InputSystem;
using Nova;

public class ScreenshotTaker : MonoBehaviour
{

    [SerializeField]
    [Range(1,5)]
    private int size = 1;

    [SerializeField] private Camera uiCamera;

    // Update is called once per frame

    private void Update()
    {
        if ((Keyboard.current.kKey.wasPressedThisFrame)  && Application.isEditor)
        {
            ScreenShot();
        }
    }

    [Button]
    private void TakeScreenShot()
    {
        //string path = "D:\\Unity Projects\\Hex-Game\\Hex Playground\\Assets\\Screenshots\\ScreenShot " + System.Guid.NewGuid().ToString() + ".png";
        //ScreenCapture.CaptureScreenshot(path, size);
        //UIBase.SetActive(false);
        //path = "D:\\Unity Projects\\Hex-Game\\Hex Playground\\Assets\\Screenshots\\ScreenShot No UI" + System.Guid.NewGuid().ToString() + ".png";
        //ScreenCapture.CaptureScreenshot(path, size);
        //UIBase.SetActive(true);
        ScreenShot();
    }

    private async void ScreenShot()
    {
        string GUID = System.Guid.NewGuid().ToString();
        string path = "D:\\Unity Projects\\Hex-Game\\Hex Playground\\Assets\\Screenshots\\" + GUID + ".png";
        ScreenCapture.CaptureScreenshot(path, size);

        if (uiCamera == null)
            return;
        await Task.Delay(100);
        uiCamera.enabled = false;
        path = "D:\\Unity Projects\\Hex-Game\\Hex Playground\\Assets\\Screenshots\\" + GUID + "_NO_UI_.png";
        ScreenCapture.CaptureScreenshot(path, size);
        await Task.Delay(100);
        uiCamera.enabled = true;
    }
}
