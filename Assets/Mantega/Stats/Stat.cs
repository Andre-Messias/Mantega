using System;
using UnityEngine;
using static Mantega.Stats.StatType;
using Mantega.Editor;

#if UNITY_EDITOR
using System.Linq;
using System.Collections.Generic;
using Mantega.Reflection;
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "NewStat", menuName = "Stat/Stat")]
public class Stat : ScriptableObject//, ISerializationCallbackReceiver
{
    public string StatName = "New Stat";

    [SerializeReference] private object _data;
    public object Data 
    {
        get => _data;
        set => _data = value;
    }

    /*[SerializeField] private SerializationData _serializedData;

    public void OnBeforeSerialize()
    {
        _serializedData = Data.Serialize();
    }

    public void OnAfterDeserialize()
    {
        Data = _serializedData.Deserialize();
    }*/

    public void Log()
    {
        Debug.Log($"{_data?.GetType()}: {_data}");
    }
}

public class Float : IStatType<float, FloatChange>
{
    [SerializeField] private float _value;
    public float Value => _value;

    public void ApplyChange(FloatChange change)
    {
        switch (change._field.Type)
        {
            case StatTypeChange.ChangeType.None:
                break;
            case StatTypeChange.ChangeType.Set:
                _value = change._field.Value;
                break;
            case StatTypeChange.ChangeType.Change:
                _value += change._field.Value;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}

public class FloatChange : StatTypeChange
{
    public ChangeField<float> _field = new(ChangeType.None, 0);
}

#if UNITY_EDITOR
[CustomEditor(typeof(Stat))]
public class StatEditor : Editor
{
    private List<Type> _statTypes;
    private string[] _typeNames;
    private int _currentIndex = -1;
    private Type _currentType = null;

    private SerializedProperty _statNameProp;
    private SerializedProperty _dataProp;

    private void OnEnable()
    {
        _statTypes = ReflectionUtils.FindAllClassesOfInterface(typeof(IStatType<,>));
        _statTypes.Insert(0, null);
        _typeNames = _statTypes.Select(t => t == null ? "None" : t.Name).ToArray();

        _statNameProp = serializedObject.FindProperty("StatName");
        _dataProp = serializedObject.FindProperty("_data");

        Stat stat = (Stat)target;
        _currentType = stat.Data?.GetType();
        Debug.Log($"Current Type: {_currentType}");
        if (_currentType != null && !string.IsNullOrEmpty(_currentType.AssemblyQualifiedName))
        {
            _currentIndex = _statTypes.FindIndex(t => t != null && t.AssemblyQualifiedName == _currentType.AssemblyQualifiedName);
        }
        Debug.Log($"Current Index: {_currentIndex}");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_statNameProp);

        int newIndex = EditorGUILayout.Popup("Stat Type", _currentIndex, _typeNames);
        
        if (newIndex != _currentIndex)
        {
            _currentIndex = newIndex;
            _currentType = _statTypes[newIndex];
            if (_currentType == null)
            {
                _dataProp.managedReferenceValue = null;
            }
            else
            {
                _dataProp.managedReferenceValue = Activator.CreateInstance(_currentType);
            }
        }

        EditorGUILayout.Space();
        serializedObject.ApplyModifiedProperties();

        if (_dataProp.managedReferenceValue != null)
        {
            EditorGUILayout.PropertyField(_dataProp, true);
        }
        else
        {
            EditorGUILayout.HelpBox("Selecione um 'Stat Type' para configurar seus valores.", MessageType.Info);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Log Data"))
        {
            (target as Stat).Log();
        }

    }
}
#endif
