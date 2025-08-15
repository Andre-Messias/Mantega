using UnityEngine;
using System;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "NewStatType", menuName = "Stat/StatType")]
public class StatType : ScriptableObject
{
    [Header("Type")]
    [SerializeField] private string _typeName = string.Empty;
    public string TypeName => _typeName;
    public Type Type { get; private set; }

    [Header("Value")]
    [SerializeReference] public object DefaultValue;

    #region Initialization
    protected virtual void OnEnable()
    {
        if (!TryUpdateType())
        {
            throw new ArgumentException($"{typeof(StatType)}: Type '{_typeName}' not found. Please check the type name", nameof(_typeName));
        }
    }
    #endregion

    #region Type Handling
    #region Set
    public bool TrySetType(string typeName, out Type newType)
    {
        newType = null;
        if (string.IsNullOrWhiteSpace(typeName)) return false;

        newType = FindType(typeName);
        if (newType == null) return false;

        if(Type != newType)
        {
            if (newType.IsValueType)
            {
                DefaultValue = Activator.CreateInstance(newType);
            }
            else
            {
                DefaultValue = null;
            }
            Type = newType;
            _typeName = typeName;
        }
        return true;
    }
    #endregion

    #region Get
    public Type SearchType()
    {
        if (Type == null)
        {
            if (!TryUpdateType())
            {
                throw new ArgumentException($"{typeof(StatType)}: Type '{_typeName}' not found. Please check the type name", nameof(_typeName));
            }
        }
        return Type;
    }
    #endregion
    public static Type FindType(string fullName)
    {
        if (string.IsNullOrEmpty(fullName)) return null;

        Type type = Type.GetType(fullName);
        if (type != null) return type;
        
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.FullName == fullName);
    }

    public bool TryUpdateType() => TrySetType(_typeName, out _);

    #endregion

    public bool TrySetDefaultValue(object value)
    {
        if (value == null)
        {
            DefaultValue = null;
            return true;
        }

        if (!Type.IsAssignableFrom(value.GetType()))
        {
            Debug.LogWarning($"StatType: Value type {value.GetType()} is not assignable to {Type}.");
            return false;
        }
        DefaultValue = value;
        return true;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(StatType))]
public class StatTypeEditor : Editor
{
    private StatType _statType;
    private string _typeNameInput;

    private Type _resolvedTypeCache;

    private void OnEnable()
    {
        _statType = (StatType)target;
        _typeNameInput = _statType.TypeName;
        _resolvedTypeCache = _statType.SearchType();
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("Stat Configuration", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        _typeNameInput = EditorGUILayout.DelayedTextField("Type Name", _typeNameInput);
        if (EditorGUI.EndChangeCheck())
        {
            // Undo / Redo support
            Undo.RecordObject(_statType, "Set Stat Type");

            if (_statType.TrySetType(_typeNameInput, out _resolvedTypeCache))
            {
                EditorUtility.SetDirty(_statType);
            }
        }

        if (string.IsNullOrWhiteSpace(_typeNameInput))
        {
            EditorGUILayout.HelpBox("Type name cannot be empty", MessageType.Warning);
        }
        else
        {
            if (_resolvedTypeCache == null)
            {
                EditorGUILayout.HelpBox($"Type \"{_typeNameInput}\" was not found", MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox($"Resolved Type: {_resolvedTypeCache.Name}", MessageType.Info);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Default Value", EditorStyles.boldLabel);

                var drawer = StatGUIDrawerManager.GetDrawerForType(_resolvedTypeCache);
                if (drawer != null)
                {
                    EditorGUI.BeginChangeCheck();
                    object newValue = drawer.Draw(_statType, _statType.DefaultValue);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(_statType, "Set Default Value");
                        _statType.TrySetDefaultValue(newValue);
                        EditorUtility.SetDirty(_statType);
                    }
                }
                else
                {
                    // Fallback
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("DefaultValue"), true);
                }

                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
#endif
