using Nova;
using UnityEngine;

public class EndScreenPanel : WindowPopup
{
    [SerializeField] private ScreenSpace novaCanvas;

    private void Awake()
    {
        clipMask = GetComponent<ClipMask>();
    }

    private void Start()
    {
        CloseWindow();
    }

    public override void OpenWindow()
    {
        if (novaCanvas != null)
        {
            ClipMask novaClipMask = novaCanvas.gameObject.AddComponent<ClipMask>();
            novaClipMask.DoFade(0f, 0.5f);
        }
        else
            Debug.LogError("Nova Canvas not assigned!", this.gameObject);
        base.OpenWindow();
        this.transform.SetAsFirstSibling();
    }

    public override void CloseWindow()
    {
        if (novaCanvas != null)
        {
            ClipMask novaClipMask = novaCanvas.gameObject.GetComponent<ClipMask>();
            if (novaClipMask != null)
                novaClipMask.DoFade(1f, 0.5f).onComplete += () => Destroy(novaClipMask);
        }
        else
            Debug.LogError("Nova Canvas not assigned!", this.gameObject);

        base.CloseWindow();
    }
}
