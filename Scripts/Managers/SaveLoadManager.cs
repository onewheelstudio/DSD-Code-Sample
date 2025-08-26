using Nova;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveLoadManager : MonoBehaviour
{
    [SerializeField] private GameSettings gameSettings;
    [SerializeField] private PatchNotes patchNotes;
    [SerializeField] private string fileName = "TestDataSaveFile";
    private static string DIRECTORY_PATH = "SavedGames/";
    public static string LAST_SAVE_FILE = "LastSaveFile";
    public static string DirectoryPath => DIRECTORY_PATH;

    public static event Action<float> UpdateProgress;
    public static event Action<string> postUpdateMessage;

    public static bool Loading = false;
    public static bool Saving = false;
    public static event Action LoadingGame;
    public static event Action SavingGame;
    public static event Action LoadComplete;
    public static event Action SaveComplete;

    private List<SaveData> data = new ();
    private static SaveLoadManager instance;

    [SerializeField] private GameObject loadingScreenPrefab;
    private LoadingScreen loadingScreen;
    private int currentIndex = 0;
    [SerializeField] private int sceneIndex = 0;
    private int maxAutoSaves = 5;
    public static bool loadedGame = false; //for analytics
    [SerializeField] private bool doAutoSave = true;
    [SerializeField] private int autoSaveInterval = 1; //in days
    public static event Action DoingAutoSave;
    private DayNightManager dayNightManager;
    private static string persistntDataPath = string.Empty;

    private void Awake()
    {
        if (data == null)
            data = new();

        loadedGame = false;
        persistntDataPath = Application.persistentDataPath;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        GameSettingsWindow.UseAutoSave += ToggleAutoSave;
        GameSettingsWindow.AutoSaveIntervalChanged += AutoSaveIntervalChanged;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        GameSettingsWindow.UseAutoSave -= ToggleAutoSave;
        GameSettingsWindow.AutoSaveIntervalChanged -= AutoSaveIntervalChanged;
        DayNightManager.toggleDay -= AutoSave;
    }


    private void OnSceneLoaded(Scene scene, LoadSceneMode arg1)
    {
        if (scene.name != "Game Scene")
        {
            DayNightManager.toggleDay -= AutoSave;
            data = new();
            //resolves some issues when loading a game after returning to the start scene
            SaveComplete = null;
            LoadComplete = null;
            return;
        }
        else
        {
            DayNightManager.toggleDay += AutoSave;
        }    
    }

    private void AutoSave(int dayNumber)
    {
        if(!doAutoSave || Saving)
            return;

        if (dayNumber % autoSaveInterval == 0)
        {
            if(dayNightManager == null)
                dayNightManager = FindFirstObjectByType<DayNightManager>(FindObjectsInactive.Exclude);

            DoingAutoSave?.Invoke();
            AutoSave();
        }
    }

    private void AutoSaveIntervalChanged(int autoSaveInterval)
    {
        this.autoSaveInterval = autoSaveInterval;
    }

    private void ToggleAutoSave(bool useAutoSave)
    {
        this.doAutoSave = useAutoSave;
    }

    [Button]
    public void AutoSave()
    {
        if (Saving)
            return;
        Saving = true;

        if (!ES3.DirectoryExists(DIRECTORY_PATH))
        {
            SaveGame($"AutoSave 1");
            return;
        }

        List<string> fileNames = ES3.GetFiles(DIRECTORY_PATH).Where(f => f.Contains("AutoSave")).ToList();

        if (fileNames.Count < maxAutoSaves)
        {
            //check for missing or deleted auto saves
            for (int i = 0; i < maxAutoSaves; i++)
            {
                if (fileNames.Contains($"AutoSave {i + 1}.ES3"))
                    continue;

                SaveGame($"AutoSave {i + 1}");
                return;
            }
        }
        else
        {
            string OldestFile = fileNames.OrderBy(f => ES3.Load<DateTime>("Save DateTime", DIRECTORY_PATH + f, new DateTime())).FirstOrDefault();
            OldestFile = OldestFile.Replace(".ES3", "");
            StartCoroutine(SaveOverTime(OldestFile));
        }

        Saving = false;
    }

    public void SaveGame(string filename)
    {
        this.fileName = filename;
        SaveGame();
    }

    [Button]
    private void SaveGame()
    {
        if (Saving) //are we double saving? Cause of sharing violation?
            return;
        Saving = true;
        SavingGame?.Invoke();
        StartCoroutine(SaveOverTime(fileName));
    }

    private IEnumerator SaveOverTime(string fileName)
    {
        ES3.Save<bool>("SaveComplete", false, DIRECTORY_PATH + fileName + ".ES3");
        ES3.Save<DateTime>("Save DateTime", DateTime.Now, DIRECTORY_PATH + fileName + ".ES3");
        ES3.Save<string>("Save Date", DateTime.Now.ToString("g"), DIRECTORY_PATH + fileName + ".ES3");
        yield return null;

        using (var writer = ES3Writer.Create(new ES3Settings(DIRECTORY_PATH + fileName + ".ES3")))
        {
            writer.Write<float>("Build Version", patchNotes.GetLatestVersion());
            //writer.Write<string>("Save Date", DateTime.Now.ToString("g"));
            //writer.Write<bool>("SaveComplete", false);

            for (int i = 0; i < data.Count; i++)
            {
                if (data[i] == null || data[i].saveData == null)
                    continue;

                float time = Time.realtimeSinceStartup;
                data[i].saveData.Save(DIRECTORY_PATH + fileName + ".ES3", writer);
                yield return null;
                UpdateProgress?.Invoke(i / (float)data.Count);
                //Debug.Log($"Save time {Time.realtimeSinceStartup - time} - {data[i].saveData.GetType().Name}");
            }

            writer.Write<bool>("SaveComplete", true);
            writer.Save();
            writer.Dispose();
        }
        ES3.Save<string>(LAST_SAVE_FILE, fileName, GameConstants.preferencesPath);
        Saving = false;
        SaveComplete?.Invoke();
        MessagePanel.ShowMessage("Save Complete", null);
    }

    public void ChangeColony(string colonyName)
    {
        currentIndex = SceneManager.GetActiveScene().buildIndex;
        StartCoroutine(LoadColony(colonyName, currentIndex));
    }

    IEnumerator LoadColony(string colonyName, int currentScene)
    {
        //give the instancer one frame to shut down
        GPUInstancer.GPUInstancerPrefabRuntimeHandler[] prefabManager = FindObjectsOfType<GPUInstancer.GPUInstancerPrefabRuntimeHandler>();
        foreach (var item in prefabManager)
        {
            item.enabled = false;
        }

        yield return null;

        //scene 2 is the colony loading scene
        AsyncOperation async = SceneManager.LoadSceneAsync(2, LoadSceneMode.Additive);

        while (!async.isDone)
        {
            yield return null;
        }

        //unload current scene
        async = SceneManager.UnloadSceneAsync(currentScene);
        while (!async.isDone)
        {
            yield return null;
        }

        sceneIndex = 1; //scene 1 is the colony scene
        ChangeSceneAndLoadFile(colonyName);
    }

    public void ChangeSceneAndLoadFile(string filename)
    {
        this.fileName = filename;
        Loading = true;
        currentIndex = SceneManager.GetActiveScene().buildIndex;

        FindFirstObjectByType<IntroFade>().FadeToBlack(() =>
        {
            //if (this.loadingScreen == null)
            //    this.loadingScreen = Instantiate(this.loadingScreenPrefab).GetComponentInChildren<LoadingScreen>();

            StartCoroutine(LoadAsync());
        });

    }
    IEnumerator LoadAsync()
    {
        //give the instancer one frame to shut down
        GPUInstancer.GPUInstancerPrefabRuntimeHandler[] prefabManager = FindObjectsOfType<GPUInstancer.GPUInstancerPrefabRuntimeHandler>();
        foreach (var item in prefabManager)
        {
            item.enabled = false;
        }
        yield return null;


 
        //scene 2 is the colony loading scene
        AsyncOperation async = SceneManager.LoadSceneAsync(2, LoadSceneMode.Single);
        while (!async.isDone)
        {
            yield return null;
        }

        ////unload current scene - game scene or start scene
        //async = SceneManager.UnloadSceneAsync(currentIndex);
        //while (!async.isDone)
        //{
        //    yield return null;
        //}

        //load the game scene
        yield return null;
        data.Clear();
        async = SceneManager.LoadSceneAsync(sceneIndex, LoadSceneMode.Additive);
        while (!async.isDone)
        {
            yield return null;
        }
        SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(sceneIndex));
        LoadingScreen ls = FindAnyObjectByType<LoadingScreen>();
        ls.PlayIntro();
        IntroFade fade = ls.GetComponentInChildren<IntroFade>();
        fade.FadeToTransparent();

        Loading = true;
        LoadingGame?.Invoke();
        data = data.OrderBy(x => x.priority).ToList();
        yield return StartCoroutine(LoadOverTime(data));

        yield return new WaitForSeconds(2f);
        //this.loadingScreen.transform.parent.gameObject.SetActive(false);
        Loading = false;
    }

    public void LoadGame(string filename)
    {
        this.fileName = filename;
        LoadGame();
    }

    [Button]
    public void LoadGame()
    {
        Loading = true;
        LoadingGame?.Invoke();
        data = data.OrderBy(x => x.priority).ToList();
        StartCoroutine(LoadOverTime(data));
    }

    private IEnumerator LoadOverTime(List<SaveData> data)
    {
        int maxAttempts = 20;
        int attempts = 0;
        while(IsFileLocked(DirectoryPath + fileName + ".ES3") && attempts <= maxAttempts)
        {
            attempts++;
            yield return null;
        }

        if(attempts > maxAttempts)
        {
            Debug.LogError($"File {fileName} is locked after {maxAttempts} attempts, cannot load");
            Loading = false;
            SceneManager.LoadScene(0); //attempt to head back to start menu
            yield break;
        }

        ES3.CopyFile(DIRECTORY_PATH + fileName + ".ES3", DIRECTORY_PATH + "TempFile.ES3");
        float time = Time.timeSinceLevelLoad;

        for(int i = 0; i < data.Count; i++)
        {
            if(data[i] == null || data[i].saveData == null)
                continue;

            //frame delays added for io sharing conflicts...?
            yield return null;
            time = Time.timeSinceLevelLoad;
            PostUpdateMessage($"Loading {i + 1} of {data.Count}");//clear the message
            yield return data[i].saveData.Load(DIRECTORY_PATH + "TempFile.ES3", PostUpdateMessage);
            //Debug.Log($"Time: {Time.timeSinceLevelLoad - time} - {data[i].saveData.GetType().Name}");
            yield return null;
            UpdateProgress?.Invoke(i / (float)data.Count);
        }
        PostUpdateMessage("Loading Complete");
        Loading = false;
        LoadComplete?.Invoke();
        loadedGame = true;
    }

    private void PostUpdateMessage(string message)
    {
        postUpdateMessage?.Invoke(message);
    }

    public static void RegisterData(ISaveData ISaveData, float priority = 0f)
    {
        if(instance == null)
            instance = FindObjectOfType<SaveLoadManager>();
        if(instance == null)
            return;

        SaveData SaveData = new SaveData();
        SaveData.saveData = ISaveData;
        SaveData.priority = priority;
        instance.data.Add(SaveData);
    }

    public class SaveData
    {
        public ISaveData saveData;
        public float priority;
    }

    public static bool SaveFileExists(string filename)
    {
        return ES3.FileExists(DIRECTORY_PATH + filename + ".ES3");
    }

    public static bool FileIsValid(string fileName)
    {
        if (fileName.Contains("TempFile.ES3"))
            return false;
        
        if (fileName.Contains("steam_autocloud.vdf"))
            return false;

        if (!fileName.Contains(".es3") && !fileName.Contains(".ES3"))
            fileName = fileName + ".ES3";

        //make copy to try and avoid sharing violations
        string tempFileName = DIRECTORY_PATH + "TempValidationFile.ES3";
        ES3.CopyFile(DIRECTORY_PATH + fileName, tempFileName);

        if(IsFileLocked(tempFileName))
        {
            return false;
        }

        if (ES3.KeyExists("SaveComplete", tempFileName))
        {
            if (!ES3.Load<bool>("SaveComplete", tempFileName))
            {
                //Debug.LogError($"File {fileName} is incomplete");
                return false;
            }
        }

        return true;
    }

    public static bool IsFileLocked(string path)
    {
        string fullPath = Path.Combine(persistntDataPath, path);
        if (!File.Exists(fullPath))
            return false;

        try
        {
            using (FileStream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                // If we can open with exclusive read access, it's not locked.
            }
            return false;
        }
        catch (IOException)
        {
            return true; // Sharing violation or file is locked
        }
    }
}

public interface ISaveData
{
    void RegisterDataSaving();
    void Save(string savePath, ES3Writer writer);
    IEnumerator Load(string loadPath, Action<string> postUpdateMessage);
}
