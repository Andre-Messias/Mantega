namespace Mantega.Core.Reactive
{
    using Mantega.Core.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using System;
    using UnityEngine;

    /// <summary>
    /// Represents a promise that can be resolved once, signaling the completion of an asynchronous operation.
    /// </summary>
    /// <remarks><b>Thread Safety:</b> This class is fully thread-safe.</remarks>
    [Serializable]
    public class Promise : IReadOnlyPromise, ISerializationCallbackReceiver
    {
        #region IsResolved
        /// <summary>
        /// Indicates whether the promise has been successfully resolved.
        /// </summary>
        [SerializeField] protected bool _isResolved;

        /// <inheritdoc />
        public bool IsResolved
        {
            get { lock (_lock) return _isResolved; }
        }
        #endregion

        /// <summary>
        /// Serves as a lock object to synchronize access to the internal state of the <see cref="Promise{T}"/>.
        /// </summary>
        /// <remarks>This ensures thread safety for operations that modify or read the state.</remarks>
        protected readonly object _lock = new();

        /// <summary>
        /// Represents the asynchronous operation's completion state for the current task.
        /// </summary>
        protected TaskCompletionSource<bool> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// Gets the underlying task that represents the asynchronous operation.
        /// </summary>
        public Task Task => _tcs.Task;

        /// <inheritdoc/>
        public TaskAwaiter GetAwaiter() => ((Task)_tcs.Task).GetAwaiter();

        /// <summary>
        /// Transitions the promise to a resolved state, allowing any awaiting operations to continue.
        /// </summary>
        /// <remarks>Subsequent calls are logged as warnings and ignored.</remarks>
        public virtual void Resolve()
        {
            TaskCompletionSource<bool> tcsToComplete = null;

            lock (_lock)
            {
                if (_isResolved)
                {
                    Log.Warning($"Attempted to fire {nameof(Promise)} more than once.");
                    return;
                }

                _isResolved = true; 
                tcsToComplete = _tcs;
            }

            tcsToComplete?.TrySetResult(true);
        }

        /// <summary>
        /// Resets the internal state of the promise to its initial pending state.
        /// </summary>
        public virtual void Reset()
        {
            lock (_lock)
            {
                _isResolved = false;
                _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }

        /// <summary>
        /// Cancels the promise.
        /// </summary>
        /// <remarks>
        /// If the promise is already resolved, cancellation will have no effect.
        /// If the promise is pending, it will transition to a canceled state,
        /// causing any awaiting operations to throw a <see cref="TaskCanceledException"/>.
        /// </remarks>
        public virtual void Cancel()
        {
            lock (_lock)
            {
                if (_isResolved) return;
                _isResolved = true; // Avoid multiple cancellations or resolution after cancellation.
            }

            _tcs.TrySetCanceled();
        }

        /// <summary>
        /// Called before the object is serialized to allow for custom pre-serialization logic.
        /// </summary>
        /// <remarks>
        /// This method is empty by default.
        /// </remarks>
        public virtual void OnBeforeSerialize() { }

        /// <summary>
        /// Performs post-deserialization initialization to restore the promise internal state.
        /// </summary>
        public virtual void OnAfterDeserialize()
        {
            _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (_isResolved)
            {
                _tcs.TrySetResult(true);
            }
        }
    }

    /// <summary>
    /// Represents a thread-safe promise that resolves with a value of type <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>It enables asynchronous yielding until a specific value is provided. Awaiting this object 
    /// will pause execution until <see cref="Resolve(T)"/> is called.
    /// <para>
    /// <b>Thread Safety:</b> This class is fully thread-safe.
    /// </para></remarks>
    /// <typeparam name="T">The type of the value produced by the promise when it resolves.</typeparam>
    [Serializable]
    public class Promise<T> : Promise, IReadOnlyPromise<T>
    {
        #region Value
        /// <summary>
        /// Indicates the value with which the promise was resolved.
        /// </summary>
        [SerializeField] protected T _value;

        /// <inheritdoc/>
        public T Value 
        {
            get
            {
                lock (_lock)
                {
                    if(!_isResolved)
                    {
                        throw new InvalidOperationException($"Attempted to access the Value of {nameof(Promise<T>)} before it was resolved. Ensure you 'await' the promise before accessing its value.");
                    }
                    return _value;
                }
            }
        }
        #endregion

        /// <summary>
        /// Represents the asynchronous operation's completion source for type T.
        /// </summary>
        private TaskCompletionSource<T> _tcsT = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <inheritdoc/>
        public new Task<T> Task => _tcsT.Task;

        /// <inheritdoc/>
        public new TaskAwaiter<T> GetAwaiter() => _tcsT.Task.GetAwaiter();

        /// <summary>
        /// Transitions the promise to a resolved state with the specified value, allowing any awaiting operations to continue and retrieve the result.
        /// </summary>
        /// <remarks>
        /// This method has no effect if called more than once; only the first invocation will complete the promise. 
        /// Resolving this typed promise also implicitly resolves the base parameterless promise.
        /// </remarks>
        /// <param name="value">The value to fulfill the promise with.</param>
        public virtual void Resolve(T value)
        {
            TaskCompletionSource<bool> baseTcsToComplete = null;
            TaskCompletionSource<T> typedTcsToComplete = null;

            lock (_lock)
            {
                if (_isResolved)
                {
                    Log.Warning($"Attempted to fire {nameof(Promise<T>)} more than once.");
                    return;
                }

                _isResolved = true;
                _value = value;

                baseTcsToComplete = _tcs;
                typedTcsToComplete = _tcsT;
            }

            baseTcsToComplete?.TrySetResult(true);
            typedTcsToComplete?.TrySetResult(value);
        }

        /// <inheritdoc/>
        public override void Reset()
        {
            lock (_lock)
            {
                base.Reset();
                _value = default;
                _tcsT = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }

        /// <inheritdoc/>
        public override void Cancel()
        {
            base.Cancel();
            lock (_lock)
            {
                _value = default; // Clear the value on cancellation.
            }
        
            _tcsT.TrySetCanceled();
        }

        /// <inheritdoc/>
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            _tcsT = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (_isResolved)
            {
                _tcsT.TrySetResult(_value);
            }
        }
    }
}