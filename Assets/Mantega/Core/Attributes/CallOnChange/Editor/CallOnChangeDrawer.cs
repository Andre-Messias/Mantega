namespace Mantega.Core.Editor
{
    using System.Reflection;
    using UnityEngine;
    using UnityEditor;

    using Mantega.Core;
    using Mantega.Core.Diagnostics;

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
            if(targetObject == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                Log.Warning($"[CallOnChange] Could not find target object for property '{property.name}' in '{property.serializedObject.targetObject.GetType().Name}'.", property.serializedObject.targetObject);
                return;
            }

            // Get the current (old) value
            object oldValue = fieldInfo.GetValue(targetObject);
            if (oldValue is System.ICloneable cloneable)
            {
                oldValue = cloneable.Clone(); 
            }
            else if (oldValue is System.Collections.IList)
            {
                var type = oldValue.GetType();
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>))
                {
                    oldValue = System.Activator.CreateInstance(type, oldValue);
                }
            }

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

                    object arg1 = oldValue;
                    object arg2 = newValue;

                    if (oldValue is IValueContainer oldContainer)
                    {
                        // If the method expects a parameter that is not the same type as the old value, try to get the value from the container
                        if (parameters.Length > 0 && !parameters[0].ParameterType.IsInstanceOfType(oldValue))
                        {
                            arg1 = oldContainer.GetValue();
                        }
                    }

                    if (newValue is IValueContainer newContainer)
                    {
                        // If the method expects a parameter that is not the same type as the new value, try to get the value from the container
                        if (parameters.Length > 1 && !parameters[1].ParameterType.IsInstanceOfType(newValue))
                        {
                            arg2 = newContainer.GetValue();
                        }
                        // If the method expects only one parameter and it's not the same type as the new value, try to get the value from the container
                        else if (parameters.Length == 1 && !parameters[0].ParameterType.IsInstanceOfType(newValue))
                        {
                            arg2 = newContainer.GetValue();
                        }
                    }

                    switch(parameters.Length)
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
                            Debug.LogError($"[CallOnChange] Method '{callOnChangeAttribute.MethodName}' in '{currentType.Name}' must have either 0, 1 or 2 parameters (old value and/or new value).");
                            break;
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
            // The property path uses ".Array.data[index]" for array elements, so we need to replace it with "[index]" to correctly parse the path
            string path = property.propertyPath.Replace(".Array.data[", "[");
            string[] elements = path.Split('.');

            for (int i = 0; i < elements.Length - 1; i++)
            {
                string element = elements[i];
                if (element.Contains("["))
                {
                    string elementName = element[..element.IndexOf("[")];
                    int index = System.Convert.ToInt32(element[element.IndexOf("[")..].Replace("[", "").Replace("]", ""));

                    FieldInfo field = obj.GetType().GetField(elementName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (field == null) return null;

                    obj = field.GetValue(obj);
                    if (obj is System.Collections.IList list && index < list.Count)
                    {
                        obj = list[index];
                    }
                    else return null;
                }
                else
                {
                    FieldInfo field = obj.GetType().GetField(element, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (field == null) return null;
                    obj = field.GetValue(obj);
                }
            }

            return obj;
        }
    }
}