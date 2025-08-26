using System.Net.Mail;
using UnityEngine;

public class EmailReport
{
    private static ProgressIndicator progressIndicator;

    public static async void SendReport(ReportData reportData, bool showProgress = true)
    {
        try
        {
            await SendReportAsync(reportData, showProgress);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Unable to send email report {reportData.message}");

            if (showProgress)
            {
                progressIndicator ??= GameObject.FindFirstObjectByType<ProgressIndicator>();
                progressIndicator?.StopProgress();
            }
        }
    }

    private static async Awaitable SendReportAsync(ReportData reportData, bool showProgess)
    {
        await Awaitable.NextFrameAsync();
        if(showProgess)
        {
            progressIndicator ??= GameObject.FindFirstObjectByType<ProgressIndicator>();
            progressIndicator?.StartProgress("Sending Report", null);
        }

        MailMessage mail = new MailMessage();
        SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");
        mail.From = new MailAddress("deepspacedirective@gmail.com");
        mail.To.Add("deepspacedirective@gmail.com");
        mail.Subject = $"Bug Report with Save File";
        mail.Body = "<b>Message:</b> <br/>" + reportData.message + "<br/> <br/>" + GetSystemStats();
        mail.IsBodyHtml = true;

        //files to send
        //send player log
        string logFileName = Application.persistentDataPath + "/Player.log";
        if(System.IO.File.Exists(logFileName))
        {
            //need to make copy as file appears to be locked in standalone build
            string tempLogPath = Application.persistentDataPath + "/TempPlayer.log";
            System.IO.File.Copy(logFileName, tempLogPath, true);  // Overwrite if needed

            Attachment attachment = new Attachment(tempLogPath);
            mail.Attachments.Add(attachment);
        }

        //send previous player log
        logFileName = Application.persistentDataPath + "/Player-prev.log";
        if(System.IO.File.Exists(logFileName))
            mail.Attachments.Add(new Attachment(logFileName));

        //send save file
        if (!string.IsNullOrEmpty(reportData.fileName) && ES3.FileExists("SavedGames/" + reportData.fileName + ".ES3"))
            mail.Attachments.Add(new Attachment(Application.persistentDataPath +$"/SavedGames/{reportData.fileName}.ES3"));

        SmtpServer.Port = 587;
        SmtpServer.Credentials = new System.Net.NetworkCredential("deepspacedirective", "djgt vact lxnt dwys");
        SmtpServer.EnableSsl = true;

        //use background thread for sending email
        //avoids blocking main thread while uploading files
        await Awaitable.BackgroundThreadAsync();
        SmtpServer.Send(mail);
        await Awaitable.MainThreadAsync();

        if(showProgess)
            progressIndicator?.StopProgress();
    }

    private static string GetSystemStats()
    {
        string stats = "<b>System Details:</b> <br/>" + SystemInfo.operatingSystem + "<br/>"
                     + SystemInfo.systemMemorySize.ToString() + "<br/>"
                     + SystemInfo.processorType + "<br/>"
                     + Screen.currentResolution.ToString() + "<br/>"
                     + SystemInfo.graphicsDeviceName + "<br/>"
                     + SystemInfo.graphicsMemorySize.ToString() + "<br/>";

        return stats;
    }

}

public struct ReportData
{
    public string message;
    public string user;
    public string fileName;
}
