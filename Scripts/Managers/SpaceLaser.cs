using Nova;
using Nova.Animations;
using NovaSamples.UIControls;
using OWS.Nova;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpaceLaser : WindowPopup, ISaveData
{
    private UIControlActions inputActions;
    private bool isAttacking = false;
    public bool IsAttacking => isAttacking;

    private CursorManager cursorManager;

    [SerializeField] private ProjectileData projectileStats;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float delay = 0.25f;
    [SerializeField] private float spread = 0.25f;
    private int count = 1;

    [Header("Menu Bits")]
    [SerializeField] private ToggleSwitch toggleTargeting;
    [SerializeField] private TextBlock statsText;
    [SerializeField] private ProgressBar reloadProgress;
    [SerializeField] private Button openButton;
    private AnimationHandle animationHandle;
    private UIBlock2D buttonBlock;
    private bool spaceLaserUnlocked = false;

    public static event Action SpaceLaserFired;
    public static event Action SpaceLaserIsAttacking;

    [Header("Space Laser HotKey")]
    [SerializeField] private InputActionReference m_Action;
    [SerializeField] private string m_BindingId;
    [SerializeField] private TextBlock hotKeyTip;

    private void Awake()
    {
        inputActions = new UIControlActions();
        cursorManager = FindObjectOfType<CursorManager>();
        reloadProgress.Percent = 0f;
        buttonBlock = openButton.GetComponent<UIBlock2D>();
        ButtonOff();
        RegisterDataSaving();
    }

    public override void OnEnable()
    {
        base.OnEnable();
        inputActions.UI.SpaceLasers.performed += ToggleAttack;
        inputActions.UI.SpaceLasers.Enable();

        //inputActions.UI.CloseWindow.performed += TurnOffAttack;
        //inputActions.UI.CloseWindow.Enable();

        toggleTargeting.Toggled += SetIsAttacking;

        DayNightManager.toggleNight += OpenWindow;
        UnlockSpaceLaserTrigger.UnlockSpaceLaser += ButtonOn;
        ControlsManager.UIControlsUpdated += UpdateSpaceLaserBinding;
        WindowPopup.SomeWindowOpened += ForceOff;

        CloseWindow();
        StartCoroutine(DoReload()); 
    }



    public override void OnDisable()
    {
        base.OnDisable();
        inputActions.UI.SpaceLasers.performed -= ToggleAttack;
        inputActions.UI.SpaceLasers.Disable();
        inputActions.UI.CloseWindow.Disable();

        toggleTargeting.Toggled -= SetIsAttacking;
        DayNightManager.toggleNight -= OpenWindow;
        UnlockSpaceLaserTrigger.UnlockSpaceLaser -= ButtonOn;
        ControlsManager.UIControlsUpdated -= UpdateSpaceLaserBinding;
        WindowPopup.SomeWindowOpened -= ForceOff;
        StopAllCoroutines();
    }

    private void UpdateSpaceLaserBinding(string rebinds)
    {
        inputActions.LoadBindingOverridesFromJson(rebinds);
    }

    private void ToggleAttack(InputAction.CallbackContext context)
    {
        if (WindowPopup.BlockWindowHotkeys)
            return;

        isAttacking = !isAttacking;
        toggleTargeting.ToggledOn = isAttacking; //should invoke event

        if (!instanceIsOpen && isAttacking)
            OpenWindow();
        else if(!isAttacking)
            CloseWindow();
    }

    private void SetIsAttacking(ToggleSwitch toggle, bool isAttacking)
    {
        this.isAttacking = isAttacking;
        if (this.isAttacking)
        {
            cursorManager.SetCursor(CursorType.target);
            Color cursorColor = count > 0 ? ColorManager.GetColor(ColorCode.techCredit) : ColorManager.GetColor(ColorCode.red);
            cursorManager.SetCursorColor(cursorColor);
            SpaceLaserIsAttacking?.Invoke();
            //MessagePanel.ShowMessage("Laser Activated", null);
        }
        else
        {
            cursorManager.SetCursor(CursorType.hex);
            cursorManager.SetCursorColor(Color.white);
            //MessagePanel.ShowMessage("Launch De-Activated", null);
        }
    }

    private void Update()
    {
        if(isAttacking)
        {
            if (Mouse.current.rightButton.wasPressedThisFrame )
            {
                cursorManager.SetCursor(CursorType.hex);
                isAttacking = false;
                toggleTargeting.ToggledOn = isAttacking;
                //MessagePanel.ShowMessage("Launch De-Actived", null);
            }
            else if (Mouse.current.leftButton.wasPressedThisFrame && count > 0)
            {
                if (PCInputManager.MouseOverVisibleUIObject())
                    return;

                StartCoroutine(LaunchAttack());
            }
        }
    }

    private IEnumerator LaunchAttack()
    {
        Vector3 attackPoint = HelperFunctions.GetMouseVector3OnPlane();
        Vector3 launchPoint = attackPoint + Vector3.up * 40f;

        count--;
        SpaceLaserFired?.Invoke();
        for (int i = 0; i < projectileStats.GetStatAsInt(Stat.burst); i++)
        {
            Vector3 offset = new Vector3(UnityEngine.Random.Range(-spread, spread), 0, UnityEngine.Random.Range(-spread, spread));
            GameObject projectile = projectileStats.GetProjectile();
            projectile.GetComponent<Projectile>().SetDamage(projectileStats.GetStat(Stat.damage));
            projectile.transform.position = launchPoint;
            projectile.transform.LookAt(attackPoint + offset);
            UpdateStatsText();
            yield return new WaitForSeconds(delay);
        }
    }

    private IEnumerator DoReload()
    {
        while(true)
        {
            if (isAttacking)
            {
                Color cursorColor = count > 0 ? ColorManager.GetColor(ColorCode.techCredit) : ColorManager.GetColor(ColorCode.red);
                cursorManager.SetCursorColor(cursorColor);
            }

            if (count >= projectileStats.GetStat(Stat.charges))
            {
                yield return null;
                continue;
            }
            else
            {
                float progress = Time.deltaTime / projectileStats.GetStat(Stat.reloadTime);
                reloadProgress.Percent += progress;
                cursorManager.SetProgress(reloadProgress.Percent);
                yield return null;

                if (reloadProgress.Percent >= 1f)
                {
                    count++;
                    UpdateStatsText();
                    reloadProgress.Percent = 0f;
                    if(isAttacking)
                        SFXManager.PlaySFX(SFXType.ResourceReveal);
                }
            }
        }
    }

    private void OpenWindow(int dayNumber)
    {
        if (!spaceLaserUnlocked)
            return;

        UpdateBindingDisplay();
        if (dayNumber == 2)
        {
            OpenWindow(false);
        }
        else
            DayNightManager.toggleNight -= OpenWindow; 
    }

    public override void ToggleWindow()
    {
        if (!spaceLaserUnlocked)
            return;
        UpdateBindingDisplay();

        if (this.instanceIsOpen)
            ForceClose();
        else
            OpenWindow();
    }

    public override void OpenWindow()
    {
        if (!spaceLaserUnlocked)
            return;

        if(!animationHandle.IsComplete())
        {
            animationHandle.Complete();
            buttonBlock.Color = Color.white;
        }
        UpdateBindingDisplay();

        base.OpenWindow();
        UpdateStatsText();
        isAttacking = true;
        toggleTargeting.ToggledOn = true;
    }

    public override void CloseWindow()
    {
        if (isAttacking)
        {
            isAttacking = false;
            toggleTargeting.ToggledOn = false; //should invoke event
            return;
        }

        ForceClose();
    }

    public void ForceClose()
    {
        if (!animationHandle.IsComplete())
        {
            animationHandle.Complete();
            buttonBlock.Color = Color.white;
        }

        base.CloseWindow();
        cursorManager.SetCursor(CursorType.hex);
        cursorManager.SetCursorColor(Color.white);
        isAttacking = false;
        toggleTargeting.ToggledOn = false; //should invoke event
    }

    private void ForceOff(WindowPopup window)
    {
        if (window is InfoToolTipWindow || !isAttacking)
            return;

        isAttacking = false;
        toggleTargeting.ToggledOn = false; //should invoke event
    }

    private void ButtonOff()
    {
        Interactable interactable = openButton.GetComponent<Interactable>();
        if (interactable.ClickBehavior == ClickBehavior.None)
            return;

        openButton.GetComponent<Interactable>().ClickBehavior = ClickBehavior.None;
        buttonBlock.Color = ColorManager.GetColor(ColorCode.buttonGreyOut);
    }

    private void ButtonOn()
    {
        spaceLaserUnlocked = true;

        Interactable interactable = openButton.GetComponent<Interactable>();
        if (interactable.ClickBehavior == ClickBehavior.OnRelease)
            return;

        openButton.GetComponent<Interactable>().ClickBehavior = ClickBehavior.OnRelease;

        if (!StateOfTheGame.tutorialSkipped && !SaveLoadManager.Loading)
        {
            ButtonHighlightAnimation animation = new ButtonHighlightAnimation()
            {
                startSize = new Vector3(50, 50, 0),
                endSize = new Vector3(50, 50, 0) * 1.1f,
                startColor = ColorManager.GetColor(ColorCode.callOut),
                endColor = ColorManager.GetColor(ColorCode.callOut),
                endAlpha = 0.5f,
                uIBlock = buttonBlock
            };
            
            ButtonIndicator.IndicatorButton(buttonBlock);
            animationHandle = animation.Loop(1f, -1);
            OpenWindow(false);
        }
        else
        {
            buttonBlock.Color = Color.white;
        }

    }

    private void UpdateStatsText()
    {
        float reloadTime = Mathf.RoundToInt(projectileStats.GetStat(Stat.reloadTime) * 10) / 10;
        string statsString = $"Charges: {count}\nBurst: {projectileStats.GetStat(Stat.burst)}\nDamage: {projectileStats.GetStat(Stat.damage)}\nRecharge: {reloadTime}s";
        this.statsText.Text = statsString;
    }

    public void UpdateBindingDisplay()
    {
        var displayString = string.Empty;
        var deviceLayoutName = default(string);
        var controlPath = default(string);

        // Get display string from action.
        var action = m_Action?.action;
        if (action != null)
        {
            var bindingIndex = action.bindings.IndexOf(x => x.id.ToString() == m_BindingId);
            if (bindingIndex != -1)
                displayString = action.GetBindingDisplayString(bindingIndex, out deviceLayoutName, out controlPath, 0);
        }

        // Set on label (if any).
        if (hotKeyTip != null)
            hotKeyTip.Text = $"Press {displayString} to Toggle";

    }

    private const string SPACE_LASER_UNLOCKED = "SpaceLaserUnlocked";

    public void RegisterDataSaving()
    {
        SaveLoadManager.RegisterData(this);
    }

    public void Save(string savePath, ES3Writer writer)
    {
        writer.Write<bool>(SPACE_LASER_UNLOCKED, spaceLaserUnlocked);
    }

    public IEnumerator Load(string loadPath, Action<string> postUpdateMessage)
    {
        if(ES3.KeyExists(SPACE_LASER_UNLOCKED, loadPath))
            spaceLaserUnlocked = ES3.Load(SPACE_LASER_UNLOCKED, loadPath, false);

        if (spaceLaserUnlocked)
            ButtonOn();

        yield return null;
    }
}
