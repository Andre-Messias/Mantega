namespace System.Runtime.CompilerServices
{
    using System;

    /// <summary>
    /// This attribute enables retrieval of the caller's argument expression as a string.
    /// </summary>
    /// <remarks>Apply this attribute to a parameter of type <see cref="string"/> to automatically obtain the
    /// source expression used for another parameter in the calling code.</remarks>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    internal sealed class CallerArgumentExpressionAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the parameter associated with this instance.
        /// </summary>
        public string ParameterName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CallerArgumentExpressionAttribute"/> class.
        /// </summary>
        /// <remarks>This constructor is typically used to indicate which parameter's expression should be
        /// retrieved at the call site.</remarks>
        /// <param name="parameterName">The name of the method parameter for which the expression is being captured. Cannot be <see langword="null"/>.</param>
        public CallerArgumentExpressionAttribute(string parameterName)
        {
            ParameterName = parameterName ?? throw new ArgumentNullException(nameof(parameterName), "Parameter name cannot be null.");
        }
    }
}