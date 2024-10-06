using Nova;
using NovaSamples.UIControls;
using OWS.ObjectPooling;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
 
public class WorldMapToolTipWindow : WindowPopup // IPointerExitHandler, ICancelHandler, IPointerEnterHandler
{
    [SerializeField, Required]
    private TextBlock toolTipLabel;
    [SerializeField, Required]
    private GameObject buttonPrefab;
    private static ObjectPool<PoolObject> buttonPool;
    private List<GameObject> buttonList = new List<GameObject>();
    [SerializeField, Required]
    private Transform buttonContainer;

    [SerializeField]
    [Range(0, 500)]
    private float xOffset = 150;
    [SerializeField]
    [Range(0, 500)]
    private float yOffset = 0;
    public static NovaToolTip toolTipObject;

    public Slider stat1;
    public Slider stat2;
    public Slider stat3;
    public Slider stat4;
    public Slider stat5;

    private bool mouseIsOver = false;
    private float closeDelay = 0.35f;

    private UIControlActions uiActions;

    private Vector2 canvasResolution;
    private float canvasScale;

    private void Awake()
    {
        buttonPool = new ObjectPool<PoolObject>(buttonPrefab);
        uiActions = new UIControlActions();

        canvasResolution = GameObject.FindObjectOfType<Nova.ScreenSpace>().ReferenceResolution;
        canvasScale = Screen.width / (float)canvasResolution.x;
    }

    private new void OnEnable()
    {
        base.OnEnable();
        NovaToolTip.openToolTip += OpenToolTip;
        NovaToolTip.closeToolTip += CloseWindow;
        NovaToolTip.updateToolTip += PopulateToolTip;

        uiActions.UI.CloseWindow.started += ForceClose;
        uiActions.UI.CloseWindow.Enable();

        UIBlock2D block = this.GetComponent<UIBlock2D>();
        block.AddGestureHandler<Gesture.OnUnhover, PopUpVisuals>(OnEndHover);
        block.AddGestureHandler<Gesture.OnHover, PopUpVisuals>(OnStartHover);
    }

    private new void OnDisable()
    {
        base.OnDisable();
        NovaToolTip.openToolTip -= OpenToolTip;
        NovaToolTip.closeToolTip -= CloseWindow;
        NovaToolTip.updateToolTip -= PopulateToolTip;

        uiActions.UI.CloseWindow.started -= ForceClose;
        uiActions.UI.CloseWindow.Disable();


        UIBlock2D block = this.GetComponent<UIBlock2D>();
        block.RemoveGestureHandler<Gesture.OnUnhover, PopUpVisuals>(OnEndHover);
        block.RemoveGestureHandler<Gesture.OnHover, PopUpVisuals>(OnStartHover);
    }

    private void OpenToolTip(List<PopUpInfo> popUpInfos, List<PopUpPriorityButton> popUpButtons, List<PopUpValues> popUpValues, NovaToolTip toolTip)
    {
        if (toolTipObject != null && toolTipObject == toolTip)
            return;

        PopulateToolTip(popUpInfos, popUpButtons, popUpValues, toolTip);

        OpenWindow();
        this.GetComponent<UIBlock2D>().Position.Y = Mouse.current.position.ReadValue().y / canvasScale;
        this.GetComponent<UIBlock2D>().Position.X = Mouse.current.position.ReadValue().x / canvasScale;
        //this.transform.position = (GetMousePosition() + TooltipOffset()) / canvasScale;
    }

    private void PopulateToolTip(List<PopUpInfo> popUpInfos, List<PopUpPriorityButton> popUpButtons, List<PopUpValues> popUpValues, NovaToolTip toolTip)
    {
        toolTipObject = toolTip;

        SetUpText(popUpInfos);
        SetUpButtons(popUpButtons);
        SetUpValues(popUpValues);
    }


    private void SetUpText(List<PopUpInfo> popUpInfos)
    {
        ClearText();
        toolTipLabel.Text = popUpInfos[0].info;
    }

