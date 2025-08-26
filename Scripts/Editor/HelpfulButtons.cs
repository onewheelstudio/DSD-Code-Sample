
using Nova;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class HelpfulButtons : OdinEditorWindow
{
    [MenuItem("Tools/Helpful Buttons")]
    private static void OpenWindow()
    {
        GetWindow<HelpfulButtons>().Show();
    }

    [ButtonGroup]
    private void StartScene()
    {
        LoadScene("Assets/Scenes/Start Scene.unity");
    }

    [ButtonGroup]
    private void GameScene()
    {
        LoadScene("Assets/Scenes/Game Scene.unity");
    }

    [ButtonGroup]
    private void TestScene()
    {
        LoadScene("Assets/Scenes/Testing/Test Scene.unity");
    }

    private void LoadScene(string scenePath)
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
    }

    [ButtonGroup("Data")]
    private void PatchNotes()
    {
        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath("Assets/Prefabs/Patch Notes.asset");
    }

    [ButtonGroup("Data")]
    private void ColorData()
    {
        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath("Assets/Prefabs/Color Data.asset");
    }

    [ButtonGroup("Data")]
    private void UnitImages()
    {
        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath("Assets/Prefabs/Units/Unit Images.asset");
    }
    
    [ButtonGroup("Data")]
    private void GameSettings()
    {
        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath("Assets/Scripts/Managers/GameSettings.asset");
    }

    [ButtonGroup("Scene Clean Up")]
    private void ToggleOffNavigation()
    {
        Interactable[] uIBlock2Ds = FindObjectsOfType<Interactable>(true);
        foreach (Interactable block in uIBlock2Ds)
        {
            block.Navigable = false;
        }
    }

    [ButtonGroup("Scene Clean Up")]
    private void HideAllWindows()
    {
        WindowPopup[] windows = FindObjectsOfType<WindowPopup>(true);

        foreach (WindowPopup window in windows)
        {
            window.CloseWindow();
        }
    }
}


