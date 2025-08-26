using HexGame.Grid;
using System.Collections;
using UnityEngine;

public class StartSceneMiningDrone : MonoBehaviour
{
    [SerializeField] private Hex3 location;
    private Drone drone;
    // Start is called before the first frame update
    void Awake()
    {
        drone = this.GetComponent<Drone>();
    }

    private void OnEnable()
    {
        StartCoroutine(DoDroneStuff());
    }

    private IEnumerator DoDroneStuff()
    {
        while(this.enabled)
        {
            yield return drone.DoDroneAction(location);
        }
    }
}
