using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AllowOnlyOneInstance : MonoBehaviour
{
    private void OnEnable()
    {
        if (GameObject.FindObjectOfType<AllowOnlyOneInstance>() != this)
            Destroy(this.gameObject);
    }
}
