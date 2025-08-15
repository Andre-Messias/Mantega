using UnityEngine;
using System;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Represents a type of statistic
/// </summary>
/// <remarks>This class is designed to define and manage a specific type of statistic, including its name,
/// associated .NET type, and default value. It provides functionality to resolve and update the associated type
/// dynamically at runtime</remarks>
[CreateAssetMenu(fileName = "NewStatType", menuName = "Stat/StatType")]
public class StatType : ScriptableObject
{
    #region Type
    // [Header("Type")]

    /// <summary>
    /// The backing field for the <see cref="Type.FullName"/> of the associated <see cref="System.Type"/>
    /// </summary>
    [SerializeField, Tooltip("The full name of the desigred type")] private string _typeName = string.Empty;

    /// <summary>
    /// Accessor for the <see cref="_typeName"/> field
    /// </summary>
    public string TypeName => _typeName;

    /// <summary>
    /// Gets the <see cref="System.Type"/> of the object represented by this instance
    /// </summary>
    public Type Type { get; private set; }

    #endregion

    #region Value
    // [Header("Value")]

    /// <summary>
    /// The default value for this statistic. Its actual <see cref="System.Type"/> should match the resolved <see cref="Type"/>.
    /// <remarks>Uses <see cref="SerializeReference"/> to support polymorphism for custom classes</remarks>
    /// </summary>
    [SerializeReference, Tooltip("Stat type default value")] public object DefaultValue;

    #endregion

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
    /// <summary>
    /// Attempts to set the <see cref="Type"/> of the current instance based on the specified <param name="typeName">
    /// </summary>
    /// <remarks>This method updates the type only if the specified <param name="typeName"> is valid and resolves to a
    /// different <see cref="Type"/> than the current one. If the new <see cref="System.Type"/> is a <see cref="System.ValueType"/>, the <see cref="DefaultValue"/> is initialized to an
    /// instance of that type; otherwise, it is set to <see langword="null"/></remarks>
    /// <param name="typeName">The fully qualified name of the type to set</param>
    /// <param name="newType">When this method returns, contains the <see cref="Type"/> object corresponding to the specified  <paramref
    /// name="typeName"/>, if the operation was successful; otherwise, <see langword="null"/></param>
    /// <returns><see langword="true"/> if the <see cref="Type"/> was successfully set; otherwise, <see langword="false"/></returns>
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

    /// <summary>
    /// Resolves and returns the <see cref="Type"/> associated with the current instance
    /// </summary>
    /// <remarks>If the <see cref="Type"/> is not already set, this method attempts to resolve it.  If the
    /// resolution fails, an exception is thrown</remarks>
    /// <returns>The resolved <see cref="Type"/> associated with the current instance</returns>
    /// <exception cref="ArgumentException">Thrown if the type cannot be resolved. Ensure that the <see cref="_typeName"/> is valid and correctly specified</exception>
    public Type ResolveType()
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

    /// <summary>
    /// Finds a <see cref="System.Type"/> object based on its fully qualified name
    /// </summary>
    /// <remarks>This method first attempts to locate the <see cref="System.Type"/> using <see cref="Type.GetType(string)"/>.  If
    /// the type is not found, it searches all loaded assemblies in the current application domain (very costy)</remarks>
    /// <param name="typeName">The fully qualified name of the <see cref="System.Type"/> to locate. This includes the <see cref="Type.Namespace"/> and <see cref="Type.FullName"/></param>
    /// <returns>The <see cref="Type"/> object that matches the specified fully qualified name, or <see langword="null"/> if no
    /// matching type is found</returns>
    public static Type FindType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName)) return null;

        Type type = Type.GetType(typeName);
        if (type != null) return type;
        
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.FullName == typeName);
    }

    /// <summary>
    /// Attempts to update the <see cref="Type"/> of the current object based on the <see cref="_typeName"/>
    /// </summary>
    /// <remarks>This method uses the internal <see cref="_typeName"/> to attempt the update. The operation will fail if the
    /// <see cref="_typeName"/> is invalid or if the <see cref="System.Type"/> cannot be resolved</remarks>
    /// <returns><see langword="true"/> if the <see cref="Type"/> was successfully updated; otherwise, <see langword="false"/></returns>
    public bool TryUpdateType() => TrySetType(_typeName, out _);

    #endregion

    /// <summary>
    /// Attempts to set the <see cref="DefaultValue"/> for the current instance
    /// </summary>
    /// <param name="value">The value to set as the default. Can be <see langword="null"/></param>
    /// <exception cref="ArgumentException">Throw if the value being set is invalid to the current <see cref="Type"/>. Ensure that only valid values are set</exception>
    public void SetDefaultValue(object value)
    {
        if(value == null)
        {
            DefaultValue = null;
            return;
        }

        if (!Type.IsAssignableFrom(value.GetType()))
        {
            throw new ArgumentException($"{nameof(value)} must be of type {Type.FullName}, but was {value.GetType().FullName}", nameof(value));
        }
        DefaultValue = value;
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
        _resolvedTypeCache = _statType.ResolveType();
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("Stat Configuration", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        _typeNameInput = EditorGUILayout.DelayedTextField("Type Name", _typeNameInput);
        // Try to resolve the type
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
                // Display resolved type information
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
                        _statType.SetDefaultValue(newValue);
                        EditorUtility.SetDirty(_statType);
                    }
                }
                else
                {
                    // Fallback
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("DefaultValue"), true);
                }

                // Apply changes to the serialized object
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
#endif
