namespace Mantega.Core.Diagnostics
{
    using UnityEngine;

    public static class Log
    {
        internal static string FormatMessage(string reason, string contextName, Object context)
        {
            string header = context != null ? $"[{context.GetType().Name}]" : "[Mantega]";
            string location = context != null ? $" on GameObject '{context.name}'" : "";

            return $"{header} {reason} {location}";
        }

        public static void Warning(string message, Object context = null)
        {
            string formatted = FormatMessage(message, "", context);
            Debug.LogWarning(formatted, context);
        }

        public static void Error(string message, Object context = null)
        {
            string formatted = FormatMessage(message, "", context);
            Debug.LogError(formatted, context);
        }
    }
}