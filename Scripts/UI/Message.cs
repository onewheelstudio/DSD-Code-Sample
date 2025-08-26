using DG.Tweening;
using Nova;
using NovaSamples.UIControls;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Message : UIControl<MessageVisuals>
{
    private ClipMask clipMask;
    private float displayTime = 10f;
    [SerializeField]
    [Range(0f, 3f)]
    private float fadeInTime = 0.25f;
    [SerializeField, Range(0f, 10f)]
    private float fadeOutTime;
    private Transform messageObjectPosition;
    private Action messageAction;
    private MessageVisuals messageVisuals;
    public static System.Action<Vector3> moveToObject;

    private void OnEnable()
    {
        messageVisuals = View.Visuals as MessageVisuals;
        this.GetComponent<UIBlock2D>().AddGestureHandler<Gesture.OnClick, MessageVisuals>(MessageClicked);
        displayTime = 10f;
    }


    private void OnDisable()
    {
        this.GetComponent<UIBlock2D>().RemoveGestureHandler<Gesture.OnClick, MessageVisuals>(MessageClicked);
        DOTween.Kill(this,true);
    }

    public void SetMessage(string message, GameObject gameObject)
    {
        if(clipMask == null)
            clipMask = this.GetComponent<ClipMask>();
        
        this.transform.SetAsLastSibling();
        if (gameObject != null)
            messageObjectPosition = gameObject.transform;
        else
            messageObjectPosition = null;

        messageVisuals.textBlock.Text = ">>> " + message;
        messageVisuals.textBlock.Color = Color.white;
        clipMask.DoFade(1f, fadeInTime);
        SFXManager.PlaySFX(SFXType.message);
        if(this.gameObject.activeInHierarchy) //rare case of the message panel being turned off
            StartCoroutine(TurnOff());
    }


    private IEnumerator TurnOff()
    {
        yield return null; //allow display time to get set ;)
        yield return new WaitForSeconds(displayTime);
        clipMask.DoFade(0f, fadeOutTime);
        yield return new WaitForSeconds(fadeOutTime);
        this.gameObject.SetActive(false);
        messageObjectPosition = null;
        messageAction = null;
    }

    private void MoveToMessageObject()
    {
        if(messageObjectPosition != null)
            moveToObject(messageObjectPosition.position);

        messageAction?.Invoke();
    }


    private void MessageClicked(Gesture.OnClick evt, MessageVisuals target)
    {
        StopAllCoroutines();
        clipMask.DoFade(1f, 0.3f);
        displayTime = 2f;
        StartCoroutine(TurnOff());

        SFXManager.PlaySFX(SFXType.click);
        MoveToMessageObject();
    }

    public Message SetAction(Action action)
    {
        messageAction = action;
        return this;
    }

    public Message SetDisplayTime(float displayTime)
    {
        this.displayTime = displayTime;
        return this;
    }
    internal void SetMessage(MessageData messageData)
    {
        if (clipMask == null)
            clipMask = this.GetComponent<ClipMask>();

        this.transform.SetAsLastSibling();
        if (messageData.messageObject != null)
            messageObjectPosition = messageData.messageObject.transform;
        else
            messageObjectPosition = null;

        messageVisuals.textBlock.Text = ">>> " + messageData.message.TMP_Color(messageData.messageColor);
        clipMask.DoFade(1f, fadeInTime);
        SFXManager.PlaySFX(SFXType.message);

        if(messageData.waitUntil != null)
            StartCoroutine(WaitUntil(messageData.waitUntil));
        else
            StartCoroutine(TurnOff());
    }

    private IEnumerator WaitUntil(Func<bool> waitUntil)
    {
        yield return null; //allow display time to get set ;)
        yield return new WaitUntil(waitUntil);
        clipMask.DoFade(0f, fadeOutTime);
        yield return new WaitForSeconds(fadeOutTime);
        this.gameObject.SetActive(false);
    }

}

