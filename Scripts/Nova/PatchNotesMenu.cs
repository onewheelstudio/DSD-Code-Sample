using Nova;
using Sirenix.OdinInspector;
using UnityEngine;

public class PatchNotesMenu : WindowPopup
{
    [SerializeField, AssetsOnly] private PatchNotes patchNotes;
    [SerializeField] private ListView listView;

    private void Start()
    {
        listView.AddDataBinder<PatchNotes.NoteContainer, PatchNoteVisuals>(BindPatchNotes);
        listView.SetDataSource(patchNotes.GetAllNotes());

        //only show patch notes if the player has played before
        bool hasPlayedBefore = ES3.Load<bool>("HasPlayedBefore", GameConstants.preferencesPath, false);
        if(!hasPlayedBefore)
        {
            CloseWindow();
            return;
        }

        if (patchNotes.IsLatestRead())
            CloseWindow();
        else
        {
            OpenWindow();
            patchNotes.SetLatestAsRead();
        }
    }

    private void BindPatchNotes(Data.OnBind<PatchNotes.NoteContainer> evt, PatchNoteVisuals target, int index)
    {
        PatchNotes.NoteContainer noteContainer = evt.UserData;
        target.version.Text = "Version " + noteContainer.version.ToString();
        target.notes.Text = noteContainer.notes;
    }

    [Button]
    private void ResetPlayedBefore()
    {
        ES3.Save("HasPlayedBefore", false, GameConstants.preferencesPath);
    }
}
