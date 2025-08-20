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
    /// <summary>
    /// Defines a mechanism for creating a strongly-typed copy of an object
    /// </summary>
    /// <typeparam name="T">The type of the object to be cloned</typeparam>
    public interface ITypedClonable<T>
    {
        /// <summary>
        /// Creates a new instance of the current object with the same values as this instance
        /// </summary>
        /// <returns>A new instance of type <typeparamref name="T"/> that is a copy of the current instance</returns>
        public T Clone();
    }

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
            /// the previous value, and the second parameter represents the new value. Subscribers can use this event to
            /// react to changes in the value</remarks>
            public event Action<T, T> OnValueChanged;
        }

        /// <summary>
        /// Represents an interface for notifying subscribers about internal changes
        /// </summary>
        /// <remarks>This interface defines an event that is triggered whenever an internal change occurs.
        /// Implementers of this interface can use the <see cref="OnInternalChange"/> event to notify subscribers about
        /// state changes or other relevant updates</remarks>
        public interface IInternalChange<T> : ITypedClonable<T>
        {
            /// <summary>
            /// Occurs when a change is detected, providing the old and new values of the changed item
            /// </summary>
            /// <remarks>This event is triggered whenever an internal change occurs. The first parameter represents
            /// the previous value, and the second parameter represents the new value. Subscribers can use this event to react to changes in
            /// the state or data</remarks>
            event Action<T, T> OnInternalChange;
        }

        /// <summary>
        /// Represents a synchronizable value of type <typeparamref name="T"/> that notifies subscribers when its value
        /// changes
        /// </summary>
        /// <remarks>The <see cref="Syncable{T}"/> class provides a mechanism to track changes to a value and
        /// notify listeners via the <see cref="OnValueChanged"/> event. If <typeparamref name="T"/> have internal changes it must implement <see cref="IInternalChange{T}"/></remarks>
        /// <typeparam name="T">The type of the value being synchronized, if it has internal changes it must implement <see cref="IInternalChange{T}"/></typeparam>
        [Serializable]
        public class Syncable<T> : IReadOnlySyncable<T>
        {
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
            /// the previous value, and the second parameter represents the new value. Subscribers can use this event to
            /// respond to changes in the value</remarks>
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
                if (_value is IInternalChange<T> oldInternalChange)
                {
                    oldInternalChange.OnInternalChange -= HandleInternalChange;
                }

                _value = newValue;

                if (_value is IInternalChange<T> newInternalChange)
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
            private void HandleInternalChange(T oldValue, T newValue)
            {
                OnValueChanged?.Invoke(oldValue, newValue);
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
#if UNITY_EDITOR
        using Editor;
#endif

        [Serializable]
        public class ControlledInt : IInternalChange<ControlledInt>
        {
#if UNITY_EDITOR
            [CallOnChange(nameof(OnEditorChangeValue))]
#endif
            [SerializeField] private int _value;
            public int Value
            {
                get => _value;
                set => SetValue(value);
            }

#if UNITY_EDITOR
            [CallOnChange(nameof(OnEditorChangeMax))]
#endif
            [SerializeField] private int _max;
            public int Max
            {
                get => _max;
                set => SetMax(value);
            }

#if UNITY_EDITOR
            [CallOnChange(nameof(OnEditorChangeMin))]
#endif
            [SerializeField] private int _min;
            public int Min
            {
                get => _min;
                set => SetMin(value);
            }

            public event Action<ControlledInt, ControlledInt> OnInternalChange;

            #region Value Change Logic

            public int SetValue(int value) => _value = Mathf.Clamp(value, _min, _max);
            public int SetMax(int max) => _max = Mathf.Max(_min, max);
            public int SetMin(int min) => _min = Mathf.Min(_max, min);

            #endregion

            #region String
            public override string ToString()
            {
                return $"{nameof(ControlledInt)}: Value={_value}, Min={_min}, Max={_max}";
            }
            #endregion

            #region Cloneable Implementation
            public ControlledInt Clone()
            {
                return new ControlledInt
                {
                    _value = this._value,
                    _max = this._max,
                    _min = this._min
                };
            }

            #endregion

#if UNITY_EDITOR
            private void OnEditorChangeMax(int oldV, int newV)
            {
                ControlledInt clone = Clone();
                clone._max = oldV;

                _max = SetMax(newV);
                OnInternalChange?.Invoke(clone, this);
            }

            private void OnEditorChangeMin(int oldV, int newV)
            {
                ControlledInt clone = Clone();
                clone._min = oldV;

                _min = SetMin(newV);
                OnInternalChange?.Invoke(clone, this);
            }

            private void OnEditorChangeValue(int oldV, int newV)
            {
                ControlledInt clone = Clone();
                clone._value = oldV;

                newV = SetValue(newV);
                OnInternalChange?.Invoke(clone, this);
            }
#endif
        }
    }

    namespace Editor
    {
        /// <summary>
        /// Specifies that a method should be invoked when the value of the decorated field changes
        /// </summary>
        /// <remarks>This attribute is applied to fields to indicate that a specific method should be called
        /// whenever the field's value changes. The method specified by <paramref name="methodName"/> must exist in the
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
        /// <summary>
        /// Drawer for the <see cref="CallOnChangeAttribute"/> that invokes a method when the property changes
        /// </summary>
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

            /// <summary>
            /// Calculates the height, in pixels, required to render the specified property in the Unity Editor
            /// </summary>
            /// <param name="property">The <see cref="SerializedProperty"/> to calculate the height for</param>
            /// <param name="label">The label to display alongside the property in the Unity Editor</param>
            /// <returns>The height, in pixels, required to render the property, including any child properties if applicable</returns>
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            /// <summary>
            /// Retrieves the parent object of the specified serialized property
            /// </summary>
            /// <remarks>This method traverses the property path of the given <see
            /// cref="SerializedProperty"/> to locate its parent object. It uses reflection to access fields, including
            /// private and public fields, along the property path</remarks>
            /// <param name="property">The <see cref="SerializedProperty"/> for which the parent object is to be retrieved</param>
            /// <returns>The parent object of the specified serialized property, or <see langword="null"/> if the parent object
            /// cannot be determined</returns>
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
