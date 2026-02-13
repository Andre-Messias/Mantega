namespace Mantega.Core.Reactive
{
    using System;

    using Mantega.Core.Diagnostics;

    /// <summary>
    /// Represents a <see cref="DeferredEvent{T}"/> that can be fired without providing additional event data.
    /// </summary>
    /// <remarks>Use this class when you need to signal an event occurrence without passing any payload to
    /// event handlers. This is a specialization of <see cref="DeferredEvent{Unit}"/> for scenarios where only the event notification
    /// is required.</remarks>
    public class DeferredEvent : IReadOnlyDeferredEvent
    {
        #region HasFired
        /// <summary>
        /// Indicates whether the event has been fired.
        /// </summary>
        protected bool _hasFired;

        public bool HasFired
        {
            get { lock (_lock) return _hasFired; }
        }
        #endregion

        /// <summary>
        /// Internal list of listeners to be invoked when the event is fired.
        /// </summary>
        protected Action _voidListeners;

        /// <summary>
        /// Serves as a lock object to synchronize access to the internal state of the <see cref="DeferredEvent{T}"/>.
        /// </summary>
        /// <remarks>This ensures thread safety for operations that modify or read the state.</remarks>
        protected readonly object _lock = new();

        /// <inheritdoc />
        public virtual IReadOnlyDeferredEvent Then(Action callback)
        {
            Validations.ValidateNotNull(callback);

            bool fireImmediately = false;

            lock (_lock)
            {
                if (_hasFired)
                {
                    fireImmediately = true;
                }
                else
                {
                    _voidListeners += callback;
                }
            }

            if (fireImmediately)
            {
                callback.Invoke();
            }

            return this;
        }

        /// <inheritdoc />
        public void Remove(Action callback)
        {
            Validations.ValidateNotNull(callback);
            lock (_lock)
            {
                if (_voidListeners == null) return;
                _voidListeners -= callback;
            }
        }

        /// <summary>
        /// Invokes all registered listeners with the specified value, if this method has not already been called.
        /// </summary>
        /// <remarks>This method has no effect if called more than once; only the first invocation will
        /// notify listeners. After firing, all listeners are cleared and will not be invoked again.</remarks>
        public virtual void Fire()
        {
            Action toInvoke = null;

            lock (_lock)
            {
                if (_hasFired)
                {
                    Log.Warning($"Attempted to fire {nameof(DeferredEvent)} more than once.");
                    return;
                }

                _hasFired = true;

                // Snapshot e Limpeza
                toInvoke = _voidListeners;
                _voidListeners = null;
            }

            if (toInvoke == null) return;

            // Execução segura fora do lock
            SafeInvoke(toInvoke);
        }

        /// <summary>
        /// Invokes each delegate in the specified action safely, suppressing exceptions thrown by individual listeners.
        /// </summary>
        /// <remarks>Exceptions thrown by any delegate in the action are caught and logged, allowing
        /// remaining delegates to execute.</remarks>
        /// <param name="toInvoke">The action containing one or more delegates to invoke. If <see langword="null"/>, no operation is performed.</param>
        protected static void SafeInvoke(Action toInvoke)
        {
            if (toInvoke == null) return;
            foreach (Delegate handler in toInvoke.GetInvocationList())
            {
                try
                {
                    ((Action)handler).Invoke();
                }
                catch (Exception ex)
                {
                    SafeInvokeError(ex);
                }
            }
        }

        /// <summary>
        /// Logs an error that occurred during listener execution using the provided exception.
        /// </summary>
        /// <remarks>This method is intended for internal use to ensure errors are consistently logged. It
        /// does not throw exceptions and should be used when handling listener failures.</remarks>
        /// <param name="ex">The exception containing details about the error to be logged. Cannot be <see langword="null"/>.</param>
        protected static void SafeInvokeError(Exception ex)
        {
            if(ex == null)
            {
                Log.Error("Listener send a null exception.");
                return;
            }

            Log.Error($"Error in listener: {ex.Message}\n{ex.StackTrace}");
        }

        /// <summary>
        /// Resets the internal state of the object to its initial values.
        /// </summary>
        /// <remarks>Call this method to clear any stored value and remove all listeners. After calling
        /// <c>Reset</c>, the object behaves as if it was newly created.</remarks>
        public virtual void Reset()
        {
            lock (_lock)
            {
                _hasFired = false;
                _voidListeners = null;
            }
        }
    }

    /// <summary>
    /// Represents an event that can be fired once with a value of type <typeparamref name="T"/> and allows listeners to be registered before or after firing.
    /// </summary>
    /// <remarks>It enables deferred notification of a value to registered listeners. Callbacks
    /// registered before the event is fired are invoked when Fire is called; callbacks registered after firing are
    /// invoked immediately. The event can only be fired once, and subsequent calls to Fire are ignored.
    /// <para>
    /// Use <see cref="Reset"/> to clear the state and allow reuse of the object.
    /// </para>
    /// <para>
    /// <b>Thread Safety:</b> This class is fully thread-safe. All public members utilize internal synchronization 
    /// to support concurrent access from multiple threads.
    /// </para></remarks>
    /// <typeparam name="T">The type of value associated with the event when it is fired.</typeparam>
    public class DeferredEvent<T> : DeferredEvent, IReadOnlyDeferredEvent<T>
    {
        #region Value
        /// <summary>
        /// Indicates the value with which the event was fired.
        /// </summary>
        private T _value;

        public T Value 
        { 
            get { lock (_lock) return _value; } 
        }
        #endregion

        #region Listeners
        /// <summary>
        /// Internal list of typed listeners to be invoked when the event is fired.
        /// </summary>
        private Action<T> _typedListeners;
        #endregion

        /// <remarks>If the result is already available when this method is called, the callback is
        /// invoked immediately. Otherwise, the callback is invoked once the result becomes available. Multiple
        /// callbacks may be registered and will be invoked in the order they were added.</remarks>
        /// <inheritdoc />
        public new virtual IReadOnlyDeferredEvent<T> Then(Action callback)
        {
            base.Then(callback);
            return this;
        }

        /// <inheritdoc cref="Then(Action)"/>
        public virtual IReadOnlyDeferredEvent<T> Then(Action<T> callback)
        {
            Validations.ValidateNotNull(callback);

            bool fireImmediately = false;
            T capturedValue = default;

            lock (_lock)
            {
                if (_hasFired)
                {
                    fireImmediately = true;
                    capturedValue = _value;
                }
                else
                {
                    _typedListeners += callback;
                }
            }

            if (fireImmediately)
            {
                callback.Invoke(capturedValue);
            }

            return this;
        }

        /// <inheritdoc cref="DeferredEvent.Remove"/>
        public void Remove(Action<T> callback)
        {
            Validations.ValidateNotNull(callback);

            lock (_lock)
            {
                if (_typedListeners == null) return;
                _typedListeners -= callback;
            }
        }

        /// <remarks>
        /// This method has no effect if called more than once; only the first invocation will notify listeners. 
        /// After firing, all listeners are cleared and will not be invoked again.
        /// <para>
        /// <b>Execution Order:</b> Typed listeners registered via <see cref="Then(Action{T})"/> are invoked <b>first</b>, 
        /// followed by parameterless listeners registered via <see cref="Then(Action)"/>. 
        /// Consequently, the invocation order might not strictly follow the registration order if mixed listener types are used.
        /// </para>
        /// </remarks>
        /// <param name="value">The value to pass to each registered listener.</param>
        /// <inheritdoc cref="DeferredEvent.Fire"/>
        public virtual void Fire(T value)
        {
            Action<T> typedToInvoke = null;
            Action voidToInvoke = null;

            lock (_lock)
            {
                if (_hasFired)
                {
                    Log.Warning($"Attempted to fire {nameof(DeferredEvent<T>)} more than once.");
                    return;
                }

                _hasFired = true;
                _value = value;

                typedToInvoke = _typedListeners;
                voidToInvoke = _voidListeners;

                _typedListeners = null;
                _voidListeners = null;
            }

            SafeInvoke(typedToInvoke, value);
            SafeInvoke(voidToInvoke);
        }

        /// <param name="value">The value to pass to each registered listener.</param>
        /// <inheritdoc cref="DeferredEvent.SafeInvoke(Action)"/>
        protected static void SafeInvoke(Action<T> toInvoke, T value)
        {
            if (toInvoke == null) return;
            foreach (Delegate handler in toInvoke.GetInvocationList())
            {
                try
                {
                    ((Action<T>)handler).Invoke(value);
                }
                catch (Exception ex)
                {
                    SafeInvokeError(ex);
                }
            }
        }

        /// <inheritdoc />
        public override void Reset()
        {
            lock (_lock)
            {
                base.Reset();
                _value = default;
                _typedListeners = null;
            }
        }
    }
}