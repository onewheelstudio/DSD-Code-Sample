//-----------------------------------------------------------------------
// <copyright file="RootObjectValidator.cs" company="Sirenix ApS">
// Copyright (c) Sirenix ApS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

//[assembly: Sirenix.OdinInspector.Editor.Validation.RegisterValidator(typeof(Sirenix.OdinInspector.Editor.Validation.IRootValidatorValidator<>))]

namespace Sirenix.OdinInspector.Editor.Validation
{
    //public class IRootValidatorValidator<T> : RootObjectValidator<T>
    //    where T : UnityEngine.Object, IRootValidator
    //{
    //    protected override void Validate(ValidationResult result)
    //    {
    //        var container = new ValidationResultContainer();
    //        this.Object.Validate(container);

    //        foreach (var item in container.Results)
    //        {
    //            var type = item.Type == ValidationResultContainer.ValidationResultItemType.Error ? ValidationResultType.Error : ValidationResultType.Warning;
    //            result.Add(new ResultItem(item.Message, type));
    //        }
    //    }
    //}
}
