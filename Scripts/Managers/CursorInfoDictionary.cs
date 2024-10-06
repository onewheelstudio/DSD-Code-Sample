using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "Cursor Info Dictionary", menuName = "Hex/Create Info Dictionary")]
public class CursorInfoDictionary : SerializedScriptableObject
{
    public Dictionary<CursorType, CursorInfo> Cursors = new Dictionary<CursorType,CursorInfo>();
}
