#if UNITY_EDITOR
namespace Mantega.Core.Editor
{
    using System.Reflection;
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    /// Drawer for the <see cref="CallOnChangeAttribute"/> that invokes a method when the property changes
    /// </summary>
    [CustomPropertyDrawer(typeof(CallOnChangeAttribute))]
    public class CallOnChangeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            object targetObject = GetParentObject(property);

            // Get the current (old) value
            object oldValue = fieldInfo.GetValue(targetObject);

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
                var targetType = targetObject.GetType();

                // Find the method with the specified name
                MethodInfo method = targetType.GetMethod(callOnChangeAttribute.MethodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (method != null)
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length == 2)
                    {
                        // Call the method
                        method.Invoke(targetObject, new[] { oldValue, newValue });
                    }
                    else if (parameters.Length == 0)
                    {
                        method.Invoke(targetObject, null);
                    }
                    else
                    {
                        Debug.LogError($"[CallOnChange] Method '{callOnChangeAttribute.MethodName}' in '{targetType.Name}' must have either 0 or 2 parameters (old value and new value).");
                    }
                }
                else
                {
                    Debug.LogWarning($"[CallOnChange] Method '{callOnChangeAttribute.MethodName}' not found in '{targetType.Name}'");
                }
            }
        }

        /// <summary>
        /// Calculates the height, in pixels, required to render the specified property in the Unity Editor
        /// </summary>
        /// <param name="property">The <see cref="SerializedProperty"/> to calculate the height for</param>
        /// <param name="label">The label to display alongside the property in the Unity Editor</param>
        /// <returns>The height, in pixels, required to render the property, including any child properties if applicable</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        /// <summary>
        /// Retrieves the parent object of the specified serialized property
        /// </summary>
        /// <remarks>This method traverses the property path of the given <see
        /// cref="SerializedProperty"/> to locate its parent object. It uses reflection to access fields, including
        /// private and public fields, along the property path</remarks>
        /// <param name="property">The <see cref="SerializedProperty"/> for which the parent object is to be retrieved</param>
        /// <returns>The parent object of the specified serialized property, or <see langword="null"/> if the parent object
        /// cannot be determined</returns>
        private static object GetParentObject(SerializedProperty property)
        {
            object obj = property.serializedObject.targetObject;
            string[] path = property.propertyPath.Split('.');

            for (int i = 0; i < path.Length - 1; i++)
            {
                var type = obj.GetType();
                FieldInfo field = type.GetField(path[i],
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (field == null) return null;
                obj = field.GetValue(obj);
            }

            return obj;
        }
    }
}
#endif