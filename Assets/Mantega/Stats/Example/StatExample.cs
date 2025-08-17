using UnityEngine;
using Mantega.Stats;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class StatExample : MonoBehaviour
{
    public StatType_ControlledInt _statInt;
    public StatTypeChange_ControlledInt _changeInt;
}

#if UNITY_EDITOR
[CustomEditor(typeof(StatExample))]
public class StatExampleEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        StatExample example = (StatExample)target;
        if (GUILayout.Button("Apply Change"))
        {
            if (example._statInt.TryApplyChange(example._changeInt))
            {
                Debug.Log("Change applied successfully.");
            }
            else
            {
                Debug.LogWarning("Failed to apply change.");
            }
        }
    }
}
#endif