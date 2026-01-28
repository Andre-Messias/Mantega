namespace Mantega.Diagnostics
{
    using System.Runtime.CompilerServices;
    using UnityEngine;

    public static class Validations 
    {
        private static string BuildErrorMessage(string reason, string paramName, string typeName, Object context)
        {
            string header = context != null ? $"[{context.GetType().Name}]" : "[Mantega]";
            string location = context != null ? $" on GameObject '{context.name}'" : "";

            return $"{header} Validation failed: {reason}. Variable '{paramName}' (Type: {typeName}){location}.";
        }

        #region Validate Not Null
        public static void ValidateNotNull<T>(T obj, Object context = null, [CallerArgumentExpression("obj")] string paramName = "") where T : class
        {
            if (IsNull(obj))
            {
                string message = BuildErrorMessage("The reference is null", paramName, typeof(T).Name, context);
                throw new System.ArgumentNullException(paramName, message);
            }
        }

        private static bool IsNull<T>(T obj)
        {
            return obj == null || obj.Equals(null);
        }

        #endregion

        #region Validate Component Exists
        public static void ValidateComponentExists<T>(GameObject target, Object context = null, [CallerArgumentExpression("target")] string paramName = "") where T : Component
        {
            ValidateNotNull(target, context, paramName);

            if (target.GetComponent<T>() == null)
            {
                string message = BuildErrorMessage($"Required component '{typeof(T).Name}' not found", paramName, "GameObject", context);
                throw new MissingComponentException(message);
            }
        }

        public static void ValidateComponentInChildrenExists<T>(GameObject target, Object context = null, bool includeInactive = true, [CallerArgumentExpression("target")] string paramName = "") where T : Component
        {
            ValidateNotNull(target, context, paramName);

            if (target.GetComponentInChildren<T>(includeInactive) == null)
            {
                string inactiveNotice = includeInactive ? " (including inactive elements)" : "";
                string reason = $"Required component '{typeof(T).Name}' not found in hierarchy{inactiveNotice}";

                string message = BuildErrorMessage(reason, paramName, "GameObject", context);
                throw new MissingComponentException(message);
            }
        }
        #endregion

        public static void ValidateNotNegative(int value, Object context = null, [CallerArgumentExpression("value")] string paramName = "")
        {
            if (value < 0)
                ThrowOutOfRange(paramName, value, "int", context);
        }

        public static void ValidateNotNegative(float value, Object context = null, [CallerArgumentExpression("value")] string paramName = "")
        {
            if (value < 0)
                ThrowOutOfRange(paramName, value, "float", context);
        }

        private static void ThrowOutOfRange(string paramName, object actualValue, string typeName, Object context)
        {
            string reason = $"The value is negative (Current value: {actualValue})";
            string message = BuildErrorMessage(reason, paramName, typeName, context);

            throw new System.ArgumentOutOfRangeException(paramName, actualValue, message);
        }
    }
}
