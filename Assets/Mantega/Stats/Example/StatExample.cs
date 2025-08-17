using UnityEngine;
using Mantega.Stats;
using Mantega;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class StatExample : MonoBehaviour
{
    [Header("Stat Example")]
    [SerializeField] private StatType.ControlledInt _statInt;
    [SerializeField] private StatType.ControlledIntChange _changeInt;

    [Header("Syncable Example")]
    [SerializeField] private Syncable<int> _syncableInt = new(0);
    public IReadOnlySyncable<int> SyncableInt => _syncableInt;

    public void Test()
    {
        Debug.Log("Stat Example Test");
    }

    private void OnEnable()
    {
        _syncableInt.OnValueChanged += DebugSyncableIntValue;
    }

    private void OnDisable()
    {
        _syncableInt.OnValueChanged -= DebugSyncableIntValue;
    }

    private void DebugSyncableIntValue(int oldValue, int newValue) => Debug.Log($"Syncable Int Changed: {oldValue} -> {newValue}");

#if UNITY_EDITOR
    public void ApplyChange() => _statInt.ApplyChange(_changeInt);

    public void ApplySyncableChange()
    {
        _syncableInt.Value += 1;
        Debug.Log($"Syncable Int Changed: {_syncableInt.Value}");
    }

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

        if (GUILayout.Button("Apply Syncable Change"))
        {
            example.ApplySyncableChange();
        }

        if (GUILayout.Button("Test Action"))
        {
            example.Test();
        }
    }
}
#endif