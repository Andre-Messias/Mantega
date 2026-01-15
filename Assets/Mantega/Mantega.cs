using System;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Mantega
{
    /// <summary>
    /// Defines a mechanism for creating a strongly-typed copy of an object.
    /// </summary>
    /// <typeparam name="T">The type of the object to be cloned.</typeparam>
    public interface ITypedClonable<T>
    {
        /// <summary>
        /// Creates a new instance of the current object with the same values as this instance.
        /// </summary>
        /// <returns>A new instance of type <typeparamref name="T"/> that is a copy of the current instance.</returns>
        public T Clone();
    }

    namespace Reflection
    {
        /// <summary>
        /// Provides utility methods for working with reflection, such as discovering types that implement specific
        /// interfaces.
        /// </summary>
        /// <remarks>This class is designed to simplify common reflection tasks, such as finding types
        /// that implement a specific generic interface. It operates on the current application domain and handles
        /// scenarios where some assemblies may not be fully loadable due to reflection issues.</remarks>
        public static class ReflectionUtils
        {
            /// <summary>
            /// Finds all non-abstract classes in the current application domain that implement a specified generic
            /// interface type.
            /// </summary>
            /// <remarks>This method scans all loaded assemblies in the current application domain to
            /// find classes that implement the specified generic interface. Assemblies that cannot be fully loaded due
            /// to reflection issues are skipped.</remarks>
            /// <param name="genericInterfaceType">The generic interface type definition to search for. This must be an interface and a generic type
            /// definition.</param>
            /// <returns>A list of <see cref="Type"/> objects representing the classes that implement the specified generic
            /// interface type. The list will be empty if no matching classes are found.</returns>
            /// <exception cref="ArgumentException">Thrown if <paramref name="genericInterfaceType"/> is not an interface or is not a generic type
            /// definition.</exception>
            public static List<Type> FindAllClassesOfInterface(Type genericInterfaceType)
            {
                if (!genericInterfaceType.IsInterface || !genericInterfaceType.IsGenericTypeDefinition)
                {
                    throw new ArgumentException("O tipo fornecido deve ser uma definição de interface genérica.", nameof(genericInterfaceType));
                }

                // All loaded assemblies in the current application domain
                return AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly =>
                    {
                        // To avoid issues with assemblies that cannot be loaded, we catch the ReflectionTypeLoadException
                        try
                        {
                            return assembly.GetTypes();
                        }
                        catch (ReflectionTypeLoadException)
                        {
                            return Enumerable.Empty<Type>();
                        }
                    })
                    .Where(type =>
                        !type.IsAbstract &&                                       // Non abstract classes only
                        !type.IsInterface &&                                      // Non interfaces only
                        type.GetInterfaces().Any(i =>                             // Get all implemented interfaces and check if any of them...
                            i.IsGenericType &&                                    // Generic interface?
                            i.GetGenericTypeDefinition() == genericInterfaceType  // Is the generic type definition the same as the one we're looking for?
                        )
                    ).ToList();
            }

            /// <summary>
            /// Determines whether the specified <paramref name="sourceObject"/> can be converted to the type of 
            /// <paramref name="targetObject"/> and, if possible, provides the converted value.
            /// </summary>
            /// <remarks>This method attempts to convert the <paramref name="sourceObject"/> to the
            /// type of  <paramref name="targetObject"/> using the following rules: <list type="bullet"> <item>If
            /// <paramref name="sourceObject"/> is <see langword="null"/>, the method returns  <see langword="true"/> if
            /// the target type is nullable; otherwise, it returns <see langword="false"/>.</item> <item>If <paramref
            /// name="sourceObject"/> is already of the target type or a compatible type,  the method returns <see
            /// langword="true"/> and assigns <paramref name="sourceObject"/> to  <paramref name="converted"/>.</item>
            /// <item>If the conversion is possible using <see cref="Convert.ChangeType(object, Type)"/>, the method 
            /// returns <see langword="true"/> and assigns the converted value to <paramref name="converted"/>.</item>
            /// <item>If none of the above conditions are met, the method returns <see langword="false"/>.</item>
            /// </list></remarks>
            /// <param name="sourceObject">The object to be converted. Can be <see langword="null"/>.</param>
            /// <param name="targetObject">An object whose type is used as the target type for the conversion.  Cannot be <see langword="null"/>.</param>
            /// <param name="converted">When this method returns, contains the converted value if the conversion  was successful; otherwise,
            /// <see langword="null"/>.</param>
            /// <returns><see langword="true"/> if the <paramref name="sourceObject"/> can be converted to the type of  <paramref
            /// name="targetObject"/>; otherwise, <see langword="false"/>.</returns>
            public static bool CanConvert([NotNull] object sourceObject, [NotNull] object targetObject, out object converted)
            {
                converted = null;

                if (targetObject == null)
                {
                    return false;
                }

                Type targetType = targetObject.GetType();

                // Null source handling
                if (sourceObject == null)
                {
                    // Non-nullable value types cannot be assigned null
                    if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                    {
                        return false;
                    }

                    // converted is already null
                    return true;
                }

                Type sourceType = sourceObject.GetType();

                // Is already the correct type
                if (targetType.IsAssignableFrom(sourceType))
                {
                    converted = sourceObject;
                    return true;
                }

                // Convertible via IConvertible
                try
                {
                    converted = Convert.ChangeType(sourceObject, targetType);
                    return true;
                }
                catch (Exception)
                {
                    // Ignora as exceções (InvalidCastException, FormatException, OverflowException) 
                    // e tenta o próximo método.
                }

                // Unable to convert
                return false;
            }
        }
    }

    namespace Syncables
    {
        using Editor;

        /// <summary>
        /// Represents a read-only synchronization interface for a value of type <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>This interface provides access to a value that can be observed for changes. Consumers can
        /// subscribe to the <see cref="OnValueChanged"/> event to be notified when the value changes.</remarks>
        /// <typeparam name="T">The type of the value being synchronized.</typeparam>
        public interface IReadOnlySyncable<T>
        {
            /// <summary>
            /// Gets the value stored in the current instance.
            /// </summary>
            public T Value { get; }

            /// <summary>
            /// Occurs when the value changes, providing the previous and current values.
            /// </summary>
            /// <remarks>This event is triggered whenever the value is updated. The first parameter represents
            /// the previous value, and the second parameter represents the new value. Subscribers can use this event to
            /// react to changes in the value.</remarks>
            public event Action<T, T> OnValueChanged;
        }

        /// <summary>
        /// Represents an interface for notifying subscribers about internal changes.
        /// </summary>
        /// <remarks>This interface defines an event that is triggered whenever an internal change occurs.
        /// Implementers of this interface can use the <see cref="OnInternalChange"/> event to notify subscribers about
        /// state changes or other relevant updates.</remarks>
        public interface IInternalChange<T> : ITypedClonable<T>
        {
            /// <summary>
            /// Occurs when a change is detected, providing the old and new values of the changed item.
            /// </summary>
            /// <remarks>This event is triggered whenever an internal change occurs. The first parameter represents
            /// the previous value, and the second parameter represents the new value. Subscribers can use this event to react to changes in
            /// the state or data.</remarks>
            event Action<T, T> OnInternalChange;
        }

        /// <summary>
        /// Represents a synchronizable value of type <typeparamref name="T"/> that notifies subscribers when its value
        /// changes.
        /// </summary>
        /// <remarks>The <see cref="Syncable{T}"/> class provides a mechanism to track changes to a value and
        /// notify listeners via the <see cref="OnValueChanged"/> event. If <typeparamref name="T"/> have internal changes it must implement <see cref="IInternalChange{T}"/>.</remarks>
        /// <typeparam name="T">The type of the value being synchronized, if it has internal changes it must implement <see cref="IInternalChange{T}"/>.</typeparam>
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
            /// Occurs when the value changes, providing the previous and current <typeparamref name="T"/> values.
            /// </summary>
            /// <remarks>This event is triggered whenever the value is updated or an internal change happen (in this case oldValue == newValue). The first parameter represents
            /// the previous value, and the second parameter represents the new value. Subscribers can use this event to
            /// respond to changes in the value.</remarks>
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

    namespace Variables
    {
        using Syncables;

#if UNITY_EDITOR
        using Editor;
#endif

        #region ControlledInt

        /// <summary>
        /// Represents an integer value constrained within a specified range, with support for dynamic range adjustments
        /// and change notifications
        /// </summary>
        /// <remarks>The <see cref="ControlledInt"/> class provides a mechanism to manage an integer value
        /// that is constrained between a minimum and maximum value. The range can be dynamically adjusted, and the
        /// value is automatically clamped to remain within the specified bounds. The class also supports event-based
        /// notifications for internal changes, enabling observers to track modifications to the value, minimum, or
        /// maximum</remarks>
        [Serializable]
        public class ControlledInt : IInternalChange<ControlledInt>
        {
            #region Value
            /// <summary>
            /// Represents the serialized integer value
            /// </summary>
            /// <remarks>This field is serialized, making it visible and editable in the Unity
            /// Inspector. In the Unity Editor, changes to this value trigger the <see cref="OnEditorChangeValue(int, int)"/> method</remarks>
#if UNITY_EDITOR
            [CallOnChange(nameof(OnEditorChangeValue))]
#endif
            [SerializeField] private int _value;

            /// <summary>
            /// Gets or sets the integer value associated with this instance
            /// </summary>
            public int Value
            {
                get => _value;
                set => SetValue(value);
            }
            #endregion

            #region Max
            /// <summary>
            /// Represents the serialized maximum allowable value
            /// </summary>
            /// <remarks>This field is serialized, making it visible and editable in the Unity
            /// Inspector. In the Unity Editor, changes to this value trigger the <see cref="OnEditorChangeMax(int, int)"/> method</remarks>
#if UNITY_EDITOR
            [CallOnChange(nameof(OnEditorChangeMax))]
#endif
            [SerializeField] private int _max;

            /// <summary>
            /// Gets or sets the maximum allowable value
            /// </summary>
            public int Max
            {
                get => _max;
                set => SetMax(value);
            }
            #endregion

            #region Min
            /// <summary>
            /// Represents the serialized minimum allowable value
            /// </summary>
            /// <remarks>This field is serialized, making it visible and editable in the Unity
            /// Inspector. In the Unity Editor, changes to this value trigger the <see cref="OnEditorChangeMin(int, int)"/> method</remarks>
#if UNITY_EDITOR
            [CallOnChange(nameof(OnEditorChangeMin))]
#endif
            [SerializeField] private int _min;

            /// <summary>
            /// Gets or sets the minimum allowable value
            /// </summary>
            public int Min
            {
                get => _min;
                set => SetMin(value);
            }
            #endregion

            public event Action<ControlledInt, ControlledInt> OnInternalChange;

            #region Value Change Logic

            /// <summary>
            /// Sets the value, clamping it within the allowed range
            /// </summary>
            /// <param name="value">The value to set. It will be clamped to ensure it falls within the range defined by the <see cref="_min"/> and
            /// <see cref="_max"/> values</param>
            /// <returns>The clamped value that was set</returns>
            public int SetValue(int value) => _value = Mathf.Clamp(value, _min, _max);

            /// <summary>
            /// Sets the maximum value, ensuring it is not less than the current minimum value
            /// </summary>
            /// <remarks>If the specified <paramref name="max"/> is less than the current <see cref="_min"/>
            /// value, the <see cref="_min"/> value is used instead</remarks>
            /// <param name="max">The proposed maximum value to set</param>
            /// <returns>The resulting maximum value after applying the constraint</returns>
            public int SetMax(int max) => _max = Mathf.Max(_min, max);

            /// <summary>
            /// Sets the minimum value, ensuring it does not exceed the current maximum value
            /// </summary>
            /// <remarks>If the specified <paramref name="min"/> is greater than the current <see cref="_max"/>
            /// value, the <see cref="_max"/> value is used instead</remarks>
            /// <param name="min">The proposed minimum value</param>
            /// <returns>The updated minimum value, which will be the smaller of the proposed value and the current maximum</returns>
            public int SetMin(int min) => _min = Mathf.Min(_max, min);

            #endregion

            /// <summary>
            /// Returns a string representation of the current object, including the value and its defined range
            /// </summary>
            /// <returns>The exact details of the representation are unspecified and subject to change, but the
            /// following may be regarded as "ControlledInt: Value={value}, Min={min}, Max={max}"</returns>
            public override string ToString()
            {
                return $"{nameof(ControlledInt)}: Value={_value}, Min={_min}, Max={_max}";
            }

            /// <summary>
            /// Creates a new instance of <see cref="ControlledInt"/> with the same value, minimum, and maximum as the
            /// current instance
            /// </summary>
            /// <returns>A new <see cref="ControlledInt"/> instance that is a copy of the current instance</returns>
            public ControlledInt Clone()
            {
                return new ControlledInt
                {
                    _value = this._value,
                    _max = this._max,
                    _min = this._min
                };
            }

#if UNITY_EDITOR

            #region EditorChangeHandlers
            /// <summary>
            /// Handles changes to the value in the Unity Editor
            /// </summary>
            /// <remarks>This method is intended for use in the Unity Editor to update the value
            /// and trigger the <see cref="OnInternalChange"/> event. It creates a clone of the current object
            /// with the old value, updates the value, and then invokes the event with the cloned and
            /// updated objects</remarks>
            /// <param name="oldV">The previous value before the change</param>
            /// <param name="newV">The new value after the change</param>
            private void OnEditorChangeValue(int oldV, int newV)
            {
                ControlledInt clone = Clone();
                clone._value = oldV;

                newV = SetValue(newV);
                OnInternalChange?.Invoke(clone, this);
            }

            /// <summary>
            /// Handles changes to the maximum value in the Unity Editor
            /// </summary>
            /// <remarks>This method is intended for use in the Unity Editor to update the maximum
            /// value and trigger the <see cref="OnInternalChange"/> event. It creates a clone of the current object
            /// with the old maximum value, updates the maximum value, and then invokes the event with the cloned and
            /// updated objects</remarks>
            /// <param name="oldV">The previous maximum value</param>
            /// <param name="newV">The new maximum value to be set</param>
            private void OnEditorChangeMax(int oldV, int newV)
            {
                ControlledInt clone = Clone();
                clone._max = oldV;

                _max = SetMax(newV);
                OnInternalChange?.Invoke(clone, this);
            }

            /// <summary>
            /// Handles changes to the minimum value of the controlled integer
            /// </summary>
            /// <remarks>This method updates the internal minimum value and triggers the <see
            /// cref="OnInternalChange"/> event with a clone of the previous state and the current state. Callers
            /// should ensure that the new value is valid within the context of the controlled integer's
            /// constraints</remarks>
            /// <param name="oldV">The previous minimum value before the change</param>
            /// <param name="newV">The new minimum value to be set</param>
            private void OnEditorChangeMin(int oldV, int newV)
            {
                ControlledInt clone = Clone();
                clone._min = oldV;

                _min = SetMin(newV);
                OnInternalChange?.Invoke(clone, this);
            }
            #endregion

#endif
        }

        #endregion

    }

    namespace Beta
    {
        using Syncables;

#if UNITY_EDITOR
        using Editor;
#endif

    }

    namespace Editor
    {
        #region CallOnChange
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
        #endregion

        #region MantegaStyles
#if UNITY_EDITOR
        /// <summary>
        /// Provides custom GUI styles for use in Unity editor extensions
        /// </summary>
        /// <remarks>This class contains predefined styles that can be used to maintain consistent visual
        /// design in Unity editor tools. The styles are read-only and optimized for common use cases</remarks>
        public sealed class MantegaStyles
        {
            /// <summary>
            /// Represents a read-only GUI style for displaying JSON content in a help box format
            /// </summary>
            /// <remarks>This style is based on <see cref="EditorStyles.helpBox"/> and includes
            /// additional padding  and word wrapping to enhance readability of JSON content</remarks>
            readonly static GUIStyle _jsonStyle = new(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10),
                wordWrap = true // Line wrap
            };

            /// <summary>
            /// Gets the GUI style used for rendering JSON content in the user interface
            /// </summary>
            public static GUIStyle JsonStyle => _jsonStyle;
        }
#endif
        #endregion
    }
}
