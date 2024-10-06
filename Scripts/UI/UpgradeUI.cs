using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI description;
    [SerializeField] private TextMeshProUGUI cost;


    [Required]
    [SerializeField]
    private Upgrade upgrade;

    public void SetUpgradeInfo(Upgrade upgrade)
    {
        if(upgrade.cost > 10000)
            this.cost.text = $"{upgrade.cost / 1000}k \n";
        else
            this.cost.text = $"{upgrade.cost} \n";

        this.upgrade = upgrade;
    }
}
