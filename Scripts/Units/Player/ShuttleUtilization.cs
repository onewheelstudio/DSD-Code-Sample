using HexGame.Units;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShuttleUtilization : MonoBehaviour, IHavePopupInfo
{
    private UnitStorageBehavior usb;
    private Queue<int> shortQueue = new Queue<int>(8);
    private Queue<int> longQueue = new Queue<int>(40);
    private Queue<int> extraLongQueue = new Queue<int>(90);
    private int shortSize = 8;
    private int longSize = 40;
    private int extraLongSize = 90;
    private int shortAccumulator;
    private int longAccumulator;
    private int extraLongAccumulator;
    [ShowInInspector]
    public float ShortAverage { get; private set; }
    [ShowInInspector]
    public float LongAverage { get; private set; }
    
    [ShowInInspector]
    public float ExtraLongAverage { get; private set; }

    [ShowInInspector]
    public float Average
    {
        get
        {
            return Mathf.Max(ShortAverage, LongAverage);
        }
    }
    private WaitForSeconds waitTime = new WaitForSeconds(1f);
    private List<PopUpInfo> popUpInfos = new List<PopUpInfo>();

    /// <summary>
    /// Computes a new windowed average each time a new sample arrives
    /// </summary>
    /// <param name="newSample"></param>
    public void TrackShuttleAvailablity(int newSample)
    {
        shortAccumulator += newSample;
        longAccumulator += newSample;
        extraLongAccumulator += newSample;
        shortQueue.Enqueue(newSample);
        longQueue.Enqueue(newSample);
        extraLongQueue.Enqueue(newSample);

        if (shortQueue.Count > shortSize)
        {
            shortAccumulator -= shortQueue.Dequeue();
        }

        if(longQueue.Count > longSize)
        {
            longAccumulator -= longQueue.Dequeue();
        }

        if(extraLongQueue.Count > extraLongSize)
        {
            extraLongAccumulator -= extraLongQueue.Dequeue();
        }

        ShortAverage = (float)shortAccumulator / (float)shortQueue.Count;
        LongAverage = (float)longAccumulator / (float)longQueue.Count;
        ExtraLongAverage = (float)extraLongAccumulator / (float)extraLongQueue.Count;
    }

    // Start is called before the first frame update
    void Start()
    {
        usb = this.GetComponent<UnitStorageBehavior>();
        StartCoroutine(CheckShuttleAvailablity());
    }

    private IEnumerator CheckShuttleAvailablity()
    {
        while(true)
        {
            yield return waitTime;

            for (int i = 0; i < usb.shuttles.Count; i++)
            {
                CargoShuttleBehavior shuttle = usb.shuttles[i];
                if (shuttle.IsAvailable())
                {
                    TrackShuttleAvailablity(0);
                }
                else
                    TrackShuttleAvailablity(1);
            }
        }
    }

    public List<PopUpInfo> GetPopupInfo()
    {
        if (popUpInfos.Count == 0)
            popUpInfos.Add(new PopUpInfo("Shuttle Utilization", 0f, PopUpInfo.PopUpInfoType.shuttleUtilization, Mathf.RoundToInt(Average * 100)));
        else
            popUpInfos[0] = new PopUpInfo("Shuttle Utilization", 0f, PopUpInfo.PopUpInfoType.shuttleUtilization, Mathf.RoundToInt(Average * 100));

        return popUpInfos;
    }
}
