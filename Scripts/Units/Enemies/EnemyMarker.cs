using HexGame.Units;
using UnityEngine;

public class EnemyMarker : MonoBehaviour
{
    [SerializeField] private Transform marker;

    private void OnEnable()
    {
        DayNightManager.toggleDay += TurnOff;
        MessageData messageData = new MessageData();
        messageData.message = "Enemy incursion detected";
        messageData.messageObject = this.gameObject;
        messageData.messageColor = ColorManager.GetColor(ColorCode.offPriority);
        messageData.waitUntil = () => DayNightManager.isDay;

        MessagePanel.ShowMessage(messageData);
    }

    private void OnDisable()
    {
        DayNightManager.toggleDay -= TurnOff;
    }

    private void TurnOff(int dayNumber)
    {
        this.gameObject.SetActive(false);
    }

    private void TurnOff()
    {
        this.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Unit>())
            TurnOff();

    }
}
