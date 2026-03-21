namespace Mantega.Core.Editor
{
    using System.Reflection;
    using UnityEngine;
    using UnityEditor;

    using Mantega.Core;
    using System;

    /// <summary>
    /// Drawer for the <see cref="CallOnChangeAttribute"/> that invokes a method when the property changes
    /// </summary>
    [CustomPropertyDrawer(typeof(CallOnChangeAttribute))]
    public class CallOnChangeDrawer : PropertyDrawer
    {
        /// <summary>
        /// Draws the property in the Unity Editor and invokes the specified method when the property value changes.
        /// </summary>
        /// <param name="position">The position on the screen to draw the property field.</param>
        /// <param name="property">The <see cref="SerializedProperty"/> being drawn.</param>
        /// <param name="label">Represents the label to display alongside the property field in the Unity Editor.</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            object targetObject = GetParentObject(property);
            if (property.isArray || property.propertyPath.Contains(".Array.data["))
            {
                EditorGUI.PropertyField(position, property, label, true);

                Rect helpBoxRect = new(position.x, position.y + EditorGUI.GetPropertyHeight(property, label, true) + 2, position.width, 30);
                EditorGUI.HelpBox(helpBoxRect, "[CallOnChange] not supported on Arrays/Lists. Use OnValidate.", MessageType.Warning);
                return;
            }
            else if (targetObject == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                Rect helpBoxRect = new(position.x, position.y + EditorGUI.GetPropertyHeight(property, label, true) + 2, position.width, 30);
                EditorGUI.HelpBox(helpBoxRect, "[CallOnChange] Could not find target object. Make sure the property is not static and is a field of a MonoBehaviour or ScriptableObject.", MessageType.Warning);
                return;
            }

            // Get the current (old) value
            object rawOldValue = fieldInfo.GetValue(targetObject);
            object oldExtractedValue = rawOldValue;
            if (rawOldValue is IValueContainer oldContainerBase)
            {
                oldExtractedValue = oldContainerBase.GetValue();
            }

            object clonedOldValue = oldExtractedValue switch
            {
                ITypedCloneable<object> typed => typed.Clone(),
                ICloneable cloneable => cloneable.Clone(),
                _ => oldExtractedValue // Fallback 
            };

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
                object rawNewValue = fieldInfo.GetValue(targetObject);
                object newExtractedValue = rawNewValue;
                if (rawNewValue is IValueContainer newContainerBase)
                {
                    newExtractedValue = newContainerBase.GetValue();
                }

                // Check for change
                if (Equals(clonedOldValue, newExtractedValue)) return;
                

                // Get the target object the property belongs to
                var currentType = targetObject.GetType();

                // Find the method with the specified name
                MethodInfo method = null;

                while (currentType != null && method == null)
                {
                    method = currentType.GetMethod(callOnChangeAttribute.MethodName,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    // Method not found in the current type
                    if (method == null)
                    {
                        currentType = currentType.BaseType;
                    }
                }

                if (method != null)
                {
                    var parameters = method.GetParameters();

                    object arg1 = clonedOldValue;
                    object arg2 = newExtractedValue;

                    void executeMethod()
                    {
                        if (targetObject == null) return;

                        switch (parameters.Length)
                        {
                            case 0:
                                method.Invoke(targetObject, null);
                                break;
                            case 1:
                                method.Invoke(targetObject, new[] { arg2 });
                                break;
                            case 2:
                                method.Invoke(targetObject, new[] { arg1, arg2 });
                                break;
                            default:
                                Debug.LogError($"[CallOnChange] Method '{callOnChangeAttribute.MethodName}' in '{currentType.Name}' must have either 0, 1 or 2 parameters.");
                                break;
                        }

                        if (!callOnChangeAttribute.UseDelayCall)
                        {
                            property.serializedObject.Update();
                        }
                    };

                    if (callOnChangeAttribute.UseDelayCall)
                    {
                        EditorApplication.delayCall += executeMethod;
                    }
                    else
                    {
                        executeMethod();
                    }
                }
                else
                {
                    Debug.LogWarning($"[CallOnChange] Method '{callOnChangeAttribute.MethodName}' not found in '{currentType.Name}'");
                }
            }
        }

        /// <summary>
        /// Calculates the height, in pixels, required to render the specified property in the Unity Editor.
        /// </summary>
        /// <param name="property">The <see cref="SerializedProperty"/> to calculate the height for.</param>
        /// <param name="label">The label to display alongside the property in the Unity Editor.</param>
        /// <returns>The height, in pixels, required to render the property, including any child properties if applicable.</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        /// <summary>
        /// Retrieves the parent object of the specified serialized property.
        /// </summary>
        /// <remarks>This method traverses the property path of the given <see
        /// cref="SerializedProperty"/> to locate its parent object. It uses reflection to access fields, including
        /// private and public fields, along the property path.</remarks>
        /// <param name="property">The <see cref="SerializedProperty"/> for which the parent object is to be retrieved.</param>
        /// <returns>The parent object of the specified serialized property, or <see langword="null"/> if the parent object
        /// cannot be determined.</returns>
        private static object GetParentObject(SerializedProperty property)
        {
            object obj = property.serializedObject.targetObject;
            string[] elements = property.propertyPath.Split('.');

            for (int i = 0; i < elements.Length - 1; i++)
            {
                FieldInfo field = obj.GetType().GetField(elements[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field == null) return null;
                obj = field.GetValue(obj);
            }

            return obj;
        }
    }
}