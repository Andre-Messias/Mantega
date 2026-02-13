namespace Mantega.Core.Reactive
{
    using System;

    /// <summary>
    /// Represents a read-only deferred event.
    /// </summary>
    /// <remarks>A deferred event allows registration of callbacks that are invoked when the event is fired.
    /// This interface exposes only read-only operations; it does not provide a way to fire the event or modify its
    /// state. Callbacks registered after the event has already fired are invoked immediately.</remarks>
    /// <typeparam name="T">The type of the value associated with the event.</typeparam>
    public interface IReadOnlyDeferredEvent<T>
    {
        /// <summary>
        /// Gets a value indicating whether the event has already fired.
        /// </summary>
        bool HasFired { get; }

        /// <summary>
        /// Gets the value with which the event was fired.
        /// </summary>
        T Value { get; }


        /// <summary>
        /// Registers a callback to be invoked when the event is ready.
        /// </summary>
        /// <param name="callback">The action to execute with the <see cref="Value"/> when it becomes available. Cannot be <see langword="null"/>.</param>
        /// <returns>The current instance of <see cref="IReadOnlyDeferredEvent{T}"/> to allow chaining.</returns>
        public IReadOnlyDeferredEvent<T> Then(Action<T> callback);

        /// <inheritdoc cref="Then(Action{T})"/>
        public IReadOnlyDeferredEvent<T> Then(Action callback);

        /// <summary>
        /// Removes a previously registered callback from the event.
        /// </summary>
        /// <param name="callback">The specific action instance to remove.</param>
        public void Remove(Action<T> callback);

        /// <inheritdoc cref="Remove(Action{T})"/>
        public void Remove(Action callback);
    }

    /// <inheritdoc />
    public interface IReadOnlyDeferredEvent : IReadOnlyDeferredEvent<Unit>
    {
        /// <param name="callback">The action to execute when the event becomes available. Cannot be <see langword="null"/>.</param>
        /// <returns>The current instance of <see cref="IReadOnlyDeferredEvent"/> to allow chaining.</returns>
        /// <inheritdoc cref="IReadOnlyDeferredEvent{T}.Then(Action)"/>
        new IReadOnlyDeferredEvent Then(Action callback);
    }
}