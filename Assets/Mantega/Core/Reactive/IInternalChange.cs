namespace Mantega.Core.Reactive
{
    using System;

    /// <summary>
    /// Represents an interface for notifying subscribers about internal changes.
    /// </summary>
    /// <remarks>This interface defines an event that is triggered whenever an internal change occurs.
    /// Implementers of this interface can use the <see cref="OnInternalChange"/> event to notify subscribers about
    /// state changes or other relevant updates.</remarks>
    public interface IInternalChange<T> : ITypedClonable<T>
    {
        /// <summary>
        /// Occurs when a change is detected, providing the old and new values of the changed item.
        /// </summary>
        /// <remarks>This event is triggered whenever an internal change occurs. The first parameter represents
        /// the previous value, and the second parameter represents the new value. Subscribers can use this event to react to changes in
        /// the state or data.</remarks>
        event Action<T, T> OnInternalChange;
    }
}