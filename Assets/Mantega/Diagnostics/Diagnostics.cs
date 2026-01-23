namespace Mantega.Diagnostics
{
    using System.Runtime.CompilerServices;
    public static class Validations 
    {
        public static void ValidateNotNull<T>(T obj, [CallerArgumentExpression("obj")] string paramName = "") where T : class 
        {
            if (IsNull(obj))
            {
                string typeName = typeof(T).Name;
                throw new System.ArgumentNullException(paramName, $"[Mantega] The {typeName} reference is missing.");
            }
        }

        public static void ValidateNotNull<T>(T obj, UnityEngine.Object context, [CallerArgumentExpression("obj")] string paramName = "") where T : class
        {
            if (IsNull(obj))
            {
                string typeName = typeof(T).Name;
                string message = $"[{context.GetType().Name}] Validation failed: The {typeName} '{paramName}' is null on '{context.name}'.";
                throw new System.ArgumentNullException(paramName, message);
            }
        }

        private static bool IsNull<T>(T obj)
        {
            return obj == null || obj.Equals(null);
        }
    }
}