    private void SetUpValues(List<PopUpValues> valueList)
    {
        stat1.Value = valueList[0].value;
        stat1.Label = valueList[0].label;
        stat2.Value = valueList[1].value;
        stat2.Label = valueList[1].label;
        stat3.Value = valueList[2].value;
        stat3.Label = valueList[2].label;
        stat4.Value = valueList[3].value;
        stat4.Label = valueList[3].label;
        stat5.Value = valueList[4].value;
        stat5.Label = valueList[4].label;
    }

    private void SetUpButtons(List<PopUpPriorityButton> popUpButtons)
    {
        CleanUpButtons();
        popUpButtons = popUpButtons.OrderByDescending(o => o.priority).ToList();
        foreach (var popUpButton in popUpButtons)
        {
            SetUpButton(popUpButton);
        }
    }

    private void CleanUpButtons()
    {
        if (buttonList.Count == 0)
            return;

        foreach (var button in buttonList)
        {
            button.SetActive(false);
        }
    }

    private void SetUpButton(PopUpPriorityButton popUpButton)
    {
        GameObject button = buttonPool.PullGameObject();

        if (!buttonList.Contains(button))
        {
            buttonList.Add(button);
            button.transform.SetParent(buttonContainer);
        }

        button.GetComponentInChildren<TextBlock>().Text = popUpButton.displayName;

        Button uiButton = button.GetComponent<Button>();
        button.transform.localScale = Vector3.one;
        uiButton.RemoveAllListeners();
        uiButton.clicked += () => popUpButton.button?.Invoke();
        if (popUpButton.closeWindowOnClick)
            uiButton.clicked += () => CloseWindow();
    }

    private void ClearText()
    {
        toolTipLabel.Text = string.Empty;
    }
    private Vector3 TooltipOffset()
    {
        Vector3 offset = Vector3.zero;

        if (GetMousePosition().x > 0.75f * Screen.width)
            offset -= new Vector3(xOffset, -yOffset, 0f);
        else
            offset += new Vector3(xOffset, yOffset, 0f);

        return offset;
    }

    private Vector3 GetMousePosition()
    {
        return new Vector3(Mouse.current.position.ReadValue().x, Mouse.current.position.ReadValue().y, 0f);
    }

    private void CloseWindow(bool isInEditMode)
    {
        if (!isInEditMode)
            CloseWindow();
    }

    public override void CloseWindow()
    {
        if (mouseIsOver)
            return;

        if (clipMask == null)
            clipMask = this.GetComponent<ClipMask>();

        if (Application.isPlaying)
            clipMask.DoFade(0f, 0.1f);
        else
            clipMask.SetAlpha(0f);

        //clipMask.interactable = false;
        //clipMask.obstructDrags = false;
        toolTipObject = null; //used to allow tooltip to know what is active
        StopAllCoroutines();
    }

    public override void OpenWindow()
    {
        if (clipMask == null)
            clipMask = this.GetComponent<ClipMask>();

        if (Application.isPlaying)
            clipMask.DoFade(1f, 0.15f);
        else
            clipMask.SetAlpha(1f);

        //SFXManager.PlayClick();
        //clipMask.interactable = true;
        //clipMask.obstructDrags = true;

        this.transform.SetAsLastSibling();
    }

    public void OnEndHover(Gesture.OnUnhover evt, PopUpVisuals button)
    {
        mouseIsOver = false;
        StartCoroutine(DelayClose());
        //CloseWindow();
    }

    public void ForceClose(InputAction.CallbackContext cxt)
    {
        if (clipMask == null)
            clipMask = this.GetComponent<ClipMask>();

        if (Application.isPlaying)
            clipMask.DoFade(0f, 0.1f);
        else
            clipMask.SetAlpha(0f);

        //clipMask.interactable = false;
        //clipMask.obstructDrags = false;
        toolTipObject = null; //used to allow tooltip to know what is active
        StopAllCoroutines();
    }


    private void OnStartHover(Gesture.OnHover evt, PopUpVisuals target)
    {
        mouseIsOver = true;
    }

    private IEnumerator DelayClose()
    {
        yield return new WaitForSeconds(closeDelay);
        if (!mouseIsOver)
            CloseWindow();
    }
}
