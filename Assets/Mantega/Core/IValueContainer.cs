namespace Mantega.Core
{
    /// <summary>
    /// Defines a contract for objects that encapsulate a value and provide access to it.
    /// </summary>
    /// <remarks>Implementations of this interface allow consumers to retrieve the contained value in a
    /// type-agnostic manner.</remarks>
    public interface IValueContainer
    {
        /// <summary>
        /// Retrieves the current value represented by the implementing object.
        /// </summary>
        /// <returns>An object containing the current value. The exact type and meaning of the value depend on the
        /// implementation.</returns>
        object GetValue();
    }
}