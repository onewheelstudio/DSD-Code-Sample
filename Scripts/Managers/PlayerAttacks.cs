using Nova;
using NovaSamples.UIControls;
using OWS.Nova;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttacks : WindowPopup
{
    private UIControlActions inputActions;
    private bool isAttacking = false;
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
    private Animator openButtonAnimator;
    private bool spaceLaserUnlocked = false;

    public static event Action SpaceLaserFired;

    private void Awake()
    {
        inputActions = new UIControlActions();
        cursorManager = FindObjectOfType<CursorManager>();
        reloadProgress.Percent = 0f;
        openButtonAnimator = openButton.GetComponent<Animator>();
        ButtonOff();
    }

    public override void OnEnable()
    {
        base.OnEnable();
        inputActions.UI.SpaceLasers.performed += ToggleAttack;
        inputActions.UI.SpaceLasers.Enable();

        toggleTargeting.Toggled += SetIsAttacking;

        DayNightManager.transitionToNight += OpenWindow;
        UnlockSpaceLaserTrigger.UnlockSpaceLaser += ButtonOn;
        UnlockSpaceLaserTrigger.UnlockSpaceLaser += OpenWindow;
        ControlsManager.UIControlsUpdated += UpdateSpaceLaserBinding;

        CloseWindow();
        StartCoroutine(DoReload()); 
    }

    public override void OnDisable()
    {
        base.OnDisable();
        inputActions.UI.SpaceLasers.Disable();
        toggleTargeting.Toggled -= SetIsAttacking;
        DayNightManager.transitionToNight -= OpenWindow;
        UnlockSpaceLaserTrigger.UnlockSpaceLaser -= ButtonOn;
        UnlockSpaceLaserTrigger.UnlockSpaceLaser -= OpenWindow;
        ControlsManager.UIControlsUpdated -= UpdateSpaceLaserBinding;
        StopAllCoroutines();
    }

    private void UpdateSpaceLaserBinding(string rebinds)
    {
        inputActions.LoadBindingOverridesFromJson(rebinds);
    }

    private void ToggleAttack(InputAction.CallbackContext context)
    {
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
            MessagePanel.ShowMessage("Launch Activated", null);
        }
        else
        {
            cursorManager.SetCursor(CursorType.hex);
            cursorManager.SetCursorColor(Color.white);
            MessagePanel.ShowMessage("Launch De-Activated", null);
        }
    }

    private void Update()
    {
        if(isAttacking)
        {
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                cursorManager.SetCursor(CursorType.hex);
                isAttacking = false;
                toggleTargeting.ToggledOn = isAttacking;
                MessagePanel.ShowMessage("Launch De-Actived", null);
            }
            else if (Mouse.current.leftButton.wasPressedThisFrame && count > 0)
            {
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

    private void OpenWindow(int dayNumber, float delay)
    {
        if (dayNumber == 1)
        {
            OpenWindow();
        }
        else
            DayNightManager.transitionToNight -= OpenWindow; 
    }

    public override void OpenWindow()
    {
        if (!spaceLaserUnlocked)
            return;

        base.OpenWindow();
        UpdateStatsText();
        toggleTargeting.ToggledOn = isAttacking;
    }

    public override void CloseWindow()
    {
        base.CloseWindow();
        cursorManager.SetCursor(CursorType.hex);
        cursorManager.SetCursorColor(Color.white);
        isAttacking = false;
    }
    private void ButtonOff()
    {
        Interactable interactable = openButton.GetComponent<Interactable>();
        if (interactable.ClickBehavior == ClickBehavior.None)
            return;

        openButton.GetComponent<Interactable>().ClickBehavior = ClickBehavior.None;
        openButtonAnimator.SetTrigger("ButtonOff");
    }

    private void ButtonOn()
    {
        spaceLaserUnlocked = true;

        Interactable interactable = openButton.GetComponent<Interactable>();
        if (interactable.ClickBehavior == ClickBehavior.OnRelease)
            return;

        openButton.GetComponent<Interactable>().ClickBehavior = ClickBehavior.OnRelease;
        openButtonAnimator.SetTrigger("Highlight");
    }

    private void UpdateStatsText()
    {
        string statsString = $"Charges: {count}\nBurst: {projectileStats.GetStat(Stat.burst)}\nDamage: {projectileStats.GetStat(Stat.damage)}\nRecharge: {projectileStats.GetStat(Stat.reloadTime)}s";
        this.statsText.Text = statsString;
    }
}
