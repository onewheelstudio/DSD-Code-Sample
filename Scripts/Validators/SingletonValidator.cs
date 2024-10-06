#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor.Validation;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using HexGame.Units;

[assembly: RegisterValidationRule(typeof(SingletonValidator), Name = "SingletonValidator", Description = "Some description text.")]

public class SingletonValidator : RootObjectValidator<MonoBehaviour>
{
    // Introduce serialized fields here to make your validator
    // configurable from the validator window under rules.
    //public List<System.Type> singletonTypes;

    protected override void Validate(ValidationResult result)
    {
        //if (singletonTypes == null || singletonTypes.Count == 0)
        //    return;

        //foreach (System.Type item in singletonTypes)
        //{
        //    if (GameObject.FindObjectsOfType(item.GetType()).Length > 1)
        //    {
        //        result.AddWarning($"More than one type of {item.GetType().Name}");

        //    }
        //}
    }
}
#endif
