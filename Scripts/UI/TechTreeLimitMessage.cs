using Nova;
using UnityEngine;

public class TechTreeLimitMessage : MonoBehaviour
{
    [SerializeField] private GameSettings gameSettings;
    private TextBlock message;

    private void Awake()
    {
        message = GetComponent<TextBlock>();
    }

    private void OnEnable()
    {
        if(gameSettings.IsDemo)
        {
            message.Text = $"Demo Tech Tree Limited to Tier {gameSettings.MaxTierForDemo} Upgrades";
        }
        else if(gameSettings.IsEarlyAccess)
        {
            message.Text = $"Tech Tree Currently Limited to Tier {gameSettings.MaxTierForEarlyAccess} Upgrades";
        }
        else
        {
            this.gameObject.SetActive(false);
        }
    }
}
