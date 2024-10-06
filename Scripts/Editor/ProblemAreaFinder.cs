using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class ProblemAreaFinder : MonoBehaviour
{
    [Header("Update")]
    public List<MonoBehaviour> gameObjectsWithUpdate;
    [ReadOnly]
    public int updateInstances;
    [ReadOnly]
    public int activeUpdateInstances;
    public List<ScriptWithMethod> updateMethodOccurrences;

    [Space(10)]

    [Header("FixedUpdate")]
    public List<MonoBehaviour> gameObjectsWithFixedUpdate;
    [ReadOnly]
    public int fixedUpdateInstances;
    [ReadOnly]
    public int activeFixedUpdateInstances;
    public List<ScriptWithMethod> fixedUpdateMethodOccurrences;

    [Space(10)]

    [Header("FixedUpdate and Update")]
    public bool sortByPrevalence = true;

    [Button]
    void FindUpdateProblems()
    {
        gameObjectsWithUpdate = FindScriptsWithMethod("Update");
        Debug.Log($"Found {gameObjectsWithUpdate.Count} scripts with Update method:");

        gameObjectsWithFixedUpdate = FindScriptsWithMethod("FixedUpdate");
        Debug.Log($"Found {gameObjectsWithFixedUpdate.Count} scripts with FixedUpdate method:");

        updateMethodOccurrences = GetMethodOccurrences(gameObjectsWithUpdate, sortByPrevalence);
        fixedUpdateMethodOccurrences = GetMethodOccurrences(gameObjectsWithFixedUpdate, sortByPrevalence);

        foreach (var item in updateMethodOccurrences)
        {
            updateInstances += item.count;
            activeUpdateInstances += item.activeCount;
        }

        foreach (var item in fixedUpdateMethodOccurrences)
        {
            fixedUpdateInstances += item.count;
            activeFixedUpdateInstances += item.activeCount;
        }
    }

    List<MonoBehaviour> FindScriptsWithMethod(string methodName)
    {
        MonoBehaviour[] allMonoBehaviours = FindObjectsOfType<MonoBehaviour>();
        List<MonoBehaviour> scriptsWithMethod = new List<MonoBehaviour>();

        foreach (var monoBehaviour in allMonoBehaviours)
        {
            Type type = monoBehaviour.GetType();
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (method != null && method.DeclaringType == type)
            {
                scriptsWithMethod.Add(monoBehaviour);
            }
        }

        return scriptsWithMethod;
    }

    List<ScriptWithMethod> GetMethodOccurrences(List<MonoBehaviour> scripts, bool sortByPrevalence)
    {
        var methodOccurrences = new Dictionary<string, ScriptWithMethod>();

        foreach (var script in scripts)
        {
            string scriptName = script.GetType().Name;

            if (methodOccurrences.ContainsKey(scriptName))
            {
                methodOccurrences[scriptName].count++;
                if (script.isActiveAndEnabled)
                {
                    methodOccurrences[scriptName].activeCount++;
                }
            }
            else
            {
                var assetPath = UnityEditor.AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(script));
                var scriptAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);

                methodOccurrences[scriptName] = new ScriptWithMethod
                {
                    name = scriptName,
                    script = scriptAsset,
                    count = 1,
                    activeCount = script.isActiveAndEnabled ? 1 : 0,
                };
            }
        }

        var sortedList = methodOccurrences.Values.ToList();

        if (sortByPrevalence)
        {
            sortedList = sortedList.OrderByDescending(kvp => kvp.count)
                             .ThenBy(kvp => kvp.name)
                             .ToList();
        }
        else
        {
            sortedList = sortedList.OrderBy(kvp => kvp.name).ToList();
        }

        foreach (var item in sortedList)
        {
            item.name = $"{item.name}: {item.activeCount}/{item.count}";
        }

        return sortedList;
    }

    [Space(10)]

    [Header("Object Nesting and Instances")]
    public int depthThreshold = 5;
    public int instanceThreshold = 100;
    public List<ProblemObjects> problemObjects;

    [Button]
    public void FindHierarchyDepthProblems()
    {
        problemObjects = new List<ProblemObjects>();
        Dictionary<string, int> instanceCounts = new Dictionary<string, int>();

        foreach (GameObject rootObject in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            TraverseHierarchy(rootObject, 0, instanceCounts);
        }

        CombineProblemObjects();
        SortProblemObjectsByInstances();

        foreach (var obj in problemObjects)
        {
            obj.name = $"{obj.name}: {obj.depthCount} (Instances: {obj.instanceCount})";
        }
    }

    void TraverseHierarchy(GameObject obj, int currentDepth, Dictionary<string, int> instanceCounts)
    {
        if (currentDepth > depthThreshold)
        {
            problemObjects.Add(new ProblemObjects
            {
                name = obj.name,
                depthCount = currentDepth,
                gameObject = obj,
                parent = obj.transform.parent ? obj.transform.parent.gameObject.name : "Root"
            });
        }

        string objType = obj.GetType().Name;
        if (instanceCounts.ContainsKey(objType))
        {
            instanceCounts[objType]++;
        }
        else
        {
            instanceCounts[objType] = 1;
        }

        foreach (Transform child in obj.transform)
        {
            TraverseHierarchy(child.gameObject, currentDepth + 1, instanceCounts);
        }
    }

    void CombineProblemObjects()
    {
        var combinedProblems = new Dictionary<string, ProblemObjects>();

        foreach (var obj in problemObjects)
        {
            string key = $"{obj.name}_{obj.parent}_{obj.depthCount}";
            if (combinedProblems.ContainsKey(key))
            {
                combinedProblems[key].instanceCount++;
            }
            else
            {
                obj.instanceCount = 1;
                combinedProblems[key] = obj;
            }
        }

        problemObjects = combinedProblems.Values.ToList();
    }

    void SortProblemObjectsByInstances()
    {
        problemObjects = problemObjects.OrderByDescending(obj => obj.instanceCount).ToList();
    }

    [System.Serializable]
    public class ProblemObjects
    {
        [HideInInspector]
        public string name;
        [HideInInspector]
        public int depthCount;
        [ReadOnly]
        public GameObject gameObject;
        [HideInInspector]
        public string parent;
        [HideInInspector]
        public int instanceCount;
    }


    [System.Serializable]
    public class ScriptWithMethod
    {
        [HideInInspector]
        public string name;
        [ReadOnly]
        public MonoScript script;
        [HideInInspector]
        public int count;
        [HideInInspector]
        public int activeCount;
    }
}
