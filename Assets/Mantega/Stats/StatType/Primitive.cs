using System;
using UnityEngine;

namespace Mantega.Stats
{
#if UNITY_EDITOR
    using Unity.VisualScripting;
    using UnityEditor;
    using Mantega.Editor;
#endif
    public static partial class StatType
    {
        [Serializable]
        public class Primitive : StatTypeBase<object, PrimitiveChange>
        {
            public interface IWrapper
            {
                object GetValue();
            }

            [Serializable]
            public struct Wrapper<T> : IWrapper
            {
                public T Content;

                public Wrapper(T value)
                {
                    Content = value;
                }

                public object GetValue() => Content;
            }

            [SerializeReference] private object _value;
            public override object Value
            {
                get
                {
                    if (_value is IWrapper wrapper)
                        return wrapper.GetValue();

                    return _value;
                }
            }

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
            SerializedProperty _valueProp = null;
            private string _lastInput = "";

            private bool _initialized = false;
            private void Initialize(SerializedProperty property)
            {
                if (_initialized) return;
                _initialized = true;

                _valueProp = property.FindPropertyRelative("_value");
                

                _lastInput = GetPropertyValue(_valueProp)?.GetType().FullName ?? "Null";
            }

            private object GetPropertyValue(SerializedProperty property)
            {
                if (_valueProp.managedReferenceValue is Primitive.IWrapper wrapper)
                {
                    return wrapper.GetValue();
                }
                return _valueProp.managedReferenceValue;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                EditorGUI.BeginProperty(position, label, property);
                Initialize(property);

                // Label field
                var labelRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                string input = EditorGUI.TextField(labelRect, label.text, _lastInput);

                Type inputType = Type.GetType(input);

                // Check if input changed
                if (input != _lastInput)
                {
                    _lastInput = input;

                    // Has text
                    if (!string.IsNullOrEmpty(input))
                    {
                        inputType = Type.GetType(input);
                        // Valid type and change
                        if (inputType != null)
                        {
                            if (_valueProp.managedReferenceValue == null || _valueProp.managedReferenceValue.GetType() != inputType)
                            {                                
                                if(!SetValueType(inputType)) Debug.LogError($"Failed to create instance of {inputType}");
                                
                                property.serializedObject.ApplyModifiedProperties();
                            }
                        }
                    }
                }

                float currentY = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                
                // Show help or value field
                if (string.IsNullOrEmpty(input))
                {
                    EditorGUI.HelpBox(new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight * 2),
                        "Write a type, ex: System.Int32", MessageType.Warning);
                }
                else if (inputType == null)
                {
                    EditorGUI.HelpBox(new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight * 2),
                        "Type not found, write a valid type, ex: System.Int32", MessageType.Warning);
                }
                else 
                {
                    if (GetPropertyValue(_valueProp) == null)
                    {
                        if (!SetValueType(inputType)) Debug.LogError($"Failed to create instance of {inputType}");

                        property.serializedObject.ApplyModifiedProperties();
                    }

                    if(GetPropertyValue(_valueProp) != null)
                    {
                        var valueRect = new Rect(position.x, currentY, position.width, EditorGUI.GetPropertyHeight(_valueProp, true));
                        EditorGUI.PropertyField(valueRect, _valueProp, label, true);

                        // Display JSON representation of Value
                        string json = _valueProp.managedReferenceValue.Serialize().json;
                        float height = MantegaStyles.JsonStyle.CalcHeight(new GUIContent(json), EditorGUIUtility.currentViewWidth);
                        EditorGUILayout.SelectableLabel(json, MantegaStyles.JsonStyle, GUILayout.Height(height));
                    }
                    else
                    {
                        EditorGUI.HelpBox(new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight * 2),
                            "Type is valid but instance could not be created", MessageType.Error);
                    }   
                }

                EditorGUI.EndProperty();
            }

            private bool SetValueType(Type type)
            {
                try
                {
                    object value;
                    if (type == typeof(string))
                        value = string.Empty;
                    else
                        value = Activator.CreateInstance(type);

                    Type genericWrapperType = typeof(Primitive.Wrapper<>).MakeGenericType(type);
                    object wrapper = Activator.CreateInstance(genericWrapperType, new object[] { value });
                    _valueProp.managedReferenceValue = wrapper;
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to create instance of {type}: {ex.Message}");
                    return false;
                }
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                Initialize(property);

                float height = EditorGUIUtility.singleLineHeight;

                height += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight * 2;

                if (_valueProp != null && _valueProp.managedReferenceValue != null)
                {
                    height += EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(_valueProp, true);
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