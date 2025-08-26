using Sirenix.OdinInspector;
using System;
using UnityEngine;

public class LogTracking : MonoBehaviour
{
    private string logs;
    public static event Action errorWhenLoading;

    private void OnEnable()
    {
        Application.logMessageReceivedThreaded += LogMessageReceived;
        logs = "";
    }

    private void OnDisable()
    {
        Application.logMessageReceivedThreaded -= LogMessageReceived;
    }

    [Button]
    private void LogMessageReceived(string logString, string stackTrace, LogType type)
    {
        if (logString.Contains("OperationCanceledException"))
            return;

        if (type == LogType.Error || type == LogType.Exception)
        {
                        //logs += $"Time Since Level Load: {Time.timeSinceLevelLoad / 60f}\n";  
            logs += ($"LogType: {type}, LogString: {logString}, StackTrace: {stackTrace}\n\n");
            if(SaveLoadManager.Loading)
            {
                logs = "Error when loading game\n" + logs;
                errorWhenLoading?.Invoke();
            }

            if(type == LogType.Exception)
            {
                ReportData data = new ReportData();
                data.message = logString + "<br/><br/>" + stackTrace;
                data.fileName = null;
                EmailReport.SendReport(data, false);
            }
        }
    }

    public string GetLogs()
    {
        return logs;
    }
}
