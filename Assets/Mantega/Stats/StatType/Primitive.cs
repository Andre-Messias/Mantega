using System;
using UnityEngine;

namespace Mantega.Stats
{
    using Mantega.Reflection;

#if UNITY_EDITOR
    using Unity.VisualScripting;
    using UnityEditor;
    using Mantega.Editor;
#endif
    public static partial class StatType
    {
        private sealed class WrapperManager
        {
            public interface IWrapper
            {
                object GetValue();
            }

            [Serializable]
            public struct Wrapper<T> : IWrapper
            {
                [SerializeField] public T Content;

                public Wrapper(T value)
                {
                    Content = value;
                }

                public object GetValue() => Content;

            }
            public static object WrapperFromObject(object obj)
            {
                if(obj == null) return null;
                Type genericWrapperType = typeof(Wrapper<>).MakeGenericType(obj.GetType());
                object wrapper = Activator.CreateInstance(genericWrapperType, new object[] { obj });
                return wrapper;
            }

        }

        [Serializable]
        public class Primitive : StatTypeBase<object, PrimitiveChange>
        {
            [SerializeReference, SerializeField] private object _value = null;
            public override object Value
            {
                get
                {
                    if (_value is WrapperManager.IWrapper wrapper)
                        return wrapper.GetValue();

                    return _value;
                }
            }

            public Primitive() => _value = WrapperManager.WrapperFromObject(null);

            public Primitive(object content = null)
            {
                _value = WrapperManager.WrapperFromObject(content);
            }

            protected override void ApplyChangeLogic(PrimitiveChange change)
            {
                if(ReflectionUtils.CanConvert(change.Value, Value, out object converted))
                    _value = WrapperManager.WrapperFromObject(converted);
                else 
                    Debug.LogWarning($"Failed to convert {change.Value?.GetType()} to {Value?.GetType()}, no change was made");
            }

            public override string ToString()
            {
                if(_value == null) return "Null";
                return _value.ToString();
            }
        }

        [Serializable]
        public class PrimitiveChange : StatTypeChange
        {
            [SerializeReference] public object _value;
            public object Value
            {
                get
                {
                    if (_value is WrapperManager.IWrapper wrapper)
                        return wrapper.GetValue();

                    return _value;
                }
            }
        }

        #region Editor
#if UNITY_EDITOR
        public abstract class BasePrimitiveDrawer : PropertyDrawer
        {
            SerializedProperty _valueProp = null;
            private string _lastInput = "";

            private bool _initialized = false;
            protected void Initialize(SerializedProperty property)
            {
                if (_initialized) return;
                _initialized = true;

                _valueProp = property.FindPropertyRelative("_value");
                

                _lastInput = GetPropertyValue(_valueProp)?.GetType().FullName ?? "Null";
            }

            protected object GetPropertyValue(SerializedProperty property)
            {
                if (_valueProp.managedReferenceValue is WrapperManager.IWrapper wrapper)
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
                        if (SetValueType(inputType)) 
                            property.serializedObject.ApplyModifiedProperties();
                        else 
                            Debug.LogError($"Failed to create instance of {inputType}");
                    }

                    if(GetPropertyValue(_valueProp) != null)
                    {
                        var valueRect = new Rect(position.x, currentY, position.width, EditorGUI.GetPropertyHeight(_valueProp, true));
                        EditorGUI.PropertyField(valueRect, _valueProp, new("Value"), true);

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

            protected bool SetValueType(Type type)
            {
                try
                {
                    object value;

                    if (type == typeof(string))
                        value = string.Empty;
                    else if (type == typeof(char))
                        value = '&';
                    else
                        value = Activator.CreateInstance(type);

                    _valueProp.managedReferenceValue = WrapperManager.WrapperFromObject(value);
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

        [CustomPropertyDrawer(typeof(Primitive))]
        public class PrimitiveDrawer : BasePrimitiveDrawer { }

        [CustomPropertyDrawer(typeof(PrimitiveChange))]
        public class PrimitiveChangeDrawer : BasePrimitiveDrawer { }
#endif
        #endregion
    }
}