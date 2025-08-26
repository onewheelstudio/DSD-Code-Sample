using NovaSamples.UIControls;
using UnityEngine;

public class StartSceneButtons : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private GameSettings gameSettings;
    private int currentIndex;

    private void OnEnable()
    {
        playButton.Clicked += Play;
        loadGameButton.Clicked += LoadGame;
        newGameButton.Clicked += NewGame;
    }

    private void OnDisable()
    {
        playButton.Clicked -= Play;
        loadGameButton.Clicked -= LoadGame;
        newGameButton.Clicked -= NewGame;
    }

    private void Play()
    {
        if(!ES3.FileExists(GameConstants.preferencesPath))
        {
            NewGame();
            return;
        }

        if (!ES3.KeyExists(SaveLoadManager.LAST_SAVE_FILE, GameConstants.preferencesPath))
        {
            NewGame();
            return;
        }

        string fileToLoad = ES3.Load<string>(SaveLoadManager.LAST_SAVE_FILE, GameConstants.preferencesPath);
        if (SaveLoadManager.SaveFileExists(fileToLoad) && SaveLoadManager.FileIsValid(fileToLoad))
        {
            FindFirstObjectByType<SaveLoadManager>().ChangeSceneAndLoadFile(fileToLoad);
        }
        else
        {
            NewGame();
        }
    }

    private void LoadGame()
    {
        FindFirstObjectByType<SaveLoadMenu>().OpenWindow();
    }

    private void NewGame()
    {
        LoadingScreenManager.StartLoadingScreen();
    }

}
