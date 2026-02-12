namespace Mantega.Core.Reactive
{
    using System;

    using Mantega.Core.Diagnostics;

    /// <summary>
    /// Represents an event that can be fired once with a value of type <typeparamref name="T"/> and allows listeners to be registered before or after firing.
    /// </summary>
    /// <remarks>DeferredEvent<T> enables deferred notification of a value to registered listeners. Callbacks
    /// registered before the event is fired are invoked when Fire is called; callbacks registered after firing are
    /// invoked immediately. The event can only be fired once, and subsequent calls to Fire are ignored. Use Reset to
    /// clear the state and allow reuse of the object. This class is not thread-safe; synchronize access if used in
    /// multithreaded scenarios.</remarks>
    /// <typeparam name="T">The type of value associated with the event when it is fired.</typeparam>
    public class DeferredEvent<T>
    {
        #region HasFired
        /// <summary>
        /// Indicates whether the event has been fired.
        /// </summary>
        private bool _hasFired;

        /// <summary>
        /// Gets a value indicating whether the event has already fired.
        /// </summary>
        public bool HasFired => _hasFired;
        #endregion

        #region Value
        /// <summary>
        /// Indicates the value with which the event was fired.
        /// </summary>
        private T _value;

        /// <summary>
        /// Gets the value with which the event was fired.
        /// </summary>
        public T Value => _value;
        #endregion

        #region Listeners
        /// <summary>
        /// Internal list of listeners to be invoked when the event is fired.
        /// </summary>
        private Action<T> _listeners;
        #endregion

        /// <summary>
        /// Registers a callback to be invoked when the event is ready.
        /// </summary>
        /// <remarks>If the result is already available when this method is called, the callback is
        /// invoked immediately. Otherwise, the callback is invoked once the result becomes available. Multiple
        /// callbacks may be registered and will be invoked in the order they were added.</remarks>
        /// <param name="callback">The action to execute with the <see cref="_value"/> when it becomes available. Cannot be <see langword="null"/>.</param>
        public DeferredEvent<T> Then(Action<T> callback)
        {
            Validations.ValidateNotNull(callback);

            if (_hasFired)
            {
                callback.Invoke(_value);
            }
            else
            {
                _listeners += callback;
            }

            return this;
        }

        /// <inheritdoc cref="Then(Action{T})"/>/>
        public DeferredEvent<T> Then(Action callback)
        {
            Validations.ValidateNotNull(callback);

            return Then(_ => callback());
        }

        /// <summary>
        /// Invokes all registered listeners with the specified value, if this method has not already been called.
        /// </summary>
        /// <remarks>This method has no effect if called more than once; only the first invocation will
        /// notify listeners. After firing, all listeners are cleared and will not be invoked again.</remarks>
        /// <param name="value">The value to pass to each registered listener.</param>
        public void Fire(T value)
        {
            if (_hasFired)
            {
                Log.Warning("Attempted to fire DeferredEvent more than once. This call will be ignored.");
                return;
            }

            _hasFired = true;
            _value = value;

            _listeners?.Invoke(value);

            _listeners = null;
        }

        /// <summary>
        /// Resets the internal state of the object to its initial values.
        /// </summary>
        /// <remarks>Call this method to clear any stored value and remove all listeners. After calling
        /// <c>Reset</c>, the object behaves as if it was newly created. This method is not thread-safe; ensure that no
        /// other operations are performed concurrently.</remarks>
        public void Reset()
        {
            _hasFired = false;
            _value = default;
            _listeners = null;
        }
    }

    /// <summary>
    /// Represents a deferred event that can be fired without providing additional event data.
    /// </summary>
    /// <remarks>Use this class when you need to signal an event occurrence without passing any payload to
    /// event handlers. This is a specialization of DeferredEvent<Unit> for scenarios where only the event notification
    /// is required.</remarks>
    public class DeferredEvent : DeferredEvent<Unit>
    {
        /// <summary>
        /// Invokes all registered listeners, if this method has not already been called.
        /// </summary>
        /// <remarks>This method is a convenience overload for firing the event when no additional
        /// information needs to be provided to event handlers. It is equivalent to calling the base Fire method with a
        /// default unit value.</remarks>
        public void Fire() => base.Fire(Unit.Default);
    }
}