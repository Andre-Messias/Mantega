namespace Mantega.Core.Reactive.Editor
{
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Provides a custom property drawer for <see cref="Syncable{T}"/> objects.
    /// </summary>
    /// <remarks>This class customizes how <see cref="Syncable{T}"/> properties appear in the Unity Inspector by rendering
    /// only the underlying value field, rather than the entire object.</remarks>
    [CustomPropertyDrawer(typeof(Syncable<>))]
    public class SyncableDrawer : PropertyDrawer
    {
        /// <summary>
        /// Draws the property field for the underlying value of a <see cref="Syncable{T}"/> object in the Unity Editor.
        /// </summary>
        /// <remarks>This method customizes how <see cref="Syncable{T}"/> properties are displayed in the Unity
        /// Inspector by rendering only the underlying value field</remarks>
        /// <param name="position">The rectangle on the screen to use for the property field.</param>
        /// <param name="property">The serialized property representing the <see cref="Syncable{T}"/> object to be drawn.</param>
        /// <param name="label">The label to display for the property field.</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Get and draw the "_value" property of the Syncable<T> class
            SerializedProperty valueProp = property.FindPropertyRelative("_value");
            EditorGUI.PropertyField(position, valueProp, label, true);
        }

        /// <summary>
        /// Calculates the vertical space required to display the specified property in the Inspector, including its
        /// child fields if expanded.
        /// </summary>
        /// <param name="property">The serialized property for which to determine the display height. Cannot be <see langword="null"/>.</param>
        /// <param name="label">The label to use for the property field. This determines how the property is displayed in the Inspector.</param>
        /// <returns>The height, in pixels, needed to render the property and its children in the Inspector.</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty valueProp = property.FindPropertyRelative("_value");
            return EditorGUI.GetPropertyHeight(valueProp, true);
        }
    }
}