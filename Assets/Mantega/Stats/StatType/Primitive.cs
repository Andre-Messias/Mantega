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
        /// <summary>
        /// Provides functionality for wrapping objects in a generic wrapper type
        /// </summary>
        /// <remarks>The <see cref="WrapperManager"/> class includes a generic wrapper structure and
        /// methods for creating instances of the wrapper dynamically. This can be useful for scenarios where objects
        /// need to be encapsulated in a uniform type for processing or serialization</remarks>
        private sealed class WrapperManager
        {
            /// <summary>
            /// Defines a contract for a wrapper that provides access to an underlying value
            /// </summary>
            /// <remarks>This interface is typically used to abstract access to a value, allowing the
            /// implementation  to encapsulate the value and provide additional behavior if needed</remarks>
            public interface IWrapper
            {
                /// <summary>
                /// Retrieves the current value associated with the object
                /// </summary>
                /// <returns>The current value as an <see cref="object"/>. The returned value may be <see langword="null"/> if no
                /// value is set</returns>
                object GetValue();
            }

            /// <summary>
            /// Represents a wrapper for a value of type <typeparamref name="T"/>
            /// </summary>
            /// <remarks>This struct is designed to encapsulate a value of type <typeparamref
            /// name="T"/> and provide additional functionality, such as retrieving the value as an object</remarks>
            /// <typeparam name="T">The type of the value being wrapped</typeparam>
            [Serializable]
            public struct Wrapper<T> : IWrapper
            {
                /// <summary>
                /// The content being wrapped
                /// </summary>
                [SerializeField] public T Content;

                /// <summary>
                /// Initializes a new instance of the <see cref="Wrapper{T}"/> class with the specified value
                /// </summary>
                /// <param name="value">The value to be wrapped by this instance</param>
                public Wrapper(T value)
                {
                    Content = value;
                }

                /// <summary>
                /// Retrieves the value of the content
                /// </summary>
                /// <returns>The value of the content as an <see cref="object"/>. Returns <see langword="null"/> if the content
                /// is not set</returns>
                public readonly object GetValue()
                {
                    return Content;
                }
            }

            /// <summary>
            /// Creates a generic wrapper instance for the specified object
            /// </summary>
            /// <remarks>The method dynamically creates an instance of a generic wrapper type
            /// <see cref="Wrapper{T}"/> using the runtime type of the provided object. This allows the object to be
            /// encapsulated in a strongly-typed wrapper at runtime</remarks>
            /// <param name="obj">The object to be wrapped. Can be of any type</param>
            /// <returns>A generic wrapper instance of type <see cref="Wrapper{T}"/>, where <c>T</c> is the runtime type of
            /// <paramref name="obj"/> Returns <see langword="null"/> if <paramref name="obj"/> is <see
            /// langword="null"/></returns>
            public static object WrapperFromObject(object obj)
            {
                if (obj == null) return null;
                Type genericWrapperType = typeof(Wrapper<>).MakeGenericType(obj.GetType());
                object wrapper = Activator.CreateInstance(genericWrapperType, new object[] { obj });
                return wrapper;
            }
        }

        /// <summary>
        /// Represents a primitive stat type that holds a value of any object type
        /// </summary>
        /// <remarks>The <see cref="Primitive"/> class provides functionality to store and manage a value
        /// of any object type, with support for applying changes through the <see cref="PrimitiveChange"/> type. The
        /// value can be wrapped using a wrapper object, if applicable, to provide additional behavior or
        /// processing</remarks>
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
                if (ReflectionUtils.CanConvert(change.Value, Value, out object converted))
                    _value = WrapperManager.WrapperFromObject(converted);
                else
                    Debug.LogWarning($"Failed to convert {change.Value?.GetType()} to {Value?.GetType()}, no change was made");
            }

            public override string ToString()
            {
                if (_value == null) return "Null";
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
                                if (!SetValueType(inputType)) Debug.LogError($"Failed to create instance of {inputType}");

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

                    if (GetPropertyValue(_valueProp) != null)
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