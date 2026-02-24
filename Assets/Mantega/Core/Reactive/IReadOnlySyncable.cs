namespace Mantega.Core.Reactive
{
    using System;

    /// <summary>
    /// Represents a read-only synchronization interface for a value of type <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>This interface provides access to a value that can be observed for changes. Consumers can
    /// subscribe to the <see cref="OnValueChanged"/> event to be notified when the value changes.</remarks>
    /// <typeparam name="T">The type of the value being synchronized.</typeparam>
    public interface IReadOnlySyncable<T>
    {
        /// <summary>
        /// Gets the value stored in the current instance.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Occurs when the value changes, providing the previous and current values.
        /// </summary>
        /// <remarks>This event is triggered whenever the value is updated. The first parameter represents
        /// the previous value, and the second parameter represents the new value. Subscribers can use this event to
        /// react to changes in the value.</remarks>
        public event Action<T, T> OnValueChanged;
    }
}
