namespace Mantega.Core
{
    /// <summary>
    /// Defines a mechanism for creating a strongly-typed copy of an object.
    /// </summary>
    /// <typeparam name="T">The type of the object to be cloned.</typeparam>
    public interface ITypedClonable<T>
    {
        /// <summary>
        /// Creates a new instance of the current object with the same values as this instance.
        /// </summary>
        /// <returns>A new instance of type <typeparamref name="T"/> that is a copy of the current instance.</returns>
        public T Clone();
    }
}