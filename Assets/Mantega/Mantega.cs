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
    namespace Syncables
    {
        using Editor;

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
            /// <summary>
            /// Represents an interface for notifying subscribers about internal changes
            /// </summary>
            /// <remarks>This interface defines an event that is triggered whenever an internal change occurs.
            /// Implementers of this interface can use the <see cref="OnInternalChange"/> event to notify subscribers about
            /// state changes or other relevant updates</remarks>
            public interface IInternalChange
            {
                /// <summary>
                /// Occurs when an internal change is detected
                /// </summary>
                /// <remarks>This event is triggered whenever an internal state change occurs. 
                /// Subscribers can use this event to react to changes in the internal state</remarks>
                event Action<T> OnInternalChange;
            }

            // VALUE
            // [Header("Value")]

            /// <summary>
            /// The backing field that stores the current <typeparamref name="T"/> value of the object
            /// </summary>
            [SerializeField, CallOnChange(nameof(EditorSetValue))] private T _value;

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
            /// <remarks>This event is triggered whenever the value is updated or an internal change happen (in this case oldValue == newValue). The first parameter represents
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

            private void SetValue(T newValue)
            {
                if (_value is IInternalChange oldInternalChange)
                {
                    oldInternalChange.OnInternalChange -= HandleInternalChange;
                }

                _value = newValue;

                if (_value is IInternalChange newInternalChange)
                {
                    newInternalChange.OnInternalChange += HandleInternalChange;
                }
            }

            /// <summary>
            /// Handles internal changes to the value and triggers the <see cref="OnValueChanged"/> event
            /// </summary>
            /// <remarks>This method invokes the <see cref="OnValueChanged"/> event with the current value as
            /// both the old and new values. It is intended for internal use to propagate changes within the
            /// system</remarks>
            /// <param name="internalValue">The change value</param>
            private void HandleInternalChange(T internalValue)
            {
                OnValueChanged?.Invoke(_value, _value);
            }

            /// <summary>
            /// Implicitly converts a <see cref="Syncable{T}"/> instance to its underlying value of type <typeparamref
            /// name="T"/>.
            /// </summary>
            /// <param name="syncable">The <see cref="Syncable{T}"/> instance to convert. Must not be <c>null</c></param>
            public static implicit operator T([DisallowNull] Syncable<T> syncable) => syncable.Value;

            /// <summary>
            /// Sets the value of the object in the Unity Editor and triggers the <see cref="OnValueChanged"/>
            /// </summary>
            /// <remarks>This method is intended for use in the Unity Editor only. It updates the value of the
            /// object and invokes the <see cref="OnValueChanged"/> event to notify listeners of the change</remarks>
            /// <param name="oldValue">The previous value of the object</param>
            /// <param name="newValue">The new value to set for the object</param>
            private void EditorSetValue(T oldValue, T newValue)
            {
                SetValue(newValue);
                OnValueChanged?.Invoke(oldValue, newValue);
            }
        }
    }


    namespace Beta
    {
        using Syncables;
        using Editor;

        [Serializable]
        public class ControlledInt : Syncable<ControlledInt>.IInternalChange
        {
            [SerializeField, CallOnChange(nameof(OnEditorChange))] private int _value;
            public int Value
            {
                get => _value;
                set => SetValue(value);
            }

            [SerializeField, CallOnChange(nameof(OnEditorChange))] private int _max;
            public int Max
            {
                get => _max;
                set => SetMax(value);
            }

            [SerializeField, CallOnChange(nameof(OnEditorChange))] private int _min;
            public int Min
            {
                get => _min;
                set => SetMin(value);
            }

            public event Action<ControlledInt> OnInternalChange;

            public void SetValue(int value) => _value = Mathf.Clamp(value, _min, _max);
            public void SetMax(int max) => _max = Mathf.Max(_min, max);
            public void SetMin(int min) => _min = Mathf.Min(_max, min);

            private void OnEditorChange(int oldV, int newV)
            {
                Debug.Log($"ControlledInt changed: {oldV} -> {newV}");
                OnInternalChange?.Invoke(this);
            }
        }
    }

    namespace Editor
    {
        /// <summary>
        /// Specifies that a method should be invoked when the value of the decorated field changes
        /// </summary>
        /// <remarks>This attribute is applied to fields to indicate that a specific method should be called
        /// whenever the field's value changes.  The method specified by <paramref name="methodName"/> must exist in the
        /// same class as the decorated field</remarks>
        [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
        public class CallOnChangeAttribute : PropertyAttribute
        {
            /// <summary>
            /// Gets the name of the method associated with this instance
            /// </summary>
            [HideInInspector] public readonly string MethodName;

            /// <summary>
            /// Calls a method in the target object when the decorated field changes
            /// </summary>
            /// <param name="methodName">The name of the method to call</param>
            public CallOnChangeAttribute(string methodName)
            {
                MethodName = methodName;
            }
        }

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(CallOnChangeAttribute))]
        public class CallOnChangeDrawer : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                object targetObject = GetParentObject(property);

                // Get the current (old) value
                object oldValue = fieldInfo.GetValue(targetObject);

                // Start checking for changes
                EditorGUI.BeginChangeCheck();

                // Draw the property field as usual
                EditorGUI.PropertyField(position, property, label, true);

                // If a change was detected
                if (EditorGUI.EndChangeCheck())
                {
                    // Get the attribute
                    CallOnChangeAttribute callOnChangeAttribute = attribute as CallOnChangeAttribute;

                    property.serializedObject.ApplyModifiedProperties();
                    // Get the current (new) value
                    object newValue = fieldInfo.GetValue(targetObject);

                    // Get the target object the property belongs to
                    var targetType = targetObject.GetType();

                    // Find the method with the specified name
                    MethodInfo method = targetType.GetMethod(callOnChangeAttribute.MethodName,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    if (method != null)
                    {
                        // Call the method
                        method.Invoke(targetObject, new[] { oldValue, newValue });
                    }
                    else
                    {
                        Debug.LogWarning($"[CallOnChange] Method '{callOnChangeAttribute.MethodName}' not found in '{targetType.Name}'");
                    }
                }
            }

            // This makes the drawer work for properties with children (like Vectors)
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            // Walks SerializedProperty path to get the actual C# object that owns the field
            private static object GetParentObject(SerializedProperty property)
            {
                object obj = property.serializedObject.targetObject;
                string[] path = property.propertyPath.Split('.');

                for (int i = 0; i < path.Length - 1; i++)
                {
                    var type = obj.GetType();
                    FieldInfo field = type.GetField(path[i],
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    if (field == null) return null;
                    obj = field.GetValue(obj);
                }

                return obj;
            }
        }
#endif

    }
}
