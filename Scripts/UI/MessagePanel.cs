using OWS.ObjectPooling;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

public class MessagePanel : MonoBehaviour
{
    [SerializeField]
    private GameObject messagePrefab;
    private static Transform messageContainer;

    private static ObjectPool<PoolObject> messagePool;

    private void Awake()
    {
        messagePool = new ObjectPool<PoolObject>(messagePrefab);
        messageContainer = this.transform;
    }

    [Button]
    public static Message ShowMessage(string message, GameObject messageObject)
    {
        if (messagePool == null)
            return null;

        GameObject go = messagePool.PullGameObject();
        go.transform.SetParent(messageContainer, false);
        Message messageComponent = go.GetComponent<Message>();
        messageComponent.SetMessage(message, messageObject);
        return messageComponent;
    }

    public static Message ShowMessage(MessageData messageData)
    {
        if (messagePool == null)
            return null;

        GameObject go = messagePool.PullGameObject();
        go.transform.SetParent(messageContainer, false);
        Message messageComponent = go.GetComponent<Message>();
        messageComponent.SetMessage(messageData);
        return messageComponent;
    }
}

public struct MessageData
{
    public string message;
    public GameObject messageObject;
    public Color messageColor;
    public Func<bool> waitUntil;
}
