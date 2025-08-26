using Sirenix.OdinInspector;
using UnityEngine;

public class MiniMapCameraRender : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField,OnValueChanged("ToggleCamera")] private bool controlRender = true;
    [SerializeField] private RenderTexture m_SavedTexture;

    private void ToggleCamera()
    {
        _camera.enabled = !controlRender;
    }

    private void FixedUpdate()
    {
        if(controlRender)
        {
            _camera.Render();
        }
    }

    //void OnRenderImage(RenderTexture source, RenderTexture destination)
    //{
    //    if (m_SavedTexture == null)
    //        m_SavedTexture = Instantiate(source) as RenderTexture;

    //    if (Time.frameCount % 20 == 0)
    //        Graphics.Blit(source, m_SavedTexture);

    //    Graphics.Blit(m_SavedTexture, destination);
    //}
}
