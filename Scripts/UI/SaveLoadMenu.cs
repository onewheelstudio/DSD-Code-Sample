using Nova;
using NovaSamples.UIControls;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SaveLoadMenu : WindowPopup
{
    [SerializeField] private SaveLoadMode mode = SaveLoadMode.None;
    [SerializeField] private TextBlock headerText;
    [SerializeField] private ListView saveLoadList;
    private List<SaveFileData> saveFiles;
    private string fileToLoad;
    private SaveLoadManager saveLoadManager;

    [Header("Hotkeys")]
    public InputActionReference OpenLoad;
    public InputActionReference OpenSave;


    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;

    [Header("Input")]
    [SerializeField] private TextField textField;
    [SerializeField] private TextBlock placeHolderText;
    private Interactable fileNameInteractable;
    private FeedBackWindow feedBackWindow;

    private void Awake()
    {
        saveLoadList.AddDataBinder<SaveFileData, SaveLoadButton>(PopulateSaveFileData);
        fileNameInteractable = textField.GetComponent<Interactable>();
        saveLoadManager = FindFirstObjectByType<SaveLoadManager>();
        feedBackWindow = FindFirstObjectByType<FeedBackWindow>(FindObjectsInactive.Include);
        if(clipMask == null)
            clipMask = GetComponent<ClipMask>();

        if (SceneManager.GetActiveScene().buildIndex == 0)
            mode = SaveLoadMode.Load;
        else
            mode = SaveLoadMode.Save;
    }

    private void Start()
    {
        CloseWindow();
        GetSaveFileData();
    }

    private new void OnEnable()
    {
        base.OnEnable();
        closeButton.Clicked += CloseWindow;
        saveButton.Clicked += SaveFile;
        loadButton.Clicked += LoadFile;

        OpenLoad.action.performed += x => OpenWindow(SaveLoadMode.Load);
        OpenSave.action.performed += x => OpenWindow(SaveLoadMode.Save);
        OpenLoad.action.Enable();
        OpenSave.action.Enable();

        SaveLoadManager.LoadComplete += CloseWindow;
        SaveLoadManager.SaveComplete += CloseWindow;
    }

    private new void OnDisable()
    {
        closeButton.Clicked -= CloseWindow;
        saveButton.Clicked -= SaveFile;
        loadButton.Clicked -= LoadFile;
        OpenLoad.action.Disable();
        OpenSave.action.Disable();
        base.OnDisable();

        SaveLoadManager.LoadComplete -= CloseWindow;
        SaveLoadManager.SaveComplete -= CloseWindow;
    }

    private void PopulateSaveFileData(Data.OnBind<SaveFileData> evt, SaveLoadButton target, int index)
    {
        target.filename.Text = evt.UserData.fileName;
        target.filename.Color = ColorManager.GetColor(ColorCode.techCredit);
        if (evt.UserData.timeStamp > DateTime.MinValue)
            target.timeStamp.Text = evt.UserData.timeStamp.ToString("g");
        else
            target.timeStamp.Text = "---";
        //target.timeStamp.Color = ColorManager.GetColor(ColorCode.repuation);

        target.button.RemoveAllListeners();
        target.button.Clicked += () => SetFileName(evt.UserData.fileName);
        target.button.DoubleClicked += () => LoadFile();
        target.deleteButton.RemoveAllListeners();
        target.deleteButton.Clicked += () => DeleteFile(evt.UserData.fileName);
        target.sendReportButton.RemoveAllListeners();
        target.sendReportButton.Clicked += () => feedBackWindow.OpenToEmailReport(evt.UserData.fileName);
        target.sendReportButton.Clicked += CloseWindow;
    }

    private void DeleteFile(string fileName)
    {
        ES3.DeleteFile(SaveLoadManager.DirectoryPath + fileName + ".ES3");
        SaveFileData sfd = saveFiles.Find(x => x.fileName == fileName);
        saveFiles.Remove(sfd);
        OpenWindow();
    }

    private void LoadSaveFileData()
    {
        if (saveFiles == null || saveFiles.Count == 0)
            return;

        saveFiles = saveFiles.OrderByDescending(x => x.timeStamp).ToList();
        saveLoadList.SetDataSource(saveFiles);
        novaGroup.UpdateInteractables();
    }

    private async Awaitable GetSaveFileData()
    {
        if (!ES3.DirectoryExists(SaveLoadManager.DirectoryPath))
            return;

        await Awaitable.BackgroundThreadAsync();

        string[] fileNames = ES3.GetFiles(SaveLoadManager.DirectoryPath);
        saveFiles = new();
        foreach (var fileName in fileNames)
        {
            if (fileName.Contains(".tmp") || fileName.Contains(".TMP"))
                continue;

            SaveFileData saveFileData = new SaveFileData();
            saveFileData.fileName = fileName.Replace(".ES3", "").Replace(".es3", "");

            if (!SaveLoadManager.FileIsValid(fileName))
                continue;

            if (ES3.KeyExists("Save DateTime", SaveLoadManager.DirectoryPath + fileName))
                saveFileData.timeStamp = ES3.Load<DateTime>("Save DateTime", SaveLoadManager.DirectoryPath + fileName);
            else
                saveFileData.timeStamp = DateTime.MinValue;

            saveFiles.Add(saveFileData);

            //sometimes file validation is slow
            //update list in real time
            if(isOpen)
                saveLoadList.SetDataSource(saveFiles);
        }

        await Awaitable.MainThreadAsync();
        Debug.Log("Save File Validation Complete");
        novaGroup.UpdateInteractables();
    }

    public void OpenSaveWindow()
    {
        if(!DayNightManager.isDay)
        {
            MessageData messageData = new MessageData();
            messageData.message = "Can not save at night.";
            messageData.messageColor = ColorManager.GetColor(ColorCode.red);
            messageData.waitUntil = () => DayNightManager.isDay;
            MessagePanel.ShowMessage(messageData);
            return;
        }

        OpenWindow(SaveLoadMode.Save);
    }

    public void OpenLoadWindow()
    {
        OpenWindow(SaveLoadMode.Load);
    }

    public void OpenWindow(SaveLoadMode mode)
    {
        this.mode = mode;

        if (instanceIsOpen && mode == SaveLoadMode.Save)
            DoAutoSave();
        else if(mode != SaveLoadMode.None)
            OpenWindow();
    }

    private void DoAutoSave()
    {
        saveLoadManager.AutoSave();
    }

    public override void OpenWindow()
    {
        base.OpenWindow();
        blockWindowHotkeys = true;
        GetFileData();

        fileNameInteractable.enabled = mode == SaveLoadMode.Save ? true : false;
        saveButton.gameObject.SetActive(mode == SaveLoadMode.Save);
        loadButton.gameObject.SetActive(mode == SaveLoadMode.Load);

        if (mode == SaveLoadMode.Save)
        {
            headerText.Text = "Save Game";
            //placeHolderText.Text = ">>> File Name <<<"
        }
        else if (mode == SaveLoadMode.Load)
        {
            headerText.Text = "Load Game";
        }
    }

    //using coroutine to not block the UI
    private async void GetFileData()
    {
        LoadSaveFileData();
    }

    private void SaveFile()
    {
        if(string.IsNullOrEmpty(textField.Text))
        {
            DoAutoSave();
            return;
        }

        fileToLoad = textField.Text;
        saveLoadManager.SaveGame(fileToLoad);
        CloseWindow();
        GetSaveFileData();
    }

    private void LoadFile()
    {
        if(string.IsNullOrEmpty(fileToLoad))
        {
            MessagePanel.ShowMessage("No file selected to load", null);
            return;
        }

        if(saveLoadManager == null)
        {
            Debug.LogError("SaveLoadManager not found");
            return;
        }

        saveLoadManager.ChangeSceneAndLoadFile(fileToLoad);
        CloseWindow();
    }

    public override void CloseWindow()
    {
        //needed for auto save
        if(instanceIsOpen)
        {
            blockWindowHotkeys = false;
        }
        base.CloseWindow();
    }

    private void SetFileName(string fileName)
    {
        this.fileToLoad = fileName;
        textField.Text = fileName;
    }

    public class SaveFileData
    {
        public string fileName;
        public DateTime timeStamp;
    }

    public enum SaveLoadMode
    {
        None = 0,
        Save = 1,
        Load = 2,
    }
}
