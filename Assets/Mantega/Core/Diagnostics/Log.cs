namespace Mantega.Core.Diagnostics
{
    using UnityEngine;

    /// <summary>
    /// Provides static methods for logging warning and error messages to the Unity console.
    /// </summary>
    /// <remarks>The Log class offers utility methods to standardize the formatting and output of diagnostic
    /// messages in Unity projects. By including an optional context object, log entries can be associated with specific
    /// Unity objects, aiding in debugging and issue tracking within the Unity Editor. All methods are static and
    /// intended for use throughout the application wherever logging is required.</remarks>
    public static class Log
    {
        /// <summary>
        /// Formats a diagnostic message.
        /// </summary>
        /// <remarks>If <paramref name="context"/> is null, the message uses a default header and omits
        /// location information. This method is intended for internal diagnostic or logging purposes.</remarks>
        /// <param name="reason">The reason to include in the formatted message.</param>
        /// <param name="context">The object representing the context for the message. If not null, its type and name are included in the
        /// formatted output.</param>
        /// <returns>A string containing the formatted message with context details and the specified reason.</returns>
        internal static string FormatMessage(string reason, Object context = null)
        {
            string header = context != null ? $"[{context.GetType().Name}]" : "[Mantega]";
            string location = context != null ? $" on GameObject '{context.name}'" : "";

            return $"{header} {reason} {location}";
        }

        /// <summary>
        /// Logs a warning message to the Unity console.
        /// </summary>
        /// <remarks>Use this method to highlight potential issues or non-critical problems during
        /// runtime. The warning will appear in the Unity console and can help identify areas that may require attention
        /// without interrupting execution.</remarks>
        /// <param name="message">The warning message to log. Cannot be null.</param>
        /// <param name="context">An optional object that provides context for the warning. If specified, the context object will be
        /// associated with the log entry in the Unity console.</param>
        public static void Warning(string message, Object context = null)
        {
            string safeMessage = message ?? "[Null Message]";
            string formatted = FormatMessage(safeMessage, context);
            Debug.LogWarning(formatted, context);
        }

        /// <summary>
        /// Logs an error message to the Unity console.
        /// </summary>
        /// <remarks>Use this method to report errors that should be visible in the Unity Editor's
        /// console. Associating a context object helps identify the source of the error within the scene or
        /// project.</remarks>
        /// <param name="message">The error message to log. Cannot be null.</param>
        /// <param name="context">An optional Unity object to associate with the error message. If provided, the message will be linked to
        /// this object in the console.</param>
        public static void Error(string message, Object context = null)
        {
            string safeMessage = message ?? "[Null Message]";
            string formatted = FormatMessage(safeMessage, context);
            Debug.LogError(formatted, context);
        }
    }
}