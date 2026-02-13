namespace Mantega.Core.Reactive
{
    using System;
    using System.Collections.Generic;

    using Mantega.Core.Diagnostics;

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
    public class DeferredEvent<T> : IReadOnlyDeferredEvent<T>
    {
        #region HasFired
        /// <summary>
        /// Indicates whether the event has been fired.
        /// </summary>
        private bool _hasFired;

        public bool HasFired
        { 
            get { lock (_lock) return _hasFired; } 
        }
        #endregion

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
        /// Internal list of listeners to be invoked when the event is fired.
        /// </summary>
        private Action<T> _listeners;
        #endregion

        #region WrapperMap
        /// <summary>
        /// Internal mapping of original callbacks to their wrapped counterparts for removal purposes.
        /// </summary>
        private Dictionary<Action, List<Action<T>>> _wrapperMap;
        #endregion

        /// <summary>
        /// Serves as a lock object to synchronize access to the internal state of the <see cref="DeferredEvent{T}"/>.
        /// </summary>
        /// <remarks>This ensures thread safety for operations that modify or read the state.</remarks>
        private readonly object _lock = new();

        /// <remarks>If the result is already available when this method is called, the callback is
        /// invoked immediately. Otherwise, the callback is invoked once the result becomes available. Multiple
        /// callbacks may be registered and will be invoked in the order they were added.</remarks>
        /// <inheritdoc />
        public IReadOnlyDeferredEvent<T> Then(Action<T> callback)
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
                    _listeners += callback;
                }
            }

            // Avoid invoking the callback while holding the lock to prevent potential deadlocks or performance issues if the callback is long-running.
            if (fireImmediately)
            {
                callback.Invoke(capturedValue);
            }

            return this;
        }

        /// <remarks>
        /// This method wraps the provided <paramref name="callback"/> in an internal adapter. If the result is already available when this method is called, the callback is
        /// invoked immediately. Otherwise, the callback is invoked once the result becomes available.
        /// <para>
        /// <b>Performance Note:</b> This method incurs higher memory allocation overhead than <see cref="Then(Action{T})"/> 
        /// because it requires internal dictionary tracking to map the original callback to its wrapper. 
        /// Prefer using <see cref="Then(Action{T})"/> in performance-critical code paths to avoid this extra allocation.
        /// </para>
        /// </remarks>
        /// <inheritdoc />
        public IReadOnlyDeferredEvent<T> Then(Action callback)
        {
            Validations.ValidateNotNull(callback);

            bool fireImmediately = false;
            Action<T> wrapper = null;

            lock (_lock)
            {
                if (_hasFired)
                {
                    fireImmediately = true;
                }
                else
                {
                    _wrapperMap ??= new();

                    wrapper = (_ => callback());

                    if (!_wrapperMap.TryGetValue(callback, out List<Action<T>> wrapperList))
                    {
                        wrapperList = new List<Action<T>>();
                        _wrapperMap.Add(callback, wrapperList);
                    }
                    wrapperList.Add(wrapper);
                }
            }

            // Avoid invoking the callback while holding the lock to prevent potential deadlocks or performance issues if the callback is long-running.
            if (fireImmediately)
            {
                callback.Invoke();
                return this;
            }

            return Then(wrapper);
        }


        /// <remarks>
        /// This method effectively unsubscribes the listener. 
        /// </remarks>
        /// <inheritdoc />
        public void Remove(Action<T> callback)
        {
            Validations.ValidateNotNull(callback);

            lock (_lock)
            {
                if (_listeners == null) return;
                _listeners -= callback;
            }
        }

        /// <remarks>
        /// This method performs a dictionary lookup to find the internal wrapper associated with the provided <paramref name="callback"/>.
        /// <para>
        /// <b>Multiple Registrations:</b> If the specific <paramref name="callback"/> was registered multiple times, 
        /// this method removes the <b>most recently added</b> instance (LIFO behavior). You must call <c>Remove</c> once 
        /// for each time you called <c>Then</c> to completely unsubscribe.
        /// </para>
        /// </remarks>
        /// <inheritdoc />
        public void Remove(Action callback)
        {
            Validations.ValidateNotNull(callback);

            lock (_lock)
            {
                if (_listeners == null || _wrapperMap == null) return;
                if (_wrapperMap.TryGetValue(callback, out List<Action<T>> wrapperList))
                {
                    if (wrapperList.Count > 0)
                    {
                        // Last wrapper is the most recently added
                        int lastIndex = wrapperList.Count - 1;
                        Action<T> wrapperToRemove = wrapperList[lastIndex];

                        _listeners -= wrapperToRemove;
                        wrapperList.RemoveAt(lastIndex);

                        // Empty wrapper list means no more wrappers for this callback
                        if (wrapperList.Count == 0)
                        {
                            _wrapperMap.Remove(callback);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Invokes all registered listeners with the specified value, if this method has not already been called.
        /// </summary>
        /// <remarks>This method has no effect if called more than once; only the first invocation will
        /// notify listeners. After firing, all listeners are cleared and will not be invoked again.</remarks>
        /// <param name="value">The value to pass to each registered listener.</param>
        public void Fire(T value)
        {
            Action<T> toInvoke = null;

            lock (_lock)
            {
                if (_hasFired)
                {
                    Log.Warning($"Attempted to fire {nameof(DeferredEvent)} more than once.");
                    return;
                }

                _hasFired = true;
                _value = value;

                toInvoke = _listeners;
                _listeners = null;
                _wrapperMap = null;
            }

            if (toInvoke == null) return;

            Delegate[] invocationList = toInvoke.GetInvocationList();

            foreach (Delegate handler in invocationList)
            {
                try
                {
                    ((Action<T>)handler).Invoke(value);
                }
                catch (Exception ex)
                {
                    Log.Error($"Error in {nameof(DeferredEvent)} listener: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        /// <summary>
        /// Resets the internal state of the object to its initial values.
        /// </summary>
        /// <remarks>Call this method to clear any stored value and remove all listeners. After calling
        /// <c>Reset</c>, the object behaves as if it was newly created.</remarks>
        public void Reset()
        {
            lock (_lock)
            {
                _hasFired = false;
                _value = default;
                _listeners = null;
                _wrapperMap = null;
            }
        }
    }

    /// <summary>
    /// Represents a <see cref="DeferredEvent{T}"/> that can be fired without providing additional event data.
    /// </summary>
    /// <remarks>Use this class when you need to signal an event occurrence without passing any payload to
    /// event handlers. This is a specialization of <see cref="DeferredEvent{Unit}"/> for scenarios where only the event notification
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