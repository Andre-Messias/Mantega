using System;
using UnityEngine;

namespace Mantega.Stats
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
#if UNITY_EDITOR
    using UnityEditor;
#endif
    public static partial class StatType
    {
        [Serializable]
        public class Primitive : StatTypeBase<object, PrimitiveChange>
        {
            [SerializeReference] private object _value;
            public override object Value => _value;

            protected override void ApplyChangeLogic(PrimitiveChange change)
            {
                _value = change.Value;
            }

            public override string ToString()
            {
                return _value.ToString();
            }
        }


#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(Primitive))]
        public class PrimitiveDrawer : PropertyDrawer
        {
            private static readonly Dictionary<string, System.Type> TypeMap = new Dictionary<string, System.Type>
    {
        {"None", null},
        {"Integer", typeof(int)},
        {"Float", typeof(float)},
        {"Boolean", typeof(bool)},
        {"String", typeof(string)},
        {"Vector2", typeof(Vector2)},
        {"Vector3", typeof(Vector3)},
        {"Color", typeof(Color)},
        {"Double", typeof(double)},
        {"Long", typeof(long)}
    };

            private static readonly string[] TypeNames = new string[]
            {
        "None", "Integer", "Float", "Boolean", "String", "Vector2", "Vector3", "Color", "Double", "Long"
            };

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                EditorGUI.BeginProperty(position, label, property);

                var valueProperty = property.FindPropertyRelative("_value");
                if (valueProperty == null)
                {
                    EditorGUI.HelpBox(position, "Could not find _value property", MessageType.Error);
                    EditorGUI.EndProperty();
                    return;
                }

                // Draw label
                var labelRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                property.isExpanded = EditorGUI.Foldout(labelRect, property.isExpanded, label, true);

                if (!property.isExpanded)
                {
                    EditorGUI.EndProperty();
                    return;
                }

                EditorGUI.indentLevel++;

                var currentY = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                // Determine current type
                int currentIndex = 0;
                var currentValue = valueProperty.managedReferenceValue;
                if (currentValue != null)
                {
                    var currentType = currentValue.GetType();
                    foreach (var kvp in TypeMap)
                    {
                        if (kvp.Value == currentType)
                        {
                            currentIndex = Array.IndexOf(TypeNames, kvp.Key);
                            break;
                        }
                    }
                }

                // Type selection dropdown
                var dropdownRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
                var newIndex = EditorGUI.Popup(dropdownRect, "Type", currentIndex, TypeNames);
                currentY += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                // Handle type change
                if (newIndex != currentIndex)
                {
                    if (newIndex == 0) // None
                    {
                        valueProperty.managedReferenceValue = null;
                        property.serializedObject.ApplyModifiedProperties();
                    }
                    else
                    {
                        var selectedType = TypeMap[TypeNames[newIndex]];
                        if (selectedType != null)
                        {
                            try
                            {
                                var newInstance = CreateDefaultValue(selectedType);
                                valueProperty.managedReferenceValue = newInstance;
                                property.serializedObject.ApplyModifiedProperties();
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"Failed to create instance of type {selectedType}: {ex.Message}");
                            }
                        }
                    }
                }

                // Draw value field if we have a type
                if (valueProperty.managedReferenceValue != null)
                {
                    var valueRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);

                    // Draw appropriate field based on type
                    DrawValueField(valueRect, valueProperty);
                }

                EditorGUI.indentLevel--;
                EditorGUI.EndProperty();
            }

            private object CreateDefaultValue(Type type)
            {
                if (type == typeof(int)) return 0;
                if (type == typeof(float)) return 0f;
                if (type == typeof(bool)) return false;
                if (type == typeof(string)) return "";
                if (type == typeof(Vector2)) return Vector2.zero;
                if (type == typeof(Vector3)) return Vector3.zero;
                if (type == typeof(Color)) return Color.white;
                if (type == typeof(double)) return 0.0;
                if (type == typeof(long)) return 0L;

                return Activator.CreateInstance(type);
            }

            private void DrawValueField(Rect position, SerializedProperty property)
            {
                var value = property.managedReferenceValue;
                if (value == null) return;

                var type = value.GetType();

                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Int32:
                        property.managedReferenceValue = EditorGUI.IntField(position, "Value", (int)value);
                        break;
                    case TypeCode.Single:
                        property.managedReferenceValue = EditorGUI.FloatField(position, "Value", (float)value);
                        break;
                    case TypeCode.Boolean:
                        property.managedReferenceValue = EditorGUI.Toggle(position, "Value", (bool)value);
                        break;
                    case TypeCode.String:
                        property.managedReferenceValue = EditorGUI.TextField(position, "Value", (string)value);
                        break;
                    case TypeCode.Double:
                        property.managedReferenceValue = EditorGUI.DoubleField(position, "Value", (double)value);
                        break;
                    case TypeCode.Int64:
                        property.managedReferenceValue = EditorGUI.LongField(position, "Value", (long)value);
                        break;
                    default:
                        if (type == typeof(Vector2))
                        {
                            property.managedReferenceValue = EditorGUI.Vector2Field(position, "Value", (Vector2)value);
                        }
                        else if (type == typeof(Vector3))
                        {
                            property.managedReferenceValue = EditorGUI.Vector3Field(position, "Value", (Vector3)value);
                        }
                        else if (type == typeof(Color))
                        {
                            property.managedReferenceValue = EditorGUI.ColorField(position, "Value", (Color)value);
                        }
                        else
                        {
                            EditorGUI.LabelField(position, "Unsupported type: " + type.Name);
                        }
                        break;
                }
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                if (!property.isExpanded)
                    return EditorGUIUtility.singleLineHeight;

                float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Foldout
                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Dropdown

                var valueProperty = property.FindPropertyRelative("_value");
                if (valueProperty != null && valueProperty.managedReferenceValue != null)
                {
                    var type = valueProperty.managedReferenceValue.GetType();
                    if (type == typeof(Vector3))
                        height += EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;
                    else if (type == typeof(Vector2))
                        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    else
                        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                }

                return height;
            }
        }
#endif

        [Serializable]
        public class PrimitiveChange : StatTypeChange
        {
            [SerializeReference] public object Value;
        }
    }
}