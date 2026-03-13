namespace Mantega.Core.Reactive.Example
{
    ///
    /// This script demonstrates how to use the Syncable and IInternalChange interfaces to create a synchronizable value that notifies subscribers of changes, including internal changes. 
    /// 

    using System;
    using UnityEngine;

    using Mantega.Core.Reactive;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    public class Reactive_Example : MonoBehaviour
    {
        [Serializable]
        private class InternalChange_Example : IInternalChange<InternalChange_Example>
        {
            [SerializeField] private int _value1;
            public int Value1
            {
                get => _value1;
                set
                {
                    if (_value1 == value) return;
                    ChangeValue1WithEvent(_value1, value);
                }
            }

            [SerializeField] private Color _value2;
            public Color Value2
            {
                get => _value2;
                set
                {
                    if (_value2 == value) return;
                    ChangeValue2WithEvent(_value2, value);
                }
            }

            public event Action<InternalChange_Example, InternalChange_Example> OnInternalChange;

            public InternalChange_Example(int value1, Color value2)
            {
                _value1 = value1;
                _value2 = value2;
            }

            private void ChangeValue1WithEvent(int oldValue, int newValue)
            {
                InternalChange_Example clone = Clone();
                clone._value1 = oldValue;
                _value1 = newValue;
                OnInternalChange?.Invoke(clone, this);
            }

            private void ChangeValue2WithEvent(Color oldValue, Color newValue)
            {
                InternalChange_Example clone = Clone();
                clone._value2 = oldValue;
                _value2 = newValue;
                OnInternalChange?.Invoke(clone, this);
            }

            public InternalChange_Example Clone()
            {
                return new InternalChange_Example(_value1, _value2);
            }

            public override bool Equals(object obj)
            {
                if (obj is not InternalChange_Example other) return false;
                return _value1 == other._value1 && _value2 == other._value2;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(_value1, _value2);
            }
        }

        // Syncable - Can be public if you want to edit it from other scripts.
#if UNITY_EDITOR
        [CallOnChange(nameof(OnColorChange))]
#endif
        [SerializeField] private Syncable<Color> _colorSyncable = new(Color.white);
#if UNITY_EDITOR
        [CallOnChange(nameof(OnInternalChange))]
#endif
        [SerializeField] private Syncable<InternalChange_Example> _internalChangeSyncable = new(new(0, Color.white));

        // IReadOnlySyncable - Allows you to read the value and subscribe to the event, but not modify it. Useful for exposing Syncables without allowing external modification.
        public IReadOnlySyncable<Color> ColorSyncable => _colorSyncable;

        // Promise - Used to await a future resolution.
        [SerializeField] private Promise<int> _promise = new();

        // IReadOnlyPromise - Allows you to await the promise, but not resolve it. Useful for exposing Promises without allowing external resolution.
        public IReadOnlyPromise<int> Promise => _promise;

        private void OnEnable()
        {
            _colorSyncable.OnValueChanged += OnColorChange;
            _internalChangeSyncable.OnValueChanged += OnInternalChange;
        }

        private void OnDisable()
        {
            _colorSyncable.OnValueChanged -= OnColorChange;
            _internalChangeSyncable.OnValueChanged -= OnInternalChange;
        }

        private void OnColorChange(Color oldValue, Color newValue)
        {
            Debug.Log($"Color Syncable Changed: {oldValue} -> {newValue}");
        }

        private void OnInternalChange(InternalChange_Example oldValue, InternalChange_Example newValue)
        {
            Debug.Log($"{oldValue == newValue} Internal Change Syncable Changed: Value1: {oldValue.Value1} -> {newValue.Value1}, Value2: {oldValue.Value2} -> {newValue.Value2}");
        }

        public async void AwaitPromise()
        {
            Debug.Log("Asked Deferred Event. If it has already been fired, the callback will be invoked immediately. Otherwise, it will be invoked once the event is fired.");
            var result = await _promise;
            OnPromiseResolved(result);
        }

        private void OnPromiseResolved(int value)
        {
            if (this == null) return;
            Debug.Log($"Deferred Event Fired with value: {value}");
        }

#if UNITY_EDITOR

        [CustomEditor(typeof(Reactive_Example))]
        public class Syncables_ExampleEditor : Editor
        {
            private Color GenerateRandomColor() => new(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();
                Reactive_Example example = (Reactive_Example)target;

                // Title
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Syncable Changes Example", EditorStyles.boldLabel);

                // Changing syncable values
                if (GUILayout.Button("Change Color Syncable"))
                {
                    example._colorSyncable.Value = GenerateRandomColor();
                }
                else if (GUILayout.Button("Change Internal Change Syncable Value1"))
                {
                    example._internalChangeSyncable.Value.Value1 += (int)(UnityEngine.Random.value * 100);
                }
                else if (GUILayout.Button("Change Internal Change Syncable Value2"))
                {
                    example._internalChangeSyncable.Value.Value2 = GenerateRandomColor();
                }

                // Title
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Promise Example", EditorStyles.boldLabel);

                // Resolving the promise
                if (GUILayout.Button("Resolve Promise with random value"))
                {
                    example._promise.Resolve(UnityEngine.Random.Range(0, 100));
                }
                else if (GUILayout.Button("Reset Promise"))
                {
                    example._promise.Reset();
                    Debug.Log("Promise Reset. You can now resolve it again to see the changes.");
                }
                else if (GUILayout.Button("Await Promise"))
                {
                    example.AwaitPromise();
                }
            }
        }
#endif
    }
}