namespace Mantega.Core.Editor
{
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Provides custom GUI styles for use in Unity editor extensions
    /// </summary>
    /// <remarks>This class contains predefined styles that can be used to maintain consistent visual
    /// design in Unity editor tools. The styles are read-only and optimized for common use cases</remarks>
    public sealed class MantegaStyles
    {
        /// <summary>
        /// Represents a read-only GUI style for displaying JSON content in a help box format
        /// </summary>
        /// <remarks>This style is based on <see cref="EditorStyles.helpBox"/> and includes
        /// additional padding  and word wrapping to enhance readability of JSON content</remarks>
        readonly static GUIStyle _jsonStyle = new(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 10, 10),
            wordWrap = true // Line wrap
        };

        /// <summary>
        /// Gets the GUI style used for rendering JSON content in the user interface
        /// </summary>
        public static GUIStyle JsonStyle => _jsonStyle;
    } 
}