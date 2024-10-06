using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class TestMessageSender : MonoBehaviour
{
    [Button]
    private void SendPlayerMessage()
    {
        MessagePanel.ShowMessage($"Hello I'm {this.gameObject.name}", this.gameObject);
    }
}
