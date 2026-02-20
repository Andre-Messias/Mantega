namespace Mantega.Core.Reactive
{
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a read-only promise that can be awaited.
    /// </summary>
    /// <remarks>
    /// A promise represents the eventual completion of an asynchronous operation.
    /// This interface exposes only read-only operations, allowing consumers to await the operation's 
    /// completion without being able to resolve or modify its state. If the promise is already resolved, 
    /// awaiting it will continue execution synchronously.
    /// </remarks>
    public interface IReadOnlyPromise
    {
        /// <summary>
        /// Gets a value indicating whether the promise has already been resolved.
        /// </summary>
        bool IsResolved { get; }

        /// <summary>
        /// Gets the underlying <see cref="System.Threading.Tasks.Task"/> associated with this promise.
        /// </summary>
        Task Task { get; }

        /// <summary>
        /// Gets an awaiter used to await this promise.
        /// </summary>
        /// <remarks>This method enables the use of the <c>await</c> keyword directly on the promise instance.</remarks>
        /// <returns>A <see cref="TaskAwaiter"/> that can be used to await the promise.</returns>
        TaskAwaiter GetAwaiter();
    }

    /// <summary>
    /// Represents a read-only promise associated with a value of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value produced by the promise when it resolves.</typeparam>
    /// <inheritdoc />
    public interface IReadOnlyPromise<T> : IReadOnlyPromise
    {
        /// <summary>
        /// Gets the value with which the promise was resolved.
        /// </summary>
        /// <remarks>Accessing this property before the promise is resolved throws an <see cref="InvalidOperationException"/>.</remarks>
        /// <exception cref="InvalidOperationException">Thrown if the promise has not yet been resolved.</exception>"
        T Value { get; }

        /// <summary>
        /// Gets the underlying <see cref="Task{TResult}"/> associated with this promise.
        /// </summary>
        new Task<T> Task { get; }

        /// <summary>
        /// Gets an awaiter used to await this promise and retrieve its result.
        /// </summary>
        /// <remarks>This method enables the use of the <c>await</c> keyword directly on the promise instance.</remarks>
        /// <returns>A <see cref="TaskAwaiter{T}"/> that can be used to await the promise and retrieve its result.</returns>
        new TaskAwaiter<T> GetAwaiter();
    }
}