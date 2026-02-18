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
        }

        // Syncable - Can be public if you want to edit it from other scripts.
#if UNITY_EDITOR
        [CallOnChange(nameof(OnEditorChangeColor))]
#endif
        [SerializeField] private Syncable<Color> _colorSyncable = new(Color.white);
#if UNITY_EDITOR
        [CallOnChange(nameof(OnEditorChangeInternalChange))]
#endif
        [SerializeField] private Syncable<InternalChange_Example> _internalChangeSyncable = new(new(0, Color.white));

        // IReadOnlySyncable - Allows you to read the value and subscribe to the event, but not modify it. Useful for exposing Syncables without allowing external modification.
        public IReadOnlySyncable<Color> ColorSyncable => _colorSyncable;

        // DeferredEvent - Used to call a function when the object is ready
        [SerializeField] private DeferredEvent<int> _deferredEvent = new();

        // IReadOnlyDeferredEvent - Allows you to subscribe to the event, but not fire it. Useful for exposing DeferredEvents without allowing external firing.
        public IReadOnlyDeferredEvent<int> DeferredEvent => _deferredEvent;

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
            Debug.Log($"Internal Change Syncable Changed: Value1: {oldValue.Value1} -> {newValue.Value1}, Value2: {oldValue.Value2} -> {newValue.Value2}");
        }

        public void AskDeferredEvent()
        {
            Debug.Log("Asked Deferred Event. If it has already been fired, the callback will be invoked immediately. Otherwise, it will be invoked once the event is fired.");
            _deferredEvent.Then(OnDeferrendEvent);
        }

        private void OnDeferrendEvent(int value)
        {
            if (this == null) return;
            Debug.Log($"Deferred Event Fired with value: {value}");
        }

#if UNITY_EDITOR
        private void OnColorChange()
        {
            Debug.Log($"Color Syncable Changed: {_colorSyncable.Value}");
        }

        private void OnInternalChange()
        {
            Debug.Log($"Internal Change Syncable Changed: Value1: {_internalChangeSyncable.Value.Value1}, Value2: {_internalChangeSyncable.Value.Value2}");
        }

        private void OnEditorChangeColor()
        {
            OnColorChange();
        }

        private void OnEditorChangeInternalChange()
        {
            OnInternalChange();
        }
        
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
                bool changeButtonPressed = true;
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
                else
                {
                    changeButtonPressed = false;
                }

                // Title
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("DeferredEvent Example", EditorStyles.boldLabel);

                // Firing the deferred event
                if(GUILayout.Button("Fire Deferred Event with random value"))
                {
                    example._deferredEvent.Fire(UnityEngine.Random.Range(0, 100));
                }
                else if(GUILayout.Button("Reset Deferred Event"))
                {
                    example._deferredEvent.Reset();
                    Debug.Log("Deferred Event Reset. You can now fire it again to see the changes.");
                }
                else if(GUILayout.Button("Ask Deferred Event"))
                {
                    example.AskDeferredEvent();
                }

                if (changeButtonPressed)
                {
                    // Mark the object as dirty to ensure changes are saved and reflected in the editor
                    EditorUtility.SetDirty(example);

                    if (!Application.isPlaying)
                    {
                        Debug.Log("Enter Play Mode to see the changes in action.");
                    }
                }
            }
        }
#endif
    }

}