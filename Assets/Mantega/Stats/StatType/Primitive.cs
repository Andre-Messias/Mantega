using System;
using UnityEngine;

namespace Mantega.Stats
{
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

        [Serializable]
        public class Wrapper<T>
        {
            public T Value;
        }


#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(Primitive))]
        public class PrimitiveDrawer : PropertyDrawer
        {
            SerializedProperty _valueProp = null;
            object _value = null;
            private string _lastInput = "";
            private bool _initialized = false;

            private void Initialize(SerializedProperty property)
            {
                if (_initialized) return;
                _initialized = true;

                _valueProp = property.FindPropertyRelative("_value");
                _value = _valueProp.managedReferenceValue;
                _lastInput = _value?.GetType().FullName ?? "";
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                EditorGUI.BeginProperty(position, label, property);
                Initialize(property);

                // Label field
                var labelRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                string input = EditorGUI.TextField(labelRect, label.text, _lastInput);

                // Check if input changed
                if (input != _lastInput)
                {
                    _lastInput = input;

                    // Has text
                    if (!string.IsNullOrEmpty(input))
                    {
                        Type inputType = Type.GetType(input);
                        // Valid type and change
                        if (inputType != null)
                        {
                            if (_value == null || _value.GetType() != inputType)
                            {
                                try
                                {
                                    _valueProp.managedReferenceValue = Activator.CreateInstance(inputType);
                                    _value = _valueProp.managedReferenceValue;
                                }
                                catch (System.Exception ex)
                                {
                                    Debug.LogError($"Failed to create instance of {inputType}: {ex.Message}");
                                }
                            }
                        }
                    }
                }

                float currentY = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                property.serializedObject.ApplyModifiedProperties();

                // Show help or value field
                if (string.IsNullOrEmpty(input))
                {
                    EditorGUI.HelpBox(new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight * 2),
                        "Write a type, ex: System.Int32", MessageType.Warning);
                }
                else if (Type.GetType(input) == null)
                {
                    EditorGUI.HelpBox(new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight * 2),
                        "Type not found, write a valid type, ex: System.Int32", MessageType.Warning);
                }
                else if (_valueProp.managedReferenceValue != null)
                {
                    var valueRect = new Rect(position.x, currentY, position.width, EditorGUI.GetPropertyHeight(_valueProp, true));
                    EditorGUI.PropertyField(valueRect, _valueProp, label, true);
                }

                EditorGUI.EndProperty();
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                Initialize(property);

                float height = EditorGUIUtility.singleLineHeight; // Campo de texto do tipo

                height += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight * 2; // Espaço para HelpBox

                if (_valueProp != null && _valueProp.managedReferenceValue != null)
                {
                    // Se temos um valor, adiciona a altura do campo de propriedade
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