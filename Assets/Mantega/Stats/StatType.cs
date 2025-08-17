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

                public ChangeField(ChangeType type, T value)
                {
                    _type = type;
                    _value = value;
                }
            }

#if UNITY_EDITOR
            [CustomPropertyDrawer(typeof(ChangeField<>), true)]
            public class ChangeFieldDrawer : PropertyDrawer
            {
                // Draws the property in the Inspector
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

                // Returns the height needed to draw the property in the Inspector
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
