using UnityEngine;
using Unity.VisualScripting;
using System;
using System.Reflection;
using System.Linq;
using static Mantega.Stats.StatType;


#if UNITY_EDITOR
using System.Collections.Generic;
using Mantega.Core.Reflection;
using Mantega.Core.Editor;
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "NewStat", menuName = "Stat/Stat")]
public class Stat : ScriptableObject
{
    public string StatName = "New Stat";

    [SerializeReference] private object _data;

    public object Data => _data;

    public void Log()
    {
        Debug.Log($"{_data?.GetType()}: {_data}");
        if (_data == null) return;
    }

    public object CloneValue()
    {
        if (_data == null) return null;

        var value = GetDataValue();
        if (value == null) return null;

        var json = value.Serialize().json;
        return JsonUtility.FromJson(json, value.GetType());
    }

    public object CloneData()
    {
        if (_data == null) return null;
        Debug.Log($"{_data.Serialize().json}");
        return _data.CloneViaFakeSerialization();
    }

    public object GetValue()
    {
        return GetDataValue();
    }


    private object GetDataValue()
    {
        if (_data == null)
        {
            throw new InvalidOperationException("Data object is null.");
        }

        Type dataType = _data.GetType();
        var statInterface = dataType.GetInterfaces()
                                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStatType<,>));

        if (statInterface != null)
        {
            PropertyInfo propInfo = dataType.GetProperty("Value");
            if (propInfo != null)
            {
                return propInfo.GetValue(_data, null);
            }
        }
        else
        {
            throw new InvalidOperationException($"Data does not implement {typeof(IStatType<,>)} or the 'Value' property is missing.");
        }

        return null;
    }
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
        if (_currentType != null && !string.IsNullOrEmpty(_currentType.AssemblyQualifiedName))
        {
            _currentIndex = _statTypes.FindIndex(t => t != null && t.AssemblyQualifiedName == _currentType.AssemblyQualifiedName);
        }
        else
        {
            _currentIndex = 0;
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_statNameProp);

        // Dropdown for Stat Type
        int newIndex = EditorGUILayout.Popup("Stat Type", _currentIndex, _typeNames);

        // Change Stat Type
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
                _dataProp.serializedObject.ApplyModifiedProperties();
            }
        }
        EditorGUILayout.Space(5);

        // Display JSON representation of Data
        string json = _dataProp.managedReferenceValue != null ? _dataProp.managedReferenceValue.Serialize().json : "No Data";
        float height = MantegaStyles.JsonStyle.CalcHeight(new GUIContent(json), EditorGUIUtility.currentViewWidth);
        EditorGUILayout.SelectableLabel(json, MantegaStyles.JsonStyle, GUILayout.Height(height));

        EditorGUILayout.Space(20);

        // Data Field
        if (_dataProp.managedReferenceValue != null)
        {
            EditorGUILayout.PropertyField(_dataProp, true);
        }
        else
        {
            EditorGUILayout.HelpBox("Selecione a 'Stat Type'", MessageType.Info);
        }
        serializedObject.ApplyModifiedProperties();
        EditorGUILayout.Space();

        if (GUILayout.Button("Log Data"))
        {
            (target as Stat).Log();
        }

    }
}
#endif
