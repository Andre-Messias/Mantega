namespace Mantega.Core.Reactive
{
    using System;
    using System.Collections.Generic;

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
    public class DeferredEvent<T> : IReadOnlyDeferredEvent<T>
    {
        #region HasFired
        /// <summary>
        /// Indicates whether the event has been fired.
        /// </summary>
        private bool _hasFired;

        public bool HasFired => _hasFired;
        #endregion

        #region Value
        /// <summary>
        /// Indicates the value with which the event was fired.
        /// </summary>
        private T _value;

        
        public T Value => _value;
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

        /// <remarks>If the result is already available when this method is called, the callback is
        /// invoked immediately. Otherwise, the callback is invoked once the result becomes available. Multiple
        /// callbacks may be registered and will be invoked in the order they were added.</remarks>
        /// <inheritdoc cref="IReadOnlyDeferredEvent{T}.Then(Action{T})"/>
        public IReadOnlyDeferredEvent<T> Then(Action<T> callback)
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

        /// <remarks>
        /// This method wraps the provided <paramref name="callback"/> in an internal adapter. If the result is already available when this method is called, the callback is
        /// invoked immediately. Otherwise, the callback is invoked once the result becomes available.
        /// <para>
        /// <b>Performance Note:</b> This method incurs higher memory allocation overhead than <see cref="Then(Action{T})"/> 
        /// because it requires internal dictionary tracking to map the original callback to its wrapper. 
        /// Prefer using <see cref="Then(Action{T})"/> in performance-critical code paths to avoid this extra allocation.
        /// </para>
        /// </remarks>
        /// <inheritdoc cref="Then(Action{T})"/>/>
        public IReadOnlyDeferredEvent<T> Then(Action callback)
        {
            Validations.ValidateNotNull(callback);

            if (_hasFired)
            {
                callback.Invoke();
                return this;
            }

            _wrapperMap ??= new();
            Action<T> wrapper = (_ => callback());
            if (!_wrapperMap.TryGetValue(callback, out List<Action<T>> wrapperList))
            {
                wrapperList = new List<Action<T>>();
                _wrapperMap.Add(callback, wrapperList);
            }

            wrapperList.Add(wrapper);
            return Then(wrapper);
        }


        /// <remarks>
        /// This method effectively unsubscribes the listener. 
        /// </remarks>
        /// <inheritdoc cref="Remove(Action{T})"/>/>
        public void Remove(Action<T> callback)
        {
            Validations.ValidateNotNull(callback);

            if (_listeners == null) return;
            _listeners -= callback;
        }

        /// <remarks>
        /// This method performs a dictionary lookup to find the internal wrapper associated with the provided <paramref name="callback"/>.
        /// <para>
        /// <b>Multiple Registrations:</b> If the specific <paramref name="callback"/> was registered multiple times, 
        /// this method removes the <b>most recently added</b> instance (LIFO behavior). You must call <c>Remove</c> once 
        /// for each time you called <c>Then</c> to completely unsubscribe.
        /// </para>
        /// </remarks>
        /// <inheritdoc cref="Remove(Action{T})"/>
        public void Remove(Action callback)
        {
            Validations.ValidateNotNull(callback);

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

            try
            {
                _listeners?.Invoke(value);
            }
            finally
            {
                _listeners = null;
                _wrapperMap = null;
            }
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
            _wrapperMap = null;
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