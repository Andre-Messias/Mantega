using UnityEngine;
using Mantega.Stats;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class StatExample : MonoBehaviour
{
    [SerializeField] private StatType.ControlledInt _statInt;
    [SerializeField] private StatType.ControlledIntChange _changeInt;

    public void Test()
    {
        Debug.Log("Stat Example Test");
    }

#if UNITY_EDITOR
    public void ApplyChange() => _statInt.ApplyChange(_changeInt);
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(StatExample))]
public class StatExampleEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

        StatExample example = (StatExample)target;
        if (GUILayout.Button("Apply Change"))
        {
            example.ApplyChange();
        }

        if (GUILayout.Button("Test Action"))
        {
            example.Test();
        }
    }
}
#endif