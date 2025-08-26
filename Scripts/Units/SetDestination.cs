using Pathfinding;
using Sirenix.OdinInspector;
using UnityEngine;

public class SetDestination : MonoBehaviour
{
	[SerializeField]private Transform target;
	private bool targetSet = false;
	IAstarAI ai;
	public bool HasPath => ai.hasPath;

	void OnEnable()
	{
		targetSet = false;//reset value to wait until next target given

		ai = GetComponent<IAstarAI>();
		// Update the destination right before searching for a path as well.
		// This is enough in theory, but this script will also update the destination every
		// frame as the destination is used for debugging and may be used for other things by other
		// scripts as well. So it makes sense that it is up to date every frame.
		if (ai != null) ai.onSearchPath += Update;
	}

	void OnDisable()
	{
		if (ai != null) ai.onSearchPath -= Update;
	}

	// <summary>Updates the AI's destination every frame</summary>
	void Update()
	{
		if (targetSet && ai != null && ai.destination != target.position)
		{
			ai.destination = target.position;
			ai.SearchPath();
		}


		//else
		//	Debug.Log("target location is zero");
	}

	public void SetTarget(Transform target)
    {
		targetSet = true;
		this.target = target;
    }

	[Button]
	public void SetTargetLocation(Vector3 location)
    {
		targetSet = true;
		//this.target = location;

    }

	public void SetPath(Path path, Transform target)
	{
		this.target = target;
		targetSet = true;
        ai.SetPath(path);
    }
}
