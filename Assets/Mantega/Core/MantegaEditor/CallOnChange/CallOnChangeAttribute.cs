namespace Mantega.Core.Editor
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Specifies that a method should be invoked when the value of the decorated field changes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This attribute is applied to fields to indicate that a specific method should be called
    /// whenever the field's value changes.
    /// </para>
    /// <para>
    /// The method specified by <paramref name="methodName"/> must exist in the same class 
    /// as the decorated field and match one of the following signatures:
    /// <list type="bullet">
    /// <item><description><b>Two parameters:</b> Accepts the old value and the new value.</description></item>
    /// <item><description><b>No parameters:</b> Simple notification.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class CallOnChangeAttribute : PropertyAttribute
    {
        /// <summary>
        /// Gets the name of the method associated with this instance
        /// </summary>
        [HideInInspector] public readonly string MethodName;

        /// <summary>
        /// Calls a method in the target object when the decorated field changes
        /// </summary>
        /// <param name="methodName">The name of the method to call</param>
        public CallOnChangeAttribute(string methodName)
        {
            MethodName = methodName;
        }
    }
}