using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Mantega.Stats
{
    /// <summary>
    /// Provides a collection of types and interfaces for representing and managing statistical data and changes to it
    /// </summary>
    /// <remarks>The <see cref="StatType"/> class contains definitions for statistical types, changes that can
    /// be applied to them,  and related utilities. It includes the <see cref="IStatType{T, U}"/> interface for defining
    /// statistical types  with a value and change application logic, and the <see cref="StatTypeChange"/> class for
    /// representing changes  with associated metadata</remarks>
    public static partial class StatType
    {
        /// <summary>
        /// Represents a statistical type with a value and the ability to apply changes to it
        /// </summary>
        /// <typeparam name="T">The type of the value represented by the statistical type</typeparam>
        /// <typeparam name="U">The type of the change that can be applied, constrained to <see cref="StatTypeChange"/></typeparam>
        public interface IStatType<T, U> where U : StatTypeChange
        {
            /// <summary>
            /// Gets the current value of the object
            /// </summary>
            public abstract T Value { get; }

            /// <summary>
            /// Applies the specified change to the current instance
            /// </summary>
            /// <param name="change">The change to be applied. This parameter cannot be <see cref="null"/></param>
            public abstract void ApplyChange([DisallowNull] U change);
        }

        /// <summary>
        /// Represents a base type for managing and applying changes to a value, with support for change notifications
        /// </summary>
        /// <remarks>This abstract class provides a framework for managing a value of type <typeparamref
        /// name="T"/> and applying changes of type <typeparamref name="U"/>.  It includes an event for notifying
        /// subscribers when a change is applied and defines an abstract method for implementing custom change
        /// application logic. Derived classes must implement the <see cref="ApplyChangeLogic"/> method to define how
        /// changes are applied.</remarks>
        /// <typeparam name="T">The type of the value being managed</typeparam>
        /// <typeparam name="U">The type of the change being applied, which must derive from <see cref="StatTypeChange"/></typeparam>
        public abstract class StatTypeBase<T, U> : IStatType<T, U> where U : StatTypeChange
        {
            /// <summary>
            /// Occurs when a change is applied
            /// </summary>
            /// <remarks>This event is triggered whenever a change is applied, passing the new <see cref="Value"/> and the change <typeparamref name="U"/>. 
            /// Subscribers can use this event to handle
            /// or respond to the applied changes</remarks>
            public event Action<T, U> OnApplyChange;

            /// <summary>
            /// Gets the current value of the object
            /// </summary>
            public abstract T Value { get; }

            /// <summary>
            /// Applies the specified change to the current value and triggers the change notification event
            /// </summary>
            /// <remarks>This method applies the provided change using internal logic and then invokes
            /// the <see cref="OnApplyChange"/> event, passing the updated value and the applied change as arguments.
            /// Subscribers to the event can use this to react to the change</remarks>
            /// <param name="change">The change to apply. Cannot be <see langword="null"/></param>
            public void ApplyChange([DisallowNull] U change)
            {
                ApplyChangeLogic(change);
                OnApplyChange?.Invoke(Value, change);
            }

            /// <summary>
            /// Applies the specified change to the current state of the object
            /// </summary>
            /// <remarks>This method is abstract and must be implemented by derived classes to define
            /// the specific logic for applying a change of type <typeparamref name="U"/>. The implementation should
            /// ensure that the change is applied in a consistent and valid manner</remarks>
            /// <param name="change">The change to be applied. This parameter must not be null</param>
            protected abstract void ApplyChangeLogic([DisallowNull] U change);
        }

        /// <summary>
        /// Represents a base class for defining changes to a <see cref="IStatType{T, U}"/>, including the type of change and its associated
        /// value
        /// </summary>
        public abstract class StatTypeChange
        {
            /// <summary>
            /// Represents the type of change applied to an object or property
            /// </summary>
            /// <remarks>This enumeration is typically used to indicate the nature of a modification, 
            /// such as whether a value was set, changed, or left unchanged</remarks>
            public enum ChangeType
            {
                /// <summary>
                /// Represents the absence of a value or a default state
                /// </summary>
                None,

                /// <summary>
                /// Sets the value of the specified property or field
                /// </summary>
                Set,

                /// <summary>
                /// Changes the value of the specified property or field
                /// </summary>
                Change
            }

            #region Change Field

            /// <summary>
            /// Represents a field that tracks a value and its associated <see cref="ChangeType"/>
            /// </summary>
            /// <remarks>This class is useful for scenarios where a value needs to be associated with
            /// a specific type of change, such as additions, deletions, or modifications. The <see cref="Type"/>
            /// property indicates the nature of the change, while the <see cref="Value"/> property holds the associated
            /// value</remarks>
            /// <typeparam name="T">The type of the value being tracked</typeparam>
            [Serializable]
            public class ChangeField<T>
            {
                // CHANGE TYPE
                // Header("Change Type")]

                /// <summary>
                /// The backing field that stores the <see cref="ChangeType"/> for this change
                /// </summary>
                [SerializeField] private ChangeType _type = ChangeType.None;
                
                /// <summary>
                /// Specifies the type of change to be applied
                /// </summary>
                /// <remarks>This field determines the nature of the change operation. The default
                /// value is <see cref="ChangeType.None"/></remarks>
                public ChangeType Type => _type;

                // VALUE
                // Header("Value")]

                /// <summary>
                /// The backing field that stores the <typeparamref name="T"/> value associated with this change
                /// </summary>

                [SerializeField] private T _value;

                /// <summary>
                /// Gets the <typeparamref name="T"/> value of the change
                /// </summary>
                public T Value => _value;

                /// <summary>
                /// Initializes a new instance of the <see cref="ChangeField{T}"/> class with the specified change type
                /// and value
                /// </summary>
                /// <param name="type">The type of change being represented</param>
                /// <param name="value">The value associated with the change</param>
                public ChangeField(ChangeType type, T value)
                {
                    _type = type;
                    _value = value;
                }
            }

#if UNITY_EDITOR
            /// <summary>
            /// Defines a custom property drawer for the <see cref="ChangeField{T}"/> class, allowing it to be displayed
            /// </summary>
            [CustomPropertyDrawer(typeof(ChangeField<>), true)]
            public class ChangeFieldDrawer : PropertyDrawer
            {
                /// <summary>
                /// Renders the custom GUI for <see cref="ChangeFieldDrawer"/> in the Unity Editor
                /// </summary>
                /// <remarks>This method customizes the rendering of a property by displaying a
                /// type field and, conditionally, a value field based on the selected type. The <see cref="ChangeField{T}._type"/>  field
                /// determines the <see cref="ChangeType"/> and controls whether the <see cref="ChangeField{T}._value"/> field is
                /// displayed</remarks>
                /// <param name="position">The rectangle on the screen to use for the property GUI</param>
                /// <param name="property">The serialized property to render, which must contain a relative <see cref="ChangeField{T}._type"/>  and <see cref="ChangeField{T}._value"/> field</param>
                /// <param name="label">The label to display alongside the property field</param>
                public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
                {
                    // Properties
                    SerializedProperty typeProperty = property.FindPropertyRelative("_type");
                    SerializedProperty valueProperty = property.FindPropertyRelative("_value");

                    // Start drawing the property
                    EditorGUI.BeginProperty(position, label, property);

                    // Draw the type field first
                    Rect fieldRect = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(fieldRect, typeProperty, label);

                    // Get the ChangeType from the typeProperty
                    ChangeType changeType = (ChangeType)typeProperty.enumValueIndex;

                    // Only draw the value field if the change type is not None
                    if (changeType != ChangeType.None)
                    {
                        // Move the rect down for the value field
                        fieldRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                        // Add indentation for the value field
                        EditorGUI.indentLevel++;
                        EditorGUI.PropertyField(fieldRect, valueProperty, new GUIContent(valueProperty.displayName));
                        EditorGUI.indentLevel--;
                    }

                    EditorGUI.EndProperty();
                }

                /// <summary>
                /// Calculates the height, in pixels, required to render the <see cref="ChangeFieldDrawer"/>
                /// </summary>
                /// <remarks>The height calculation includes the base line height for the property
                /// and, if the <see cref="ChangeField{T}._type"/> field indicates a value other than <see cref="ChangeType.None"/>, additional
                /// height for rendering the associated value field is included</remarks>
                /// <param name="property">The serialized property to calculate the height for. Must contain a <see cref="ChangeField{T}._type"/> field of type <see
                /// cref="ChangeType"/></param>
                /// <param name="label">The label used for the property in the inspector</param>
                /// <returns>The total height, in pixels, required to render the property</returns>
                public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
                {
                    // Line height for the type field
                    float totalHeight = EditorGUIUtility.singleLineHeight;

                    // Get the type property to determine if the value field should be drawn
                    SerializedProperty typeProperty = property.FindPropertyRelative("_type");
                    ChangeType changeType = (ChangeType)typeProperty.enumValueIndex;

                    // If the change type is not None, add height for the value field
                    if (changeType != ChangeType.None)
                    {
                        totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    }

                    return totalHeight;
                }
            }
#endif
            #endregion
        }
    }
}
