using UnityEngine;

using Mantega.Syncables;
using Mantega.Stats;
using Mantega.Beta;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class StatExample : MonoBehaviour
{
    [SerializeField] private StatType.Primitive _statPrimitive;

    [Header("Stat Example")]
    [SerializeField] private StatType.ControlledInt _statInt;
    [SerializeField] private StatType.ControlledIntChange _changeInt;

    [Header("Syncable Example")]
    [SerializeField] private Syncable<ControlledInt> _syncableControlledInt = new(new());
    public IReadOnlySyncable<ControlledInt> SyncableInt => _syncableControlledInt;

    [SerializeField] private Syncable<int> _syncableInt = new(0);
    public IReadOnlySyncable<int> SyncableSimpleInt => _syncableInt;

    public void Test()
    {
        Debug.Log(typeof(StatType.Wrapper<int>).FullName);
    }

    private void OnEnable()
    {
        _syncableControlledInt.OnValueChanged += DebugSyncableControlledIntValue;
        _syncableInt.OnValueChanged += DebugSyncableSimpleIntValue;
    }

    private void OnDisable()
    {
        _syncableControlledInt.OnValueChanged -= DebugSyncableControlledIntValue;
        _syncableInt.OnValueChanged -= DebugSyncableSimpleIntValue;
    }

    private void DebugSyncableControlledIntValue(ControlledInt oldValue, ControlledInt newValue) => Debug.Log($"Syncable Int Changed: {oldValue} -> {newValue}");
    private void DebugSyncableSimpleIntValue(int oldValue, int newValue) => Debug.Log($"Syncable Simple Int Changed: {oldValue} -> {newValue}");

#if UNITY_EDITOR
    public void ApplyChange() => _statInt.ApplyChange(_changeInt);

    public void ApplySyncableControlledIntChange()
    {
        _syncableControlledInt.Value.Value += 1;
        Debug.Log($"Syncable Int Changed: {_syncableControlledInt.Value}");
    }

    public void ApplySyncableSimpleIntChange()
    {
        _syncableInt.Value += 1;
        Debug.Log($"Syncable Simple Int Changed: {_syncableInt.Value}");
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
            example.ApplySyncableControlledIntChange();
        }

        if (GUILayout.Button("Test Action"))
        {
            example.Test();
        }
    }
}
#endif