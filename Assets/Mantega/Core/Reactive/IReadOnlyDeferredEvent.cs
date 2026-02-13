namespace Mantega.Core.Reactive
{
    using System;

    /// <summary>
    /// Represents a read-only deferred event.
    /// </summary>
    /// <remarks>
    /// A deferred event allows registration of callbacks that are invoked when the event is fired.
    /// This interface exposes only read-only operations. Callbacks registered after the event has already fired are invoked immediately.
    /// </remarks>
    public interface IReadOnlyDeferredEvent
    {
        /// <summary>
        /// Gets a value indicating whether the event has already fired.
        /// </summary>
        bool HasFired { get; }

        /// <summary>
        /// Registers a callback to be invoked when the event is ready.
        /// </summary>
        /// <param name="callback">The action to execute. Cannot be <see langword="null"/>.</param>
        /// <returns>The current instance of <see cref="IReadOnlyDeferredEvent"/> to allow chaining.</returns>
        IReadOnlyDeferredEvent Then(Action callback);

        /// <summary>
        /// Removes a previously registered callback from the event.
        /// </summary>
        /// <param name="callback">The specific action instance to remove.</param>
        void Remove(Action callback);
    }

    /// <summary>
    /// Represents a read-only deferred event associated with a value of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value associated with the event.</typeparam>
    /// <inheritdoc />
    public interface IReadOnlyDeferredEvent<T> : IReadOnlyDeferredEvent
    {
        /// <summary>
        /// Gets the value with which the event was fired.
        /// </summary>
        T Value { get; }

        /// <remarks>
        /// This method shadows the base interface to return the typed <see cref="IReadOnlyDeferredEvent{T}"/> for fluent chaining.
        /// </remarks>
        /// <returns>The current instance of <see cref="IReadOnlyDeferredEvent{T}"/> to allow chaining.</returns>
        /// <inheritdoc cref="IReadOnlyDeferredEvent.Then(Action)"/>
        new IReadOnlyDeferredEvent<T> Then(Action callback);

        /// <summary>
        /// Registers a callback to be invoked when the event is ready using the event's value.
        /// </summary>
        /// <param name="callback">The action to execute with the <see cref="Value"/> when it becomes available.</param>
        /// <inheritdoc cref="Then(Action)"/>
        IReadOnlyDeferredEvent<T> Then(Action<T> callback);

        /// <inheritdoc />
        void Remove(Action<T> callback);
    }
}