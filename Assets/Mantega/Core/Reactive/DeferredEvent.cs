namespace Mantega.Core.Reactive
{
    using System;

    public class DeferredEvent<T>
    {
        private bool _hasFired;
        private T _value;
        private Action<T> _listeners;

        public bool HasFired => _hasFired;
        public T Value => _value;

        public void Register(Action<T> callback)
        {
            if (callback == null) return;

            if (_hasFired)
            {
                callback.Invoke(_value);
            }
            else
            {
                _listeners += callback;
            }
        }

        public void Register(Action callback)
        {
            if (callback == null) return;
            Register(_ => callback());
        }

        public void Fire(T value)
        {
            if (_hasFired) return; 

            _hasFired = true;
            _value = value;

            _listeners?.Invoke(value);

            _listeners = null;
        }

        public void Reset()
        {
            _hasFired = false;
            _value = default;
            _listeners = null;
        }
    }


    public class DeferredEvent
    {
        private bool _hasFired;
        private Action _listeners;

        public bool HasFired => _hasFired;

        public void Register(Action callback)
        {
            if (callback == null) return;

            if (_hasFired)
            {
                callback.Invoke();
            }
            else
            {
                _listeners += callback;
            }
        }

        public void Fire()
        {
            if (_hasFired) return;

            _hasFired = true;
            _listeners?.Invoke();
            _listeners = null;
        }

        public void Reset()
        {
            _hasFired = false;
            _listeners = null;
        }
    }
}