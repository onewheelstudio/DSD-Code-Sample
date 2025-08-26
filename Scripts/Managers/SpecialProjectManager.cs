using HexGame.Resources;
using HexGame.Units;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecialProjectManager : MonoBehaviour, ISaveData
{
    [SerializeField] private List<ProjectData> projects = new List<ProjectData>();
    private SpecialProjectBehavior SPB;
    private ProjectData currentProject;
    private DirectiveMenu directiveMenu;
    private static int repToBuildLift = 3500;
    public static int RepToBuildLift => repToBuildLift;

    private void Awake()
    {
        RegisterDataSaving();
        directiveMenu = FindFirstObjectByType<DirectiveMenu>();
    }

    private void OnEnable()
    {
        SpecialProjectBehavior.Built += BargeBuilt;
        SpecialProjectBehavior.ProjectComplete += ProjectComplete;
        SpecialProjectBehavior.OrbitalLiftDestroyed += OrbitalLiftDestroyed;

        CheatCodes.AddButton(() => AssignProject(projects[0]), "Assign Project");
    }

    private void OnDisable()
    {
        ReputationManager.reputationChanged -= ReputationChanged;
        SpecialProjectBehavior.Built -= BargeBuilt;
        SpecialProjectBehavior.ProjectComplete -= ProjectComplete;
        SpecialProjectBehavior.OrbitalLiftDestroyed -= OrbitalLiftDestroyed;
    }

    private void OrbitalLiftDestroyed()
    {
        directiveMenu.ProjectFailed();
    }

    private void ProjectComplete(SpecialProjectProduction production)
    {
        if(currentProject == null)
            return;

        if (currentProject.project != production)
            return;

        currentProject.completed = true;
        currentProject = null;
    }

    private void BargeBuilt(SpecialProjectBehavior SPB)
    {
        ReputationManager.reputationChanged += ReputationChanged;
        this.SPB = SPB;
        //calling this to jump start the first project
        ReputationChanged(ReputationManager.Reputation);
    }


    private void ReputationChanged(int reputation)
    {
        if (SPB == null)
            return;

        if (currentProject != null && !currentProject.completed)
            return;

        foreach (var project in projects)
        {
            if (reputation < project.reputation + repToBuildLift)
                continue;

            if (project.assigned || project.completed)
                continue;

            AssignProject(project);
        }
    }

    private void AssignProject(ProjectData project)
    {
        currentProject = project;
        currentProject.assigned = true;
        SPB.AssignProject(currentProject.project);
        CommunicationMenu.AddCommunication(currentProject.communication);
    }

    public void RegisterDataSaving()
    {
        SaveLoadManager.RegisterData(this,10);
    }

    private const string PROJECT_SAVE_PATH = "SpecialProjectStatus";

    public void Save(string savePath, ES3Writer writer)
    {
        List<(bool assigned, bool completed)> projectStatus = new List<(bool, bool)>();

        foreach (var project in projects)
        {
            projectStatus.Add((project.assigned, project.completed));
        }

        writer.Write<List<(bool,bool)>>(PROJECT_SAVE_PATH, projectStatus);

    }

    public IEnumerator Load(string loadPath, Action<string> postUpdateMessage)
    {
        if(SPB == null)
            yield break;

        if (ES3.KeyExists(PROJECT_SAVE_PATH, loadPath))
        {
            List<(bool assigned, bool completed)> projectStatus = ES3.Load<List<(bool, bool)>>(PROJECT_SAVE_PATH, loadPath);
            for (int i = 0; i < projects.Count; i++)
            {
                projects[i].assigned = projectStatus[i].assigned;
                projects[i].completed = projectStatus[i].completed;

                //if we have assigned but not completed the project reassign it
                if (projects[i].assigned && !projects[i].completed)
                {
                    currentProject = projects[i];
                    currentProject.assigned = true;

                    SPB.AssignProject(currentProject.project, LoadInventory);
                }
            }

            yield return null;
        }
    }

    private Action LoadInventory;

    /// <summary>
    /// Passes an action that will get invoked at load time
    /// </summary>
    /// <param name="value"></param>
    internal void SetLiftInventory(Action value)
    {
        this.LoadInventory = value;
    }

    [System.Serializable]
    public class ProjectData
    {
        [InfoBox("Reputation required after lift unlock")]
        public int reputation;
        public SpecialProjectProduction project;
        public CommunicationBase communication;
        public bool assigned;
        public bool completed;

        [Button]
        private void Assign()
        {
            FindFirstObjectByType<SpecialProjectManager>().AssignProject(this);
        }
    }
}
