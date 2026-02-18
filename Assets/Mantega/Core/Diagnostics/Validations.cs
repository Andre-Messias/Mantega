namespace Mantega.Core.Diagnostics
{
    using UnityEngine;
    using System.Runtime.CompilerServices;

    public static class Validations 
    {
        private static string BuildErrorMessage(string reason, string paramName, string typeName, Object context)
        {
            string baseMsg = $"Validation failed: {reason}. Variable '{paramName}' (Type: {typeName})";
            return Log.FormatMessage(baseMsg, context);
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

        #region Validate Not Null or Empty
        public static void ValidateNotNullOrEmpty(string str, Object context = null, [CallerArgumentExpression("str")] string paramName = "")
        {
            ValidateNotNull(str, context, paramName);
            if (str.Length == 0)
            {
                ThrowEmpty(paramName, nameof(System.String), context);
            }
        }

        public static void ValidateNotNullOrEmpty<T>(T[] array, Object context = null, [CallerArgumentExpression("array")] string paramName = "")
        {
            ValidateNotNull(array, context, paramName);
            if (array.Length == 0)
            {
                ThrowEmpty(paramName, $"Array of {typeof(T).Name}", context);
            }
        }

        public static void ValidateNotNullOrEmpty<T>(System.Collections.Generic.List<T> list, Object context = null, [CallerArgumentExpression("list")] string paramName = "")
        {
            ValidateNotNull(list, context, paramName);
            if (list.Count == 0)
            {
                ThrowEmpty(paramName, $"List of {typeof(T).Name}", context);
            }
        }

        public static void ValidateNotNullOrEmpty(LineRenderer lineRenderer, Object context = null, [CallerArgumentExpression("lineRenderer")] string paramName = "")
        {
            ValidateNotNull(lineRenderer, context, paramName);
            if (lineRenderer.positionCount == 0)
            {
                ThrowEmpty(paramName, nameof(LineRenderer), context);
            }
        }

        private static void ThrowEmpty(string paramName, string typeName, Object context)
        {
            string message = BuildErrorMessage("The collection is empty", paramName, typeName, context);
            throw new System.ArgumentException(message, paramName);
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

        #region Validate Not Negative
        public static void ValidateNotNegative(int value, Object context = null, [CallerArgumentExpression("value")] string paramName = "")
        {
            if (value < 0)
                ThrowOutOfRange(paramName, value, nameof(System.Int32), context);
        }

        public static void ValidateNotNegative(float value, Object context = null, [CallerArgumentExpression("value")] string paramName = "")
        {
            if (value < 0)
                ThrowOutOfRange(paramName, value, nameof(System.Single), context);
        }

        public static void ValidateNotNegative(double value, Object context = null, [CallerArgumentExpression("value")] string paramName = "")
        {
            if (value < 0)
                ThrowOutOfRange(paramName, value, nameof(System.Double), context);
        }

        public static void ValidateNotNegative(Vector2 value, Object context = null, [CallerArgumentExpression("value")] string paramName = "")
        {
            if (value.x < 0 || value.y < 0)
                ThrowOutOfRange(paramName, value, nameof(UnityEngine.Vector2), context);
        }

        private static void ThrowOutOfRange(string paramName, object actualValue, string typeName, Object context)
        {
            string reason = $"The value is negative (Current value: {actualValue})";
            string message = BuildErrorMessage(reason, paramName, typeName, context);

            throw new System.ArgumentOutOfRangeException(paramName, actualValue, message);
        }
        #endregion

        #region Validate Greater Than 
        public static void ValidateGreaterThan(int value, int threshold, Object context = null, [CallerArgumentExpression("value")] string paramName = "")
        {
            if (value <= threshold)
                ThrowNotGreaterThan(paramName, value, threshold, nameof(System.Int32), context);
        }

        public static void ValidateGreaterThan(float value, float threshold, Object context = null, [CallerArgumentExpression("value")] string paramName = "")
        {
            if (value <= threshold)
                ThrowNotGreaterThan(paramName, value, threshold, nameof(System.Single), context);
        }

        private static void ThrowNotGreaterThan(string paramName, object actualValue, object threshold, string typeName, Object context)
        {
            string reason = $"The value is not greater than the required threshold (Threshold: {threshold}, Current value: {actualValue})";
            string message = BuildErrorMessage(reason, paramName, typeName, context);
            throw new System.ArgumentOutOfRangeException(paramName, actualValue, message);
        }
        #endregion
    
    }
}
