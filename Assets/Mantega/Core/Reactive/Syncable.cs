namespace Mantega.Core.Reactive
{
    using UnityEngine;
    using System;
    using System.Collections.Generic;

    using Mantega.Core.Diagnostics;

#if UNITY_EDITOR
    using Editor;
    using UnityEditor;
#endif

    /// <summary>
    /// Represents a synchronizable value of type <typeparamref name="T"/> that notifies subscribers when its value
    /// changes.
    /// </summary>
    /// <remarks>The <see cref="Syncable{T}"/> class provides a mechanism to track changes to a value and
    /// notify listeners via the <see cref="OnValueChanged"/> event. If <typeparamref name="T"/> have internal changes it must implement <see cref="IInternalChange{T}"/>.</remarks>
    /// <typeparam name="T">The type of the value being synchronized, if it has internal changes it must implement <see cref="IInternalChange{T}"/>.</typeparam>
    [Serializable]
    public class Syncable<T> : IReadOnlySyncable<T>, IValueContainer
    {
        #region Value

        /// <summary>
        /// The backing field that stores the current <typeparamref name="T"/> value of the object.
        /// </summary>
    #if UNITY_EDITOR
        [CallOnChange(nameof(EditorSetValue))]
    #endif
        [SerializeField] private T _value;

        /// <summary>
        /// The current value of the object.
        /// </summary>
        /// <remarks>When the value is updated, the <see cref="OnValueChanged"/> event is invoked with the
        /// old and new values, allowing subscribers to react to the change.</remarks>
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

        /// <summary>
        /// Retrieves the current value stored in the object.
        /// </summary>
        /// <returns>The <see cref="Value"/> contained in the object.</returns>
        public object GetValue() => Value;
        #endregion

        #region Events

        /// <summary>
        /// Occurs when the value changes, providing the previous and current <typeparamref name="T"/> values.
        /// </summary>
        /// <remarks>This event is triggered whenever the value is updated or an internal change happen (in this case oldValue == newValue). The first parameter represents
        /// the previous value, and the second parameter represents the new value. Subscribers can use this event to
        /// respond to changes in the value.</remarks>
        public event Action<T, T> OnValueChanged;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="Syncable{T}"/> class with the specified initial value
        /// </summary>
        /// <param name="initialValue">The initial value to set for the syncable object</param>
        public Syncable(T initialValue)
        {
            SetValue(initialValue);
        }

        /// <summary>
        /// Sets the value of the current instance to the specified value.
        /// </summary>
        /// <param name="newValue">The new value to assign to the instance.</param>

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
        /// Handles internal changes to the value and triggers the <see cref="OnValueChanged"/> event.
        /// </summary>
        /// <remarks>This method invokes the <see cref="OnValueChanged"/> event with the current value as
        /// both the old and new values. It is intended for internal use to propagate changes within the
        /// system.</remarks>
        /// <param name="internalValue">The change value.</param>
        private void HandleInternalChange(T oldValue, T newValue)
        {
            OnValueChanged?.Invoke(oldValue, newValue);
        }

        /// <summary>
        /// Implicitly converts a <see cref="Syncable{T}"/> instance to its underlying value of type <typeparamref
        /// name="T"/>.
        /// </summary>
        /// <param name="syncable">The <see cref="Syncable{T}"/> instance to convert. Must not be <c>null</c>.</param>
        public static implicit operator T(Syncable<T> syncable)
        {
            Validations.ValidateNotNull(syncable);
            return syncable.Value;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Sets the value of the object in the Unity Editor and triggers the <see cref="OnValueChanged"/>.
        /// </summary>
        /// <remarks>This method is intended for use in the Unity Editor only. It updates the value of the
        /// object and invokes the <see cref="OnValueChanged"/> event to notify listeners of the change.</remarks>
        /// <param name="oldValue">The previous value of the object.</param>
        /// <param name="newValue">The new value to set for the object.</param>
        private void EditorSetValue(T oldValue, T newValue)
        {
            SetValue(newValue);
            OnValueChanged?.Invoke(oldValue, newValue);
        }

#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// Provides a custom property drawer for Syncable<T> objects.
    /// </summary>
    /// <remarks>This class customizes how Syncable<T> properties appear in the Unity Inspector by rendering
    /// only the underlying value field, rather than the entire object.</remarks>
    [CustomPropertyDrawer(typeof(Syncable<>))]
    public class SyncableDrawer : PropertyDrawer
    {
        /// <summary>
        /// Draws the property field for the underlying value of a Syncable<T> object in the Unity Editor.
        /// </summary>
        /// <remarks>This method customizes how Syncable<T> properties are displayed in the Unity
        /// Inspector by rendering only the underlying value field. Use this override when creating custom property
        /// drawers for types derived from Syncable<T>.</remarks>
        /// <param name="position">The rectangle on the screen to use for the property field.</param>
        /// <param name="property">The serialized property representing the Syncable<T> object to be drawn.</param>
        /// <param name="label">The label to display for the property field.</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Get and draw the "_value" property of the Syncable<T> class
            SerializedProperty valueProp = property.FindPropertyRelative("_value");
            EditorGUI.PropertyField(position, valueProp, label, true);
        }

        /// <summary>
        /// Calculates the vertical space required to display the specified property in the Inspector, including its
        /// child fields if expanded.
        /// </summary>
        /// <param name="property">The serialized property for which to determine the display height. Cannot be <see langword="null"/>.</param>
        /// <param name="label">The label to use for the property field. This determines how the property is displayed in the Inspector.</param>
        /// <returns>The height, in pixels, needed to render the property and its children in the Inspector.</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty valueProp = property.FindPropertyRelative("_value");
            return EditorGUI.GetPropertyHeight(valueProp, true);
        }
    }
#endif
}