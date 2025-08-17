using System;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics.CodeAnalysis;

#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
#endif

namespace Mantega
{
    #region Syncable
    
    /// <summary>
    /// Represents a read-only synchronization interface for a value of type <typeparamref name="T"/>
    /// </summary>
    /// <remarks>This interface provides access to a value that can be observed for changes. Consumers can
    /// subscribe to the <see cref="OnValueChanged"/> event to be notified when the value changes</remarks>
    /// <typeparam name="T">The type of the value being synchronized</typeparam>
    public interface IReadOnlySyncable<T>
    {
        /// <summary>
        /// Gets the value stored in the current instance
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Occurs when the value changes, providing the previous and current values
        /// </summary>
        /// <remarks>This event is triggered whenever the value is updated. The first parameter represents
        /// the previous value,  and the second parameter represents the new value. Subscribers can use this event to
        /// react to changes  in the value</remarks>
        public event Action<T, T> OnValueChanged;
    }

    /// <summary>
    /// Represents a synchronizable value of type <typeparamref name="T"/> that notifies subscribers when its value
    /// changes
    /// </summary>
    /// <remarks>The <see cref="Syncable{T}"/> class provides a mechanism to track changes to a value and
    /// notify listeners via the <see cref="OnValueChanged"/> event</remarks>
    /// <typeparam name="T">The type of the value being synchronized</typeparam>
    [Serializable]
    public class Syncable<T> : IReadOnlySyncable<T>
    {
        // VALUE
        // [Header("Value")]

        /// <summary>
        /// The backing field that stores the current <typeparamref name="T"/> value of the object
        /// </summary>
        [SerializeField] private T _value;

        /// <summary>
        /// The current value of the object
        /// </summary>
        /// <remarks>When the value is updated, the <see cref="OnValueChanged"/> event is invoked with the
        /// old and new values, allowing subscribers to react to the change</remarks>
        public T Value
        {
            get => _value;
            set
            {
                // No change
                if (EqualityComparer<T>.Default.Equals(_value, value)) return;

                T oldValue = _value;
                SetValue(value);

                OnValueChanged?.Invoke(oldValue, _value);
            }
        }

        // EVENTS
        // [Header("Events")]

        /// <summary>
        /// Occurs when the value changes, providing the previous and current <typeparamref name="T"/> values
        /// </summary>
        /// <remarks>This event is triggered whenever the value is updated. The first parameter represents
        /// the previous value,  and the second parameter represents the new value. Subscribers can use this event to
        /// respond to changes  in the value</remarks>
        public event Action<T, T> OnValueChanged;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Syncable{T}"/> class with the specified initial value
        /// </summary>
        /// <param name="initialValue">The initial value to set for the syncable object</param>
        public Syncable(T initialValue)
        {
            _value = initialValue;
        }

        /// <summary>
        /// Sets the value of the current instance to the specified value
        /// </summary>
        /// <param name="newValue">The new value to assign to the instance</param>
        private void SetValue(T newValue) => _value = newValue;

        /// <summary>
        /// Implicitly converts a <see cref="Syncable{T}"/> instance to its underlying value of type <typeparamref
        /// name="T"/>.
        /// </summary>
        /// <param name="syncable">The <see cref="Syncable{T}"/> instance to convert. Must not be <c>null</c></param>
        public static implicit operator T([DisallowNull] Syncable<T> syncable) => syncable.Value;

#if UNITY_EDITOR
        /// <summary>
        /// Sets the value of the object in the Unity Editor and triggers the <see cref="OnValueChanged"/>
        /// </summary>
        /// <remarks>This method is intended for use in the Unity Editor only. It updates the value of the
        /// object and invokes the <see cref="OnValueChanged"/> event to notify listeners of the change</remarks>
        /// <param name="oldValue">The previous value of the object</param>
        /// <param name="newValue">The new value to set for the object</param>
        public void EditorSetValue(T oldValue, T newValue)
        {
            SetValue(newValue);
            OnValueChanged?.Invoke(oldValue, newValue);
        }
#endif
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(Syncable<>), true)]
    public class SyncableDrawer : PropertyDrawer
    {
        // Draws the property in the Inspector
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Get the internal "_value" property
            SerializedProperty valueProperty = property.FindPropertyRelative("_value");

            // Get the actual C# object instance this drawer is rendering
            object targetObject = GetTargetObject(property);

            // Get the current (old) value
            object oldValue = targetObject.GetType().GetProperty("Value").GetValue(targetObject);

            EditorGUI.BeginChangeCheck();

            // _value Field
            EditorGUI.PropertyField(position, valueProperty, label, true);

            if (EditorGUI.EndChangeCheck())
            {
                // Apply changes
                property.serializedObject.ApplyModifiedProperties();

                // Get the current (new) value
                object newValue = targetObject.GetType().GetProperty("Value").GetValue(targetObject);

                // Find the "EditorSetValue" method on our target object
                MethodInfo editorSetValueMethod = targetObject.GetType().GetMethod("EditorSetValue", BindingFlags.Public | BindingFlags.Instance);

                // Invoke the method with the old and new values
                editorSetValueMethod?.Invoke(targetObject, new[] { oldValue, newValue });
            }

            EditorGUI.EndProperty();
        }

        // Retrieves the target object from the SerializedProperty
        private object GetTargetObject(SerializedProperty property)
        {
            object target = property.serializedObject.targetObject;
            return fieldInfo.GetValue(target);
        }
    }
#endif

    #endregion


    namespace Beta
    {

        [Serializable]
        public class ControlledInt
        {
            [SerializeField] private int _value;
            public int Value
            {
                get => _value;
                set => Set(value);
            }
            public int Max;
            public int Min;

            public void Set(int newValue)
            {
                _value = Mathf.Clamp(newValue, Min, Max);
            }
        }
    }

}
